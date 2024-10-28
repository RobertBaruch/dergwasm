using System.Collections.Generic;
using System.IO;
using Dergwasm.Instructions;

namespace Dergwasm.Runtime
{
    // Minimum and optional maximum limits for resizable storage.
    public struct Limits
    {
        public uint Minimum;
        public uint? Maximum;

        public Limits(uint minimum)
        {
            Minimum = minimum;
            Maximum = new uint?();
        }

        public Limits(uint minimum, uint maximum)
        {
            Minimum = minimum;
            Maximum = new uint?(maximum);
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

        public override bool Equals(object other)
        {
            return other is Limits limits && Equals(limits);
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
            Proxy = proxy;
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

        public override bool Equals(object other)
        {
            return other is TableType tableType && Equals(tableType);
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
