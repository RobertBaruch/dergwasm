﻿using Elements.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Derg.Modules
{
    public class ApiData<T>
    {
        public ApiFunc Data { get; set; }
        public Func<T, HostFunc> FuncFactory { get; set; }
    }

    public class ApiData
    {
        public ApiFunc Data { get; set; }
        public Func<HostFunc> FuncFactory { get; set; }
    }

    public static class ModuleReflector
    {
        private static ValueType ValueTypeFor(Type type)
        {
            var valueType = (ValueType)
                typeof(Value)
                    .GetMethod(nameof(Value.ValueType))
                    .MakeGenericMethod(type)
                    .Invoke(null, null);
            return valueType;
        }

        /// <summary>
        /// Creates an API adapter for the host function to wasm using reflection.
        /// </summary>
        /// <typeparam name="T">The context object used to generate implementations of the wrapper.</typeparam>
        /// <param name="name">The wasm exposed function name.</param>
        /// <param name="module">The wasm exposed module.</param>
        /// <param name="method">The method to stub, this must either be an instance method of <typeparamref name="T"/> or static.</param>
        /// <returns>An API data object with a generator that returns func stubs.</returns>
        public static ApiData<T> ReflectHostFunc<T>(
            string name,
            string module,
            MethodInfo method
        )
        {
            ApiData<T> apiData = new ApiData<T>
            {
                Data = new ApiFunc
                {
                    Module = module,
                    Name = name,
                }
            };

            // This is a parameter for the outer lambda, that is stored in the closure for the func.
            var context = Expression.Parameter(typeof(T), "context");

            var funcCtor = GenerateHostFuncCtor(context, method, apiData.Data);

            var lambda = Expression.Lambda<Func<T, HostFunc>>(funcCtor, context);
            apiData.FuncFactory = lambda.Compile();
            return apiData;
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
        public static ApiData ReflectHostFunc(string name, string module, Delegate function)
        {
            ApiData apiData = new ApiData
            {
                Data = new ApiFunc
                {
                    Module = module,
                    Name = name,
                }
            };

            // This is a parameter for the outer lambda, that is stored in the closure for the func.
            var context = Expression.Constant(function.Target);

            var funcCtor = GenerateHostFuncCtor(context, function.Method, apiData.Data);

            var lambda = Expression.Lambda<Func<HostFunc>>(funcCtor);
            apiData.FuncFactory = lambda.Compile();
            return apiData;
        }

        private static Expression GenerateHostFuncCtor(Expression context, MethodInfo method, ApiFunc funcData)
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

                    funcData
                        .Parameters
                        .Add(
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

                funcData
                    .Returns
                    .Add(
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
