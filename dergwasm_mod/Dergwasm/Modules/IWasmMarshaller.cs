using Derg.Runtime;
using Dergwasm.Runtime;
using Elements.Core;
using System;
using System.Collections.Generic;

namespace Derg.Modules {
    public interface IWasmMarshaller<T> {
        // Get the type information for a parameter, Name will be filled out later.
        void AddParams(string name, List<Parameter> parameters);
        void To(Frame frame, Machine machine, T value);
        T From(Frame frame, Machine machine);
    }

    public struct DirectMarshaller<T> : IWasmMarshaller<T>
    {

        public void AddParams(string name, List<Parameter> parameters)
        {
            parameters.Add(new Parameter
            {
                Name = name,
                Type = ModuleReflector.ValueTypeFor(typeof(T)),
                CSType = typeof(T).GetNiceName()
            });
        }

        public T From(Frame frame, Machine machine) => frame.Pop().As<T>();

        public void To(Frame frame, Machine machine, T value) => frame.Push(Value.From(value));
    }
}
