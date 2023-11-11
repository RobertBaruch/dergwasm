using System;

namespace Derg
{
    // A value on the stack. It is fixed to 128 bits long, which can store
    // any of I32, I64, F32, F64, V128, or a ref type. Since WASM modules
    // are supposed to be validated, and validation includes checking that
    // all operations on the stack are performed on the correct types, it
    // means that we don't have to store type information.
    //
    // If an operation consumes a reference type, then the reference is
    // encoded as follows:
    //
    // value_lo contains either 0xFFFFFFFFFFFFFFFF (a null reference) or
    // the reference address.
    //
    // value_hi contains either 0 (a func reference) or 1 (an extern reference).
    public struct Value
    {
        public ulong value_lo;
        public ulong value_hi;

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

        public bool IsNullRef() { return value_lo == 0xFFFFFFFFFFFFFFFF; }
        public bool IsFuncRef() { return value_hi == 0; }
        public bool IsExternRef() { return value_hi == 1; }
        public ulong AsRefAddr() { return value_lo; }
    }

    // A label. Created on entry to a block, and used to exit blocks.
    public struct Label
    {
        // The number of return values for the block.
        public int arity;
        // The target PC for a BR 0 instruction within this block. With
        // the exception of the LOOP instruction, this always goes to the
        // END (or ELSE in the case of an IF instruction's positive condition)
        // of the block.
        public int target;
    }

    // A frame. Represents the state of a function.
    public class Frame
    {
        // The number of return values for the function.
        public int arity;
        // The function's locals. This includes its arguments, which come first.
        public Value[] locals;
        // TODO: This should be an interface.
        public object module_inst;
        // The current program counter.
        public int pc;
    }
}  // namespace Derg
