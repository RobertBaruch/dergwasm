using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Derg.Mem;

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

    [AttributeUsage(AttributeTargets.Method)]
    public class ModFnAttribute : Attribute
    {
        public string Name { get; }
        public string Module { get; }

        public ModFnAttribute(string name = null, string module = null)
        {
            Name = name;
            Module = module;
        }
    }

    public class ReflectedModule<T> : IHostModule where T : class
    {
        private static readonly List<Func<T, HostFunc>> ReflectedFuncs;

        static ReflectedModule()
        {
            var modAttr = typeof(T).GetCustomAttribute<ModAttribute>();
            var defaultModule = modAttr?.DefaultModule ?? "env";

            ReflectedFuncs = new List<Func<T, HostFunc>>();
            foreach (var method in typeof(T).GetMethods())
            {
                var modFnAttr = method.GetCustomAttribute<ModFnAttribute>();
                if (modFnAttr == null)
                {
                    continue;
                }

                ReflectedFuncs.Add(ReflectCallSite(modFnAttr, method, defaultModule));
            }
        }

        private static Func<T, HostFunc> ReflectCallSite(ModFnAttribute attr, MethodInfo method, string defaultModule)
        {
            var name = attr.Name ?? method.Name;
            var module = attr.Module ?? defaultModule;

            // This is a parameter for the outer lambda, that is stored in the closure for the func.
            var context = Expression.Parameter(typeof(T), "context");

            // These are passed at every host func invocation.
            var machine = Expression.Parameter(typeof(Machine), "machine");
            var frame = Expression.Parameter(typeof(Frame), "frame");

            // Parameter Processing
            var argsCount = 0;
            var variables = new List<ParameterExpression>();
            var bodies = new List<Expression>();
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
                    argsCount++;

                    var variable = Expression.Variable(param.ParameterType, param.Name);

                    var marshallerType = MarshalManager.GetMarshallerFor(param) ?? throw new InvalidOperationException($"No marshaller for {param.ParameterType}");
                    var value = Expression.Call(
                        Expression.New(marshallerType), marshallerType.GetMethod(nameof(IMarshaller<int>.FromValue)),
                        Expression.Call(frame, typeof(Frame).GetMethods().First(m => m.Name == "Pop" && !m.IsGenericMethod)),
                        machine, frame);

                    bodies.Add(Expression.Assign(variable, value));
                    variables.Add(variable);

                    parameters.Add(variable);
                }
            }

            var returnsCount = 0;
            if (method.ReturnType == typeof(void))
            {
                // The actual inner call.
                Expression.Call(context, method, parameters);
            }
            else
            {
                var callResult = Expression.Variable(method.ReturnType);
                variables.Add(callResult);
                // The actual inner call.
                bodies.Add(Expression.Assign(callResult, Expression.Call(context, method, parameters)));

                returnsCount++;
                // Process return value.
                var marshallerType = MarshalManager.GetReturnMarshallerFor(method);
                bodies.Add(
                    Expression.Call(frame, typeof(Frame).GetMethod("Push", new[] { typeof(Value) }),
                        Expression.Call(Expression.New(marshallerType), marshallerType.GetMethod(nameof(IMarshaller<int>.ToValue)), callResult, machine, frame)));

                // TODO: Add the ability to process value tuple based return values.
            }

            var callImpl = Expression.Lambda<Action<Machine, Frame>>(Expression.Block(variables, bodies), machine, frame);

            var funcCtor = Expression.New(typeof(HostFunc).GetConstructors().First(),
                Expression.Constant(module),
                Expression.Constant(name),
                Expression.Constant(new FuncType()),
                Expression.New(typeof(ActionHostProxy).GetConstructors().First(),
                    callImpl,
                    Expression.Constant(argsCount),
                    Expression.Constant(returnsCount))
                );

            var lambda = Expression.Lambda<Func<T, HostFunc>>(funcCtor, context);
            return lambda.Compile();
        }

        public List<HostFunc> Functions { get; }

        public ReflectedModule(T self)
        {
            Functions = ReflectedFuncs.Select(f => f(self)).ToList();
        }
    }
}