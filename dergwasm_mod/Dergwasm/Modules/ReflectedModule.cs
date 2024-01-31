using System;
using System.Collections.Generic;
using System.Linq;
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
                    ReflectedFuncs.Add(ModuleReflector.ReflectHostFunc<T>(attr.Name ?? boundMethod.Name, attr.Module ?? defaultModule, boundMethod));
                }
            }
        }

        public List<HostFunc> Functions { get; }
        public List<ApiFunc> ApiData { get; }

        public HostFunc this[string name]
        {
            get => Functions.First(f => f.Name == name);
        }

        public ApiFunc ApiDataFor(string name)
        {
            return ApiData.First(f => f.Name == name);
        }

        public ReflectedModule(T self)
        {
            Functions = ReflectedFuncs.Select(f => f.FuncFactory(self)).ToList();
            ApiData = ReflectedFuncs.Select(f => f.Data).ToList();
        }
    }
}
