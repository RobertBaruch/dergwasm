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

    public class ReflectedModule : IHostModule
    {
        public List<HostFunc> Functions { get; }

        public List<ApiFunc> ApiData { get; }

        public ReflectedModule()
        {
            var reflected = GetReflectedFuncs();
            ApiData = reflected.Select(r => r.Item1).ToList();
            Functions = reflected.Select(r => r.Item2).ToList();
        }

        /// <summary>
        /// Override this method to provide functions directly, the default implementation gathers functions via reflection.
        /// </summary>
        protected virtual (ApiFunc, HostFunc)[] GetReflectedFuncs()
        {
            return ModuleReflector.ReflectHostFuncs(this);
        }
    }
}
