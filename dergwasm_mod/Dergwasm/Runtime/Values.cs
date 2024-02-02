using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Derg.Resonite;
using Derg.Wasm;
using Derg.Instructions;
using Elements.Core;
using FrooxEngine;

namespace Derg.Runtime
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

    public static class ValueAccessor
    {
        private static readonly ConcurrentDictionary<Type, Delegate> valueGetters =
            new ConcurrentDictionary<Type, Delegate>();
        private static readonly ConcurrentDictionary<Type, Delegate> valueSetters =
            new ConcurrentDictionary<Type, Delegate>();

        static ValueAccessor()
        {
            // Identity
            Add(v => v, v => v);
            // Primitives
            Add(v => v.s32, v => new Value { s32 = v });
            Add(v => v.u32, v => new Value { u32 = v });
            Add(v => v.s64, v => new Value { s64 = v });
            Add(v => v.u64, v => new Value { u64 = v });
            Add(v => v.f32, v => new Value { f32 = v });
            Add(v => v.f64, v => new Value { f64 = v });
            Add(v => v.Bool, v => new Value { u32 = v ? 1u : 0u });
            // Complex Primitives
            Add(v => (ResoniteError)v.s32, v => new Value { s32 = (int)v });
            Add(v => new Ptr(v.s32), v => new Value { s32 = v.Addr });
            Add(v => new RefID(v.u64), v => new Value { u64 = (ulong)v });
            Add(v => new NullTerminatedString(v.s32), v => new Value { s32 = v.Data.Addr });
        }

        private static void Add<T>(Func<Value, T> getter, Func<T, Value> setter)
        {
            valueGetters.TryAdd(typeof(T), getter);
            valueSetters.TryAdd(typeof(T), setter);
        }

        private static Func<Value, T> CreateGetter<T>()
        {
            if (typeof(T) == typeof(NullTerminatedString))
            {
                var method = typeof(ValueAccessor).GetMethod(
                    nameof(NullTerminatedStringGetter),
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                return (Func<Value, T>)method.Invoke(null, null);
            }
            if (typeof(T).IsConstructedGenericType)
            {
                if (typeof(T).GetGenericTypeDefinition() == typeof(Ptr<>))
                {
                    var genericMethod = typeof(ValueAccessor).GetMethod(
                        nameof(PtrGetter),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    var method = genericMethod.MakeGenericMethod(typeof(T).GenericTypeArguments);
                    return (Func<Value, T>)method.Invoke(null, null);
                }
                if (typeof(T).GetGenericTypeDefinition() == typeof(WasmRefID<>))
                {
                    var genericMethod = typeof(ValueAccessor).GetMethod(
                        nameof(WRefIdGetter),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    var method = genericMethod.MakeGenericMethod(typeof(T).GenericTypeArguments);
                    return (Func<Value, T>)method.Invoke(null, null);
                }
            }
            throw new NotImplementedException();
        }

        private static Func<Value, Ptr<T>> PtrGetter<T>()
            where T : struct
        {
            return v => new Ptr<T>(v.s32);
        }

        private static Func<Value, WasmRefID<T>> WRefIdGetter<T>()
            where T : class, IWorldElement
        {
            return v => new WasmRefID<T>(v.u64);
        }

        private static Func<Value, NullTerminatedString> NullTerminatedStringGetter()
        {
            return v => new NullTerminatedString(v.s32);
        }

        private static Func<T, Value> CreateSetter<T>()
        {
            if (typeof(T) == typeof(NullTerminatedString))
            {
                var method = typeof(ValueAccessor).GetMethod(
                    nameof(NullTerminatedStringSetter),
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                return (Func<T, Value>)method.Invoke(null, null);
            }
            if (typeof(T).IsConstructedGenericType)
            {
                if (typeof(T).GetGenericTypeDefinition() == typeof(Ptr<>))
                {
                    var genericMethod = typeof(ValueAccessor).GetMethod(
                        nameof(PtrSetter),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    var method = genericMethod.MakeGenericMethod(typeof(T).GenericTypeArguments);
                    return (Func<T, Value>)method.Invoke(null, null);
                }
                if (typeof(T).GetGenericTypeDefinition() == typeof(WasmRefID<>))
                {
                    var genericMethod = typeof(ValueAccessor).GetMethod(
                        nameof(WRefIdSetter),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    var method = genericMethod.MakeGenericMethod(typeof(T).GenericTypeArguments);
                    return (Func<T, Value>)method.Invoke(null, null);
                }
            }
            throw new NotImplementedException();
        }

        private static Func<Ptr<T>, Value> PtrSetter<T>()
            where T : struct
        {
            return v => new Value { s32 = v.Addr };
        }

        private static Func<WasmRefID<T>, Value> WRefIdSetter<T>()
            where T : class, IWorldElement
        {
            return v => new Value { u64 = v.Id };
        }

        private static Func<NullTerminatedString, Value> NullTerminatedStringSetter()
        {
            return v => new Value { s32 = v.Data.Addr };
        }

        public static Func<Value, T> GetConverter<T>()
        {
            return (Func<Value, T>)valueGetters.GetOrAdd(typeof(T), k => CreateGetter<T>());
        }

        public static Func<T, Value> SetConverter<T>()
        {
            return (Func<T, Value>)valueSetters.GetOrAdd(typeof(T), k => CreateSetter<T>());
        }
    }

    /// <summary>
    /// A wrapper for <see cref="ValueAccessor"/> that stores retrieved accessors in static fields.
    /// These fields will be built and accessed once, then retrieved in constant time.
    /// </summary>
    public static class ValueAccessor<T>
    {
        private static readonly Func<Value, T> _getter = ValueAccessor.GetConverter<T>();
        private static readonly Func<T, Value> _setter = ValueAccessor.SetConverter<T>();

        public static T Get(in Value val) => _getter(val);

        public static Value Set(in T val) => _setter(val);
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

        // This is slightly more expensive than accessing the correct value directly --
        // 1.2x the time. If you already know the type of the value you're popping, then
        // extract it yourself.
        public T As<T>()
        {
            return ValueAccessor<T>.Get(this);
        }

        public static Value From<T>(T value)
        {
            return ValueAccessor<T>.Set(value);
        }

        public static unsafe ValueType ValueType<T>()
        {
            if (typeof(T) == typeof(float))
            {
                return Runtime.ValueType.F32;
            }
            else if (typeof(T) == typeof(double))
            {
                return Runtime.ValueType.F64;
            }
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            else if (sizeof(T) <= 4)
            {
                return Runtime.ValueType.I32;
            }
            else if (sizeof(T) == 8)
            {
                return Runtime.ValueType.I64;
            }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            throw new Exception($"Unknown type {typeof(T)}");
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
        public int GetReturningBlockTypeIndex() => (int)(value_hi >> 2 & 0xFFFFFFFF);

        // Only valid if the value is a block operand with a RETURNING_BLOCK signature.
        public ValueType GetReturningBlockValueType() => (ValueType)(value_hi >> 2 & 0xFF);

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

        public override bool Equals(object other)
        {
            return other is FuncType funcType && Equals(funcType);
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
            return $"({string.Join(", ", args ?? Array.Empty<ValueType>())}) -> ({string.Join(", ", returns ?? Array.Empty<ValueType>())})";
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
