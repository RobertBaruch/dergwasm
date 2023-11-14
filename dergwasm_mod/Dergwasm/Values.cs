using System;
using System.Collections.Generic;

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
        EXTERNREF = 0x6F,
    }

    public enum ReferenceValueType : ulong
    {
        // A func reference, with the funcaddr in value_lo.
        // If value_lo contains 0xFFFFFFFFFFFFFFFF, it's a null reference.
        FUNCREF = 0,

        // Am extern reference, with the externaddr in value_lo.
        // If value_lo contains 0xFFFFFFFFFFFFFFFF, it's a null reference.
        EXTERNREF = 1,
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
    public struct Value
    {
        public ulong value_lo;
        public ulong value_hi;

        public Value(ulong value_lo, ulong value_hi)
        {
            this.value_lo = value_lo;
            this.value_hi = value_hi;
        }

        public Value(Value value)
        {
            value_lo = value.value_lo;
            value_hi = value.value_hi;
        }

        unsafe public Value(float f32)
        {
            fixed (ulong* ptr = &value_lo) { *(float*)ptr = f32; }
            value_hi = 0;
        }

        unsafe public Value(double f64)
        {
            fixed (ulong* ptr = &value_lo) { *(double*)ptr = f64; }
            value_hi = 0;
        }

        unsafe public Value(uint i32)
        {
            fixed (ulong* ptr = &value_lo) { *(uint*)ptr = i32; }
            value_hi = 0;
        }

        unsafe public Value(int i32)
        {
            fixed (ulong* ptr = &value_lo) { *(int*)ptr = i32; }
            value_hi = 0;
        }

        unsafe public Value(ulong i64)
        {
            value_lo = i64;
            value_hi = 0;
        }

        unsafe public Value(long i64)
        {
            fixed (ulong* ptr = &value_lo) { *(long*)ptr = i64; }
            value_hi = 0;
        }

        unsafe public float AsF32()
        {
            fixed (ulong* ptr = &value_lo) { return *(float*)ptr; }
        }

        unsafe public double AsF64()
        {
            fixed (ulong* ptr = &value_lo) { return *(double*)ptr; }
        }

        unsafe public uint AsI32_U()
        {
            fixed (ulong* ptr = &value_lo) { return *(uint*)ptr; }
        }

        unsafe public int AsI32_S()
        {
            fixed (ulong* ptr = &value_lo) { return *(int*)ptr; }
        }

        unsafe public ulong AsI64_U()
        {
            return value_lo;
        }

        unsafe public long AsI64_S()
        {
            fixed (ulong* ptr = &value_lo) { return *(long*)ptr; }
        }

        // Only valid if the value is a block operand.
        public uint GetTarget()
        {
            return (uint)(value_lo & 0xFFFFFFFF);
        }

        // Only valid if the value is a block operand.
        public uint GetElseTarget()
        {
            return (uint)(value_lo >> 32);
        }

        // Only valid if the value is a block operand.
        public BlockType GetBlockType()
        {
            return (BlockType)(value_hi & 0b11);
        }

        // Only valid if the value is a block operand with a TYPED_BLOCK signature.
        public uint GetReturningBlockTypeIndex()
        {
            return (uint)((value_hi >> 2) & 0xFFFFFFFF);
        }

        // Only valid if the value is a block operand with a RETURNING_BLOCK signature.
        public ValueType GetReturningBlockValueType()
        {
            return (ValueType)((value_hi >> 2) & 0xFF);
        }

        // Only valid if the value is a reference type.
        public ReferenceValueType GetRefType()
        {
            return (ReferenceValueType)value_hi;
        }

        // Only valid if the value is a reference type.
        public bool IsNullRef() { return value_lo == 0xFFFFFFFFFFFFFFFF; }

        // Only valid if the value is a reference type.
        public ulong AsRefAddr() { return value_lo; }
    }

    // A label. Created on entry to a block, and used to exit blocks.
    public struct Label
    {
        // The number of return values for the block.
        public uint arity;
        // The target PC for a BR 0 instruction within this block. With the exception of the
        // LOOP instruction, this always goes to the END+1 of the block. Targets for LOOP
        // instructions go back to the LOOP instruction.
        public uint target;
        // The size of the (value) stack at the moment the label is created.
        //
        // In the WASM spec, labels are stored on the stack for simplicity. This lets
        // instructions pop everything off the stack up to the label. That would mean that
        // a stack element would have to indicate that it is a label. Since we only allocate
        // 128 bits for a stack entry, and a stack entry could be a V128, there is way to
        // differentiate between a V128 and a label when blindly popping elements off the stack.
        //
        // Therefore, we keep labels on a separate label stack for each function -- since regardless
        // of how a function ends, all labels get removed.
        public uint stack_level;

        public Label(uint arity, uint target, uint stack_level)
        {
            this.arity = arity;
            this.target = target;
            this.stack_level = stack_level;
        }
    }

    // A frame. Represents the state of a function. Frames have their own stack. Frames are
    // also not skippable like blocks. That means you can't exit a function and continue to
    // anything other than the function in the previous frame. This is in contrast to blocks,
    // where you can break out of multiple levels of blocks.
    public class Frame
    {
        // The number of return values for the function.
        public uint arity;
        // The function's locals. This includes its arguments, which come first.
        public Value[] locals;
        // The module instance this frame is executing in.
        public IModule module;
        // The current program counter.
        public uint pc;
        // The label stack. We keep a function's label stack separate because labels, unlike values,
        // never travel across function boundaries.
        public Stack<Label> labels;
    }

    public struct FuncType
    {
        public ValueType[] args;
        public ValueType[] returns;
    }
}  // namespace Derg
