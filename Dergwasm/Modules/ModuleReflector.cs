using Dergwasm.Runtime;
using Elements.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dergwasm.Modules
{
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.Struct
    )]
    public class MarshalWithAttribute : Attribute
    {
        public Type Marshaller { get; }

        public MarshalWithAttribute(Type marshaller)
        {
            Marshaller = marshaller;
        }
    }

    public static class ModuleReflector
    {
        private static readonly ConcurrentDictionary<
            Type,
            Func<object, (ApiFunc, HostFunc)[]>
        > _reflectedFuncs = new ConcurrentDictionary<Type, Func<object, (ApiFunc, HostFunc)[]>>();

        public static Runtime.ValueType[] ValueTypesFor(Type type)
        {
            var valueTypes = (Runtime.ValueType[])
                typeof(Value)
                    .GetMethod(nameof(Value.ValueType))
                    .MakeGenericMethod(type)
                    .Invoke(null, null);
            return valueTypes;
        }

        public static Type MarshallerFor(ParameterInfo info)
        {
            var marshaller =
                info.GetCustomAttribute<MarshalWithAttribute>()?.Marshaller
                ?? info.ParameterType.GetCustomAttribute<MarshalWithAttribute>()?.Marshaller
                ?? typeof(DirectMarshaller<>).MakeGenericType(info.ParameterType);
            if (marshaller.IsByRef)
            {
                throw new InvalidOperationException("Marshallers must be structs.");
            }
            if (marshaller.IsGenericTypeDefinition)
            {
                // If the marshaller is a generic type, assume the marshaller has the same type arguments.
                marshaller = marshaller.MakeGenericType(info.ParameterType.GenericTypeArguments);
            }
            return marshaller;
        }

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

            var body = new List<Expression>();
            var callParams = new List<ParameterExpression>();
            var outVars = new List<ParameterExpression>();
            foreach (var param in method.GetParameters())
            {
                if (param.ParameterType == typeof(Machine))
                {
                    callParams.Add(machine);
                    continue;
                }
                else if (param.ParameterType == typeof(Frame))
                {
                    callParams.Add(frame);
                    continue;
                }

                // Declare a local var for the popped value.
                ParameterExpression poppedValue = Expression.Variable(
                    param.ParameterType,
                    param.Name
                );
                callParams.Add(poppedValue);
                outVars.Add(poppedValue);

                // Call the appropriate pop method for the type.
                Type marshaller = MarshallerFor(param);
                MethodInfo popper = marshaller.GetMethod(nameof(IWasmMarshaller<int>.From));
                Expression popperCaller = Expression.Assign(
                    poppedValue,
                    Expression.Call(Expression.Default(marshaller), popper, frame, machine)
                );
                body.Add(popperCaller);

                marshaller
                    .GetMethod(nameof(IWasmMarshaller<int>.AddParams))
                    .Invoke(
                        marshaller.GetDefault(),
                        new object[] { param.Name, funcData.Parameters }
                    );
            }

            // The poppers need to be reversed. The first value pushed is the first call arg,
            // which means that the first value popped is the *last* call arg.
            body.Reverse();

            // The actual inner call to the host func.
            var result = Expression.Call(method.IsStatic ? null : context, method, callParams);

            var returnsCount = 0;
            if (method.ReturnType != typeof(void))
            {
                returnsCount++;
                // Process return value.
                ParameterInfo param = method.ReturnParameter;
                Type marshaller = MarshallerFor(param);
                MethodInfo pusher = marshaller.GetMethod(nameof(IWasmMarshaller<int>.To));
                MethodCallExpression pusherCaller = Expression.Call(
                    Expression.Default(marshaller),
                    pusher,
                    frame,
                    machine,
                    result
                );

                result = pusherCaller;

                marshaller
                    .GetMethod(nameof(IWasmMarshaller<int>.AddParams))
                    .Invoke(marshaller.GetDefault(), new object[] { null, funcData.Returns });

                // TODO: Add the ability to process value tuple based return values.
            }
            body.Add(result);

            BlockExpression block = Expression.Block(outVars.ToArray(), body);

            // This is the invoked callsite. To improve per-call performance, optimize the expression
            // structure going into this compilation.
            var callImpl = Expression.Lambda<HostProxy>(block, machine, frame);

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
