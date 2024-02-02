using Derg.Runtime;
using Elements.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Derg.Modules
{
    public static class ModuleReflector
    {
        private static readonly ConcurrentDictionary<
            Type,
            Func<object, (ApiFunc, HostFunc)[]>
        > _reflectedFuncs = new ConcurrentDictionary<Type, Func<object, (ApiFunc, HostFunc)[]>>();

        public static (ApiFunc, HostFunc)[] ReflectHostFuncs<T>(T ctx)
        {
            return ReflectHostFuncs((object)ctx);
        }

        public static (ApiFunc, HostFunc)[] ReflectHostFuncs(object ctx)
        {
            return _reflectedFuncs.GetOrAdd(ctx.GetType(), ReflectHostFuncsInternal)(ctx);
        }

        private static Func<object, (ApiFunc, HostFunc)[]> ReflectHostFuncsInternal(Type t)
        {
            var modAttr = t.GetCustomAttribute<ModAttribute>();
            var defaultModule = modAttr?.DefaultModule ?? "env";

            var rawContext = Expression.Parameter(typeof(object), "ctx");
            var context = Expression.Variable(t, "context");

            var tuples = new List<Expression>();
            foreach (
                var method in t.GetMethods(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Static
                        | BindingFlags.Instance
                )
            )
            {
                var modFnAttr = method.GetCustomAttributes<ModFnAttribute>();
                if (modFnAttr.Count() == 0)
                {
                    continue;
                }
                if (!method.IsPublic)
                {
                    throw new InvalidOperationException(
                        $"{method} in {t} is not public, but was declared as a module function. Please make it public."
                    );
                }

                foreach (var attr in modFnAttr)
                {
                    MethodInfo boundMethod = method;
                    if (attr.Generics.Length > 0)
                    {
                        if (!boundMethod.IsGenericMethod)
                        {
                            throw new InvalidOperationException(
                                $"Reflected module function {method} on {t} attribute provided generic arguments, but the method is not generic."
                            );
                        }
                        boundMethod = boundMethod.MakeGenericMethod(attr.Generics);
                    }
                    tuples.Add(
                        ReflectHostFunc(
                            attr.Name ?? boundMethod.Name,
                            attr.Module ?? defaultModule,
                            boundMethod,
                            context
                        )
                    );
                }
            }

            var body = Expression.Block(
                new[] { context },
                Expression.Assign(context, Expression.Convert(rawContext, t)),
                Expression.NewArrayInit(typeof((ApiFunc, HostFunc)), tuples.ToArray())
            );

            var lambda = Expression.Lambda<Func<object, (ApiFunc, HostFunc)[]>>(body, rawContext);

            return lambda.Compile();
        }

        private static Runtime.ValueType ValueTypeFor(Type type)
        {
            var valueType = (Runtime.ValueType)
                typeof(Value)
                    .GetMethod(nameof(Value.ValueType))
                    .MakeGenericMethod(type)
                    .Invoke(null, null);
            return valueType;
        }

        private static Expression /*(ApiFunc, HostFunc)*/
        ReflectHostFunc(string name, string module, MethodInfo method, ParameterExpression context)
        {
            ApiFunc apiData = new ApiFunc { Module = module, Name = name, };

            var funcCtor = GenerateHostFuncCtor(context, method, apiData);

            var tupleCtor = Expression.New(
                typeof(ValueTuple<ApiFunc, HostFunc>).GetConstructor(
                    new[] { typeof(ApiFunc), typeof(HostFunc) }
                ),
                Expression.Constant(apiData),
                funcCtor
            );

            return tupleCtor;
        }

        /// <summary>
        /// Creates an API adapter for the host function to wasm using reflection.
        ///
        /// State is shared between all instances, if the delegate is not static.
        /// </summary>
        /// <param name="name">The wasm exposed function name.</param>
        /// <param name="module">The wasm exposed module.</param>
        /// <param name="function">The method to wrap, all instances of the method have the same context object.</param>
        /// <returns>An API data object with a generator that returns func stubs.</returns>
        public static (ApiFunc, HostFunc) ReflectHostFunc(
            string name,
            string module,
            Delegate function
        )
        {
            var apiData = new ApiFunc { Module = module, Name = name, };

            // This is a parameter for the outer lambda, that is stored in the closure for the func.
            var context = Expression.Constant(function.Target);

            var funcCtor = GenerateHostFuncCtor(context, function.Method, apiData);

            var lambda = Expression.Lambda<Func<HostFunc>>(funcCtor);
            var func = lambda.Compile()();
            return (apiData, func);
        }

        private static Expression GenerateHostFuncCtor(
            Expression context,
            MethodInfo method,
            ApiFunc funcData
        )
        {
            // These are passed at every host func invocation.
            var machine = Expression.Parameter(typeof(Machine), "machine");
            var frame = Expression.Parameter(typeof(Frame), "frame");

            // Parameter Processing
            var argsCount = 0;
            var parameters = new List<Expression>();
            foreach (var param in method.GetParameters())
            {
                if (param.ParameterType == typeof(Machine))
                {
                    parameters.Add(machine);
                }
                else if (param.ParameterType == typeof(Frame))
                {
                    parameters.Add(frame);
                }
                else
                {
                    parameters.Add(
                        Expression.Call(
                            Expression.ArrayIndex(
                                Expression.PropertyOrField(frame, nameof(Frame.Locals)),
                                Expression.Constant(argsCount)
                            ),
                            typeof(Value)
                                .GetMethod(nameof(Value.As))
                                .MakeGenericMethod(param.ParameterType)
                        )
                    );

                    funcData.Parameters.Add(
                        new Parameter
                        {
                            Name = param.Name,
                            Type = ValueTypeFor(param.ParameterType),
                            CSType = param.ParameterType.GetNiceName()
                        }
                    );
                    funcData.ParameterValueTypes.Add(ValueTypeFor(param.ParameterType));

                    argsCount++;
                }
            }

            // The actual inner call.
            var result = Expression.Call(method.IsStatic ? null : context, method, parameters);

            var returnsCount = 0;
            if (method.ReturnType != typeof(void))
            {
                returnsCount++;
                // Process return value.
                result = Expression.Call(
                    frame,
                    typeof(Frame)
                        .GetMethods()
                        .First(m => m.Name == nameof(Frame.Push) && m.IsGenericMethod)
                        .MakeGenericMethod(method.ReturnType),
                    result
                );

                funcData.Returns.Add(
                    new Parameter
                    {
                        Type = ValueTypeFor(method.ReturnType),
                        CSType = method.ReturnType.GetNiceName()
                    }
                );
                funcData.ReturnValueTypes.Add(ValueTypeFor(method.ReturnType));

                // TODO: Add the ability to process value tuple based return values.
            }

            // This is the invoked callsite. To improve per-call performance, optimize the expression structure going into this compilation.
            var callImpl = Expression.Lambda<HostProxy>(result, machine, frame);

            var funcCtor = Expression.New(
                typeof(HostFunc).GetConstructors().First(),
                Expression.Constant(funcData.Module),
                Expression.Constant(funcData.Name),
                Expression.Constant(
                    new FuncType(
                        funcData.ParameterValueTypes.ToArray(),
                        funcData.ReturnValueTypes.ToArray()
                    )
                ),
                callImpl
            );

            return funcCtor;
        }
    }
}
