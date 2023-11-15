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
        FUNCREF = 1,

        // Am extern reference, with the externaddr in value_lo.
        // If value_lo contains 0xFFFFFFFFFFFFFFFF, it's a null reference.
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

        public unsafe Value(float f32)
        {
            fixed (ulong* ptr = &value_lo)
            {
                *(float*)ptr = f32;
            }
            value_hi = 0;
        }

        public unsafe Value(double f64)
        {
            fixed (ulong* ptr = &value_lo)
            {
                *(double*)ptr = f64;
            }
            value_hi = 0;
        }

        public unsafe Value(uint i32)
        {
            fixed (ulong* ptr = &value_lo)
            {
                *(uint*)ptr = i32;
            }
            value_hi = 0;
        }

        public unsafe Value(int i32)
        {
            fixed (ulong* ptr = &value_lo)
            {
                *(int*)ptr = i32;
            }
            value_hi = 0;
        }

        public unsafe Value(ulong i64)
        {
            value_lo = i64;
            value_hi = 0;
        }

        public unsafe Value(long i64)
        {
            fixed (ulong* ptr = &value_lo)
            {
                *(long*)ptr = i64;
            }
            value_hi = 0;
        }

        public unsafe float AsF32()
        {
            fixed (ulong* ptr = &value_lo)
            {
                return *(float*)ptr;
            }
        }

        public unsafe double AsF64()
        {
            fixed (ulong* ptr = &value_lo)
            {
                return *(double*)ptr;
            }
        }

        public unsafe uint AsI32_U()
        {
            fixed (ulong* ptr = &value_lo)
            {
                return *(uint*)ptr;
            }
        }

        public unsafe int AsI32_S()
        {
            fixed (ulong* ptr = &value_lo)
            {
                return *(int*)ptr;
            }
        }

        public int Int() => AsI32_S();

        public unsafe ulong AsI64_U()
        {
            return value_lo;
        }

        public unsafe long AsI64_S()
        {
            fixed (ulong* ptr = &value_lo)
            {
                return *(long*)ptr;
            }
        }

        // Only valid if the value is a block operand.
        public int GetTarget()
        {
            return (int)(value_lo & 0xFFFFFFFF);
        }

        // Only valid if the value is a block operand.
        public int GetElseTarget()
        {
            return (int)(value_lo >> 32);
        }

        // Only valid if the value is a block operand.
        public BlockType GetBlockType()
        {
            return (BlockType)(value_hi & 0b11);
        }

        // Only valid if the value is a block operand with a TYPED_BLOCK signature.
        public int GetReturningBlockTypeIndex()
        {
            return (int)((value_hi >> 2) & 0xFFFFFFFF);
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
        public bool IsNullRef()
        {
            return value_lo == 0xFFFFFFFFFFFFFFFF;
        }

        // Only valid if the value is a reference type.
        public ulong AsRefAddr()
        {
            return value_lo;
        }

        public override string ToString()
        {
            return $"Value[hi={value_hi:X16}, lo={value_lo:X16}]";
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

        // The size of the (value) stack at the moment the label is created.
        //
        // Note: I do not believe the following is correct, since it seems that ending a
        // block does NOT necessarily leave only the block's arity extra values on the stack. I think
        // a valid program would only leave arity extra values on the stack.
        //
        // Incorrect:
        //
        // In the WASM spec, labels are stored on the stack for simplicity. This lets
        // instructions pop everything off the stack up to the label. That would mean that
        // a stack element would have to indicate that it is a label. Since we only allocate
        // 128 bits for a stack entry, and a stack entry could be a V128, there is way to
        // differentiate between a V128 and a label when blindly popping elements off the stack.
        //
        // Therefore, we keep labels on a separate label stack for each function -- since regardless
        // of how a function ends, all labels get removed.
        public int stack_level;

        public Label(int arity, int target, int stack_level)
        {
            this.arity = arity;
            this.target = target;
            this.stack_level = stack_level;
        }

        public override string ToString()
        {
            return $"Label[arity={arity}, target={target}, stack_level={stack_level}]";
        }
    }

    // A frame. Represents the state of a function. Frames have their own label and value stacks.
    // Frames are also not skippable like blocks. That means you can't exit a function and continue to
    // anything other than the function in the previous frame. This is in contrast to blocks,
    // where you can break out of multiple levels of blocks.
    public class Frame
    {
        // The number of return values for the function.
        public int arity;

        // The function's locals. This includes its arguments, which come first.
        public Value[] locals;

        // The module instance this frame is executing in.
        public IModule module;

        // The current program counter.
        public int pc;

        // The label stack. Labels never apply across function boundaries.
        public Stack<Label> label_stack;

        // The value stack. Values never apply across function boundaires. Return values
        // are handled explicitly by copying from stack to stack. Args are locals copied
        // from the caller's stack.
        public List<Value> value_stack;

        public Frame(int arity, Value[] locals, IModule module)
        {
            this.arity = arity;
            this.locals = locals;
            this.module = module;
            this.pc = 0;
            this.label_stack = new Stack<Label>();
            this.value_stack = new List<Value>();
        }
    }

    public struct FuncType
    {
        public ValueType[] args;
        public ValueType[] returns;

        public FuncType(ValueType[] args, ValueType[] returns)
        {
            this.args = args;
            this.returns = returns;
        }
    }
} // namespace Derg
