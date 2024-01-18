using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LEB128;

namespace Derg
{
    // Encodings for value types.
    public enum ValueType : byte
    {
        I32 = 0x7F,
        I64 = 0x7E,
        F32 = 0x7D,
        F64 = 0x7C,
        V128 = 0x7B,
        FUNCREF = 0x70,
        EXTERNREF = 0x6F
    }

    // Reference value types. Note that a null ref is encoded as value_lo = 0, value_hi = 0.
    public enum ReferenceValueType : ulong
    {
        // A func reference, with the funcaddr in value_lo.
        FUNCREF = 1,

        // Am extern reference, with the externaddr in value_lo.
        EXTERNREF = 2,
    }

    // For block operands, the low 2 bits of value_hi show how its type signature can
    // be obtained. The other 62 bits (value_hi >> 2) is the type value, explained below.
    //
    // Block operands also contain targets (program counters to jump to). The normal
    // end-of-block target is encoded in the low 32 bits of value_lo. If there's an
    // ELSE clause, the else target is encoded in the high 32 bits of value_lo.
    public enum BlockType : ulong
    {
        // A signature where value_hi >> 2 is the type index into the function types table.
        TYPED_BLOCK = 0,

        // A void(void) signature.
        VOID_BLOCK = 1,

        // A type(void) signature, where value_hi >> 2 is the ValueType for the type.
        RETURNING_BLOCK = 2,
    }

    // A value. It is fixed to 128 bits long, which can store
    // a numeric value (any of I32, I64, F32, F64, V128), or a non-numeric value
    // (a reference or a block operand). Since WASM modules are supposed to be validated,
    // and validation includes checking that all operations on the stack are performed
    // on the correct types, it means that we don't have to store type information.
    //
    // Note: block operands are not values on the stack, but rather values in a block's operands.
    [StructLayout(LayoutKind.Explicit)]
    public struct Value
    {
        [FieldOffset(0)]
        public int s32;

        [FieldOffset(0)]
        public uint u32;

        [FieldOffset(0)]
        public long s64;

        [FieldOffset(0)]
        public ulong u64;

        [FieldOffset(0)]
        public float f32;

        [FieldOffset(0)]
        public double f64;

        [FieldOffset(8)]
        public ulong value_hi;

        // This is highly expensive because it does boxing and unboxing. It takes about
        // 4.5x the time to get a value this way. If you already know the type of the
        // value you're popping, then extract it yourself.
        public T As<T>()
            where T : unmanaged
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    return (T)Convert.ChangeType(s32, typeof(T));

                case TypeCode.UInt32:
                    return (T)Convert.ChangeType(u32, typeof(T));

                case TypeCode.Int64:
                    return (T)Convert.ChangeType(s64, typeof(T));

                case TypeCode.UInt64:
                    return (T)Convert.ChangeType(u64, typeof(T));

                case TypeCode.Single:
                    return (T)Convert.ChangeType(f32, typeof(T));

                case TypeCode.Double:
                    return (T)Convert.ChangeType(f64, typeof(T));

                case TypeCode.Boolean:
                    return (T)Convert.ChangeType(Bool, typeof(T));

