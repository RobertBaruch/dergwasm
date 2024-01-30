using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Elements.Core;

namespace Derg.Modules
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        public string DefaultModule { get; }

        public ModAttribute(string defaultModule = "env")
        {
            DefaultModule = defaultModule;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ModFnAttribute : Attribute
    {
        public string Name { get; }
        public string Module { get; }
        public Type[] Generics { get; }

        public ModFnAttribute(string name = null, string module = null, params Type[] generics)
        {
            Name = name;
            Module = module;
            Generics = generics;
        }

        public ModFnAttribute(string name, params Type[] generics)
        {
            Name = name;
            Generics = generics;
        }
    }

    internal class ApiData<T>
    {
        public Api Data { get; set; }
        public Func<T, HostFunc> Function { get; set; }
    }

    public class ReflectedModule<T> : IHostModule
    {
        private static readonly List<ApiData<T>> ReflectedFuncs;

        static ReflectedModule()
        {
            var modAttr = typeof(T).GetCustomAttribute<ModAttribute>();
            var defaultModule = modAttr?.DefaultModule ?? "env";

            ReflectedFuncs = new List<ApiData<T>>();
            foreach (
                var method in typeof(T).GetMethods(
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
                        $"{method} in {typeof(T)} is not public, but was declared as a module function. Please make it public."
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
                                $"Reflected module function {method} on {typeof(T)} attribute provided generic arguments, but the method is not generic."
                            );
                        }
                        boundMethod = boundMethod.MakeGenericMethod(attr.Generics);
                    }
                    ReflectedFuncs.Add(ReflectCallSite(attr, boundMethod, defaultModule));
                }
            }
        }

        private static ValueType ValueTypeFor(Type type)
        {
            var valueType = (ValueType)
                typeof(Value)
                    .GetMethod(nameof(Value.ValueType))
                    .MakeGenericMethod(type)
                    .Invoke(null, null);
            return valueType;
        }

        private static ApiData<T> ReflectCallSite(
            ModFnAttribute attr,
            MethodInfo method,
            string defaultModule
        )
        {
            ApiData<T> apiData = new ApiData<T>
            {
                Data = new Api
                {
                    Module = attr.Module ?? defaultModule,
                    Name = attr.Name ?? method.Name,
                }
            };

            // This is a parameter for the outer lambda, that is stored in the closure for the func.
            var context = Expression.Parameter(typeof(T), "context");

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

                    apiData
                        .Data
                        .Parameters
                        .Add(
                            new Parameter
                            {
                                Name = param.Name,
                                Type = ValueTypeFor(param.ParameterType),
                                CSType = param.ParameterType.GetNiceName()
                            }
                        );
                    apiData.Data.ParameterValueTypes.Add(ValueTypeFor(param.ParameterType));

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

                apiData
                    .Data
                    .Returns
                    .Add(
                        new Parameter
                        {
                            Type = ValueTypeFor(method.ReturnType),
                            CSType = method.ReturnType.GetNiceName()
                        }
                    );
                apiData.Data.ReturnValueTypes.Add(ValueTypeFor(method.ReturnType));

                // TODO: Add the ability to process value tuple based return values.
            }

            // This is the invoked callsite. To improve per-call performance, optimize the expression structure going into this compilation.
            var callImpl = Expression.Lambda<Action<Machine, Frame>>(result, machine, frame);

            var funcCtor = Expression.New(
                typeof(HostFunc).GetConstructors().First(),
                Expression.Constant(apiData.Data.Module),
                Expression.Constant(apiData.Data.Name),
                Expression.Constant(
                    new FuncType(
                        apiData.Data.ParameterValueTypes.ToArray(),
                        apiData.Data.ReturnValueTypes.ToArray()
                    )
                ),
                Expression.New(
                    typeof(ActionHostProxy).GetConstructors().First(),
                    callImpl,
                    Expression.Constant(argsCount),
                    Expression.Constant(returnsCount)
                )
            );

            var lambda = Expression.Lambda<Func<T, HostFunc>>(funcCtor, context);
            apiData.Function = lambda.Compile();
            return apiData;
        }

        public List<HostFunc> Functions { get; }
        public List<Api> ApiData { get; }

        public HostFunc this[string name]
        {
            get => Functions.First(f => f.Name == name);
        }

        public Api ApiDataFor(string name)
        {
            return ApiData.First(f => f.Name == name);
        }

        public ReflectedModule(T self)
        {
            Functions = ReflectedFuncs.Select(f => f.Function(self)).ToList();
            ApiData = ReflectedFuncs.Select(f => f.Data).ToList();
        }
    }
}
