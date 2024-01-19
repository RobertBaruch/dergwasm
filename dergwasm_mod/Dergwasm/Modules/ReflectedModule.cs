using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Derg.Mem;

namespace Derg.Modules
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModAttribute : Attribute
    {
        public string DefaultModule { get; }

        public ModAttribute(string defaultModule = "env")
        {
            DefaultModule = defaultModule;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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

    public class ReflectedModule<T> where T : class
    {
        private static readonly List<Func<T, HostFunc>> _reflectedFuncs;

        static ReflectedModule()
        {
            var modAttr = typeof(T).GetCustomAttribute<ModAttribute>();
            var defaultModule = modAttr?.DefaultModule ?? "env";

            _reflectedFuncs = new List<Func<T, HostFunc>>();
            foreach (var method in typeof(T).GetMethods())
            {
                var modFnAttr = method.GetCustomAttribute<ModFnAttribute>();
                if (modFnAttr == null)
                {
                    continue;
                }

                _reflectedFuncs.Add(ReflectCallSite(modFnAttr, method, defaultModule));
            }
        }

        private static Func<T, HostFunc> ReflectCallSite(ModFnAttribute attr, MethodInfo method, string defaultModule)
        {
            var name = attr.Name ?? method.Name;
            var module = attr.Module ?? defaultModule;

            var context = Expression.Parameter(typeof(T), "context");

            var machine = Expression.Parameter(typeof(Machine), "machine");
            var frame = Expression.Parameter(typeof(Frame), "frame");
            var ctxExp = Expression.Variable(typeof(MemoryContext), "ctx");

            var argsCount = 0;
            var parameters = new List<Expression>();
            foreach (var param in method.GetParameters())
            {
                if (param.ParameterType == typeof(MemoryContext))
                {
                    parameters.Add(ctxExp);
                }
                else if (param.ParameterType == typeof(Machine))
                {
                    parameters.Add(machine);
                }
                else if (param.ParameterType == typeof(Frame))
                {
                    parameters.Add(frame);
                }
                else
                {
                    var marshallerType = MarshalManager.GetMarshallerFor(param) ?? throw new InvalidOperationException($"No marshaller for {param.ParameterType}");
                    var value = Expression.Call(
                        Expression.New(marshallerType), marshallerType.GetMethod(nameof(IMarshaller<int>.FromValue)),
                        Expression.Call(frame, typeof(Frame).GetMethods().First(m => m.Name == "Pop" && !m.IsGenericMethod)),
                        ctxExp);
                    argsCount++;
                    parameters.Add(value);
                }
            }

            var body = Expression.Block(new[] { ctxExp },
                Expression.Assign(ctxExp, Expression.New(typeof(MemoryContext).GetConstructor(new[] { typeof(Machine), typeof(Frame) }), machine, frame)),
                Expression.Call(context, method, parameters));

            var funcCtor = Expression.New(typeof(HostFunc).GetConstructors().First(),
                Expression.Constant(name),
                Expression.Constant(module),
                Expression.Constant(new FuncType()),
                Expression.New(typeof(CalculatedHostProxy).GetConstructors().First(),
                    Expression.Lambda<Action<Machine, Frame>>(body, machine, frame),
                    Expression.Constant(argsCount),
                    Expression.Constant(/*arity*/0))
                );

            var lambda = Expression.Lambda<Func<T, HostFunc>>(funcCtor, context);

            return lambda.Compile();
        }

        public Memory<HostFunc> Functions { get; }

        public ReflectedModule(T self)
        {
            Functions = new Memory<HostFunc>(_reflectedFuncs.Select(f => f(self)).ToArray());
        }
    }
}