                default:
                    throw new Trap($"Invalid Value.As type {Type.GetTypeCode(typeof(T))}");
            }
        }

        public static ValueType ValueType<T>()
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return Derg.ValueType.I32;
                case TypeCode.Byte:
                    return Derg.ValueType.I32;
                case TypeCode.SByte:
                    return Derg.ValueType.I32;
                case TypeCode.Int16:
                    return Derg.ValueType.I32;
                case TypeCode.UInt16:
                    return Derg.ValueType.I32;
                case TypeCode.Int32:
                    return Derg.ValueType.I32;
                case TypeCode.UInt32:
                    return Derg.ValueType.I32;
                case TypeCode.Int64:
                    return Derg.ValueType.I64;
                case TypeCode.UInt64:
                    return Derg.ValueType.I64;
                case TypeCode.Single:
                    return Derg.ValueType.F32;
                case TypeCode.Double:
                    return Derg.ValueType.F64;
                default:
                    throw new Exception($"Unknown type {typeof(T)}");
            }
        }

        // Bools are represented as unsigned ints.
        public bool Bool => u32 != 0;

        // Only valid if the value is a block operand.
        public int GetTarget() => (int)(u64 & 0xFFFFFFFF);

        // Only valid if the value is a block operand.
        public int GetElseTarget() => (int)(u64 >> 32);

        // Only valid if the value is a block operand.
        public BlockType GetBlockType() => (BlockType)(value_hi & 0b11);

        // Only valid if the value is a block operand with a TYPED_BLOCK signature.
        public int GetReturningBlockTypeIndex() => (int)((value_hi >> 2) & 0xFFFFFFFF);

        // Only valid if the value is a block operand with a RETURNING_BLOCK signature.
        public ValueType GetReturningBlockValueType() => (ValueType)((value_hi >> 2) & 0xFF);

        // Only valid if the value is a reference type.
        public ReferenceValueType GetRefType() => (ReferenceValueType)value_hi;

        // Only valid if the value is a reference type.
        public bool IsNullRef() => u64 == 0 && value_hi == 0;

        // Only valid if the value is a reference type.
        public int RefAddr => s32;

        public static Value RefOfFuncAddr(int addr) =>
            new Value { u64 = (ulong)addr, value_hi = (ulong)ReferenceValueType.FUNCREF };

        public static Value RefOfExternAddr(int addr) =>
            new Value { u64 = (ulong)addr, value_hi = (ulong)ReferenceValueType.EXTERNREF };

        public override string ToString()
        {
            return $"Value[hi={value_hi:X16}, u64={u64:X16}]";
        }
    }

    // A label. Created on entry to a block, and used to exit blocks.
    public struct Label
    {
        // The number of return values for the block.
        public int arity;

        // The target PC for a BR 0 instruction within this block. With the exception of the
        // LOOP instruction, this always goes to the END+1 of the block. Targets for LOOP
        // instructions go back to the LOOP instruction.
        public int target;

        public Label(int arity, int target)
        {
            this.arity = arity;
            this.target = target;
        }

        public override string ToString()
        {
            return $"Label[arity={arity}, target={target}]";
        }
    }

    public struct FuncType : IEquatable<FuncType>
    {
        public ValueType[] args;
        public ValueType[] returns;

        public FuncType(ValueType[] args, ValueType[] returns)
        {
            this.args = args;
            this.returns = returns;
        }

        public static FuncType Read(BinaryReader stream)
        {
            byte tag = stream.ReadByte();
            if (tag != 0x60)
            {
                throw new Trap($"Expected 0x60 tag for functype, but got 0x{tag:X2}");
            }
            int num_args = (int)stream.ReadLEB128Unsigned();
            ValueType[] args = new ValueType[num_args];
            for (int i = 0; i < num_args; i++)
            {
                args[i] = (ValueType)stream.ReadByte();
            }
            int num_returns = (int)stream.ReadLEB128Unsigned();
            ValueType[] returns = new ValueType[num_returns];
            for (int i = 0; i < num_returns; i++)
            {
                returns[i] = (ValueType)stream.ReadByte();
            }
            FuncType funcType = new FuncType(args, returns);
            // Console.WriteLine($"Read functype: {funcType}");
            return funcType;
        }

        public bool Equals(FuncType other)
        {
            return args.SequenceEqual(other.args) && returns.SequenceEqual(other.returns);
        }

        public override int GetHashCode()
        {
            return (args, returns).GetHashCode();
        }

        public static bool operator ==(FuncType lhs, FuncType rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(FuncType lhs, FuncType rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override string ToString()
        {
            return $"({string.Join(", ", args)}) -> ({string.Join(", ", returns)})";
        }
    }

    public static class ValueExtensions
    {
        public static bool IsRefType(this ValueType v)
        {
            return v == ValueType.FUNCREF || v == ValueType.EXTERNREF;
        }
    }
} // namespace Derg
