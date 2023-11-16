using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // A frame. Represents the state of a function. Frames have their own label and value stacks.
    // Frames are also not skippable like blocks. That means you can't exit a function and continue to
    // anything other than the function in the previous frame. This is in contrast to blocks,
    // where you can break out of multiple levels of blocks.
    public class Frame
    {
        // The function currently executing.
        public ModuleFunc Func;

        // The function's locals. This includes its arguments, which come first.
        public Value[] Locals;

        // The module instance this frame is executing in.
        public IModule Module;

        // The current program counter.
        public int pc;

        // The label stack. Labels never apply across function boundaries.
        public Stack<Label> label_stack;

        // The value stack. Values never apply across function boundaires. Return values
        // are handled explicitly by copying from stack to stack. Args are locals copied
        // from the caller's stack.
        public List<Value> value_stack;

        public int Arity
        {
            get => Func.Signature.returns.Length;
        }

        public List<Instruction> Code
        {
            get => Func.Code;
        }

        public Frame(Value[] locals, IModule module)
        {
            this.Locals = locals;
            this.Module = module;
            this.pc = 0;
            this.label_stack = new Stack<Label>();
            this.value_stack = new List<Value>();
        }
    }

    public class Func { }

    public class ModuleFunc : Func
    {
        public FuncType Signature;
        public ValueType[] Locals;
        public List<Instruction> Code;

        public ModuleFunc(FuncType signature, ValueType[] locals, List<Instruction> code)
        {
            Signature = signature;
            Locals = locals;
            Code = code;
        }
    }
}
