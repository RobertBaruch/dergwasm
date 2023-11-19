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

        public Frame(ModuleFunc func, IModule module)
        {
            this.Locals = new Value[func.Signature.args.Length + func.Locals.Length];
            this.Module = module;
            this.pc = 0;
            this.label_stack = new Stack<Label>();
            this.value_stack = new List<Value>();
            this.Func = func;
        }
    }

    // Minimum and optional maximum limits for resizable storage.
    public struct Limits
    {
        public uint Minimum;
        public Nullable<uint> Maximum;

        public Limits(uint minimum)
        {
            Minimum = minimum;
            Maximum = new Nullable<uint>();
        }

        public Limits(uint minimum, uint maximum)
        {
            Minimum = minimum;
            Maximum = new Nullable<uint>(maximum);
        }
    }

    public class Func
    {
        public FuncType Signature;
    }

    public class ModuleFunc : Func
    {
        public ValueType[] Locals;
        public List<Instruction> Code;

        public ModuleFunc(FuncType signature, ValueType[] locals, List<Instruction> code)
        {
            Signature = signature;
            Locals = locals;
            Code = code;
        }
    }

    public struct TableType
    {
        public Limits Limits;

        // The WASM spec currently only allows reference types (funcref/externref).
        public ValueType ElementType;

        public TableType(Limits limits, ValueType elementType)
        {
            Limits = limits;
            ElementType = elementType;
        }
    }

    public class Table
    {
        public TableType Type;
        public Value[] Elements;

        public Table(TableType type)
        {
            Type = type;
            Elements = new Value[Type.Limits.Minimum];
        }
    }

    public class ElementSegment
    {
        public ValueType Type; // One of the reference types.
        public Value[] Elements;

        public ElementSegment(ValueType type, Value[] elements)
        {
            Type = type;
            Elements = elements;
        }
    }

    public class Memory
    {
        public Limits Limits;
        public byte[] Data;

        public Memory(Limits limits)
        {
            Limits = limits;
            Data = new byte[Limits.Minimum << 16];
        }
    }

    public class DataSegment
    {
        public byte[] Data;

        public DataSegment(byte[] data)
        {
            Data = data;
        }
    }
}
