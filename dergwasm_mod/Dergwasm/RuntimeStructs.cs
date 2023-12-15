using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using LEB128;

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
        public ModuleInstance Module;

        // The current program counter.
        public int PC;

        // The label stack. Labels never apply across function boundaries.
        public Stack<Label> label_stack;

        // The value stack. Values never apply across function boundaires. Return values
        // are handled explicitly by copying from stack to stack. Args are locals copied
        // from the caller's stack.
        public List<Value> value_stack;

        public Frame(ModuleFunc func, ModuleInstance module)
        {
            if (func != null)
            {
                this.Locals = new Value[func.Signature.args.Length + func.Locals.Length];
            }
            this.Module = module;
            this.PC = 0;
            this.label_stack = new Stack<Label>();
            this.value_stack = new List<Value>();
            this.Func = func;
        }

        public virtual int Arity
        {
            get => Func.Signature.returns.Length;
        }

        public List<Instruction> Code
        {
            get => Func.Code;
        }

        public Value TopOfStack => value_stack.Last();

        public Value Pop()
        {
            Value top = value_stack.Last();
            value_stack.RemoveAt(value_stack.Count - 1);
            return top;
        }

        public unsafe T Pop<T>()
            where T : unmanaged
        {
            Value top = Pop();
            return *(T*)&top.value_lo;
        }

        public void Push(Value val) => value_stack.Add(val);

        public void Push(int val) => Push(new Value(val));

        public void Push(uint val) => Push(new Value(val));

        public void Push(long val) => Push(new Value(val));

        public void Push(ulong val) => Push(new Value(val));

        public void Push(float val) => Push(new Value(val));

        public void Push(double val) => Push(new Value(val));

        public void Push(bool val) => Push(new Value(val));

        public void Push<R>(R ret)
        {
            switch (ret)
            {
                case int r:
                    Push(r);
                    break;

                case uint r:
                    Push(r);
                    break;

                case long r:
                    Push(r);
                    break;

                case ulong r:
                    Push(r);
                    break;

                case float r:
                    Push(r);
                    break;

                case double r:
                    Push(r);
                    break;

                case bool r:
                    Push(r);
                    break;

                default:
                    throw new Trap($"Invalid push type {ret.GetType()}");
            }
        }

        public int StackLevel() => value_stack.Count;

        public void RemoveStack(int from_level, int arity)
        {
            value_stack.RemoveRange(from_level, value_stack.Count - from_level - arity);
        }

        public Label PopLabel() => label_stack.Pop();

        public Label Label
        {
            get => label_stack.Peek();
            set => label_stack.Push(value);
        }

        public bool HasLabel() => label_stack.Count > 0;
    }

    // A call frame for when we're calling a host function.
    public class HostFrame : Frame
    {
        // The function currently executing.
        public HostFunc HostFunc;

        public HostFrame(HostFunc func, ModuleInstance module)
            : base(null, module)
        {
            if (func != null)
            {
                this.Locals = new Value[func.Proxy.NumArgs()];
            }
            this.HostFunc = func;
        }

        public override int Arity
        {
            get => HostFunc.Proxy.Arity();
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

        public static Limits Read(BinaryReader stream)
        {
            byte flag = stream.ReadByte();
            if (flag == 0)
            {
                return new Limits((uint)stream.ReadLEB128Unsigned());
            }
            else if (flag == 1)
            {
                return new Limits(
                    (uint)stream.ReadLEB128Unsigned(),
                    (uint)stream.ReadLEB128Unsigned()
                );
            }
            else
            {
                throw new Trap("Invalid flag in limits");
            }
        }

        public bool Equals(Limits other)
        {
            return Minimum.Equals(other.Minimum) && Maximum.Equals(other.Maximum);
        }

        public override int GetHashCode()
        {
            return (Minimum, Maximum).GetHashCode();
        }

        public static bool operator ==(Limits lhs, Limits rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Limits lhs, Limits rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

    public class Func
    {
        // In general, all Funcs have a module name.
        public string ModuleName;

        // In general, all Funcs have a name. Exported funcs have explicit names, but
        // if a func is not exported, it will get a synthetic name $N where N is the
        // index.
        public string Name;
        public FuncType Signature;

        public Func(string moduleName, string name, FuncType signature)
        {
            ModuleName = moduleName;
            Name = name;
            Signature = signature;
        }
    }

    // This is just a temporary marker in non-instantiated modules, which will get
    // matched with an external func.
    public class ImportedFunc : Func
    {
        public ImportedFunc(string moduleName, string name, FuncType signature)
            : base(moduleName, name, signature) { }
    }

    // A WASM function.
    public class ModuleFunc : Func
    {
        public ModuleInstance Module; // The instance of the module where this func was defined.
        public ValueType[] Locals;
        public List<Instruction> Code;

        // Locals and Code get set later, when reading the module's code section.
        public ModuleFunc(string moduleName, string name, FuncType signature)
            : base(moduleName, name, signature) { }
    }

    // A function on the host.
    public class HostFunc : Func
    {
        public HostProxy Proxy;

        public HostFunc(string moduleName, string name, FuncType signature, HostProxy proxy)
            : base(moduleName, name, signature)
        {
            this.Proxy = proxy;
        }
    }

    public struct GlobalType
    {
        public ValueType Type;
        public bool Mutable;

        public GlobalType(ValueType valueType, bool mutable)
        {
            Type = valueType;
            Mutable = mutable;
        }

        public static GlobalType Read(BinaryReader stream)
        {
            byte value_type = stream.ReadByte();
            return new GlobalType((ValueType)value_type, stream.ReadByte() != 0);
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

        public static TableType Read(BinaryReader stream)
        {
            byte elem_type = stream.ReadByte();
            if (elem_type != (byte)ValueType.FUNCREF && elem_type != (byte)ValueType.EXTERNREF)
            {
                throw new Trap($"Invalid element type 0x{elem_type:X2}");
            }
            return new TableType(Limits.Read(stream), (ValueType)elem_type);
        }

        public bool Equals(TableType other)
        {
            return Limits.Equals(other.Limits) && ElementType.Equals(other.ElementType);
        }

        public override int GetHashCode()
        {
            return (Limits, ElementType).GetHashCode();
        }

        public static bool operator ==(TableType lhs, TableType rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TableType lhs, TableType rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

    public class Table
    {
        // In general, all Tables have a module name.
        public string ModuleName;

        // In general, all Tables have a name. Exported tables have explicit names, but
        // if a table is not exported, it will get a synthetic name $N where N is the
        // index.
        public string Name;

        public TableType Type;
        public Value[] Elements;

        public Table(string moduleName, string name, TableType type)
        {
            ModuleName = moduleName;
            Name = name;
            Type = type;
            Elements = new Value[Type.Limits.Minimum];
        }
    }

    // This is just a temporary marker in non-instantiated modules, which will get
    // matched with an external func.
    public class ImportedTable : Table
    {
        public ImportedTable(string moduleName, string name, TableType type)
            : base(moduleName, name, type) { }
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
}
