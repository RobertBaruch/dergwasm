﻿using System;
using Derg.Runtime;
using Dergwasm.Runtime;

namespace Derg.Instructions
{
    public static class NumericInstructions
    {
        public static void Const(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(instruction.Operands[0]);

        private static int clz(ulong value, int numbits)
        {
            // .Net Framework 4.7.2 doesn't have a built-in clz, so we have to do it ourselves.
            // There are O(log N) algorithms for this, but it's not worth it -- I don't think
            // a lot of software is really going to use this instruction.
            int count = 0;
            ulong mask = 1UL << numbits - 1;
            while (mask != 0 && (value & mask) == 0)
            {
                count++;
                mask >>= 1;
            }
            return count;
        }

        private static int ctz(ulong value, int numbits)
        {
            // .Net Framework 4.7.2 doesn't have a built-in ctz, so we have to do it ourselves.
            // There are O(log N) algorithms for this, but it's not worth it -- I don't think
            // a lot of software is really going to use this instruction.
            int count = 0;
            ulong mask = 1UL;
            ulong stop_mask = numbits == 64 ? 0 : 1UL << numbits;
            while (mask != stop_mask && (value & mask) == 0)
            {
                count++;
                mask <<= 1;
            }
            return count;
        }

        private static int popcnt(ulong value)
        {
            int count = 0;
            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        // Count leading zeros.
        public static void I32Clz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(clz(frame.Pop().u32, 32));

        // Count leading zeros.
        public static void I64Clz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(clz(frame.Pop().u64, 64));

        // Count trailing zeros.
        public static void I32Ctz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(ctz(frame.Pop().u32, 32));

        // Count trailing zeros.
        public static void I64Ctz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(ctz(frame.Pop().u64, 64));

        // Count ones.
        public static void I32Popcnt(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(popcnt(frame.Pop().u32));

        // Count ones.
        public static void I64Popcnt(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(popcnt(frame.Pop().u64));

        public static void I32Add(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 + c2);
        }

        public static void I64Add(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 + c2);
        }

        public static void I32Sub(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 - c2);
        }

        public static void I64Sub(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 - c2);
        }

        public static void I32Mul(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 * c2);
        }

        public static void I64Mul(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 * c2);
        }

        public static void I32DivU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            try
            {
                frame.Push(c1 / c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i32.div_u: {e.Message}");
            }
        }

        public static void I64DivU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            try
            {
                frame.Push(c1 / c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i64.div_u: {e.Message}");
            }
        }

        public static void I32DivS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            try
            {
                frame.Push(c1 / c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i32.div_s: {e.Message}");
            }
        }

        public static void I64DivS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            try
            {
                frame.Push(c1 / c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i64.div_s: {e.Message}");
            }
        }

        public static void I32RemU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            try
            {
                frame.Push(c1 % c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i32.rem_u: {e.Message}");
            }
        }

        public static void I64RemU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            try
            {
                frame.Push(c1 % c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i64.rem_u: {e.Message}");
            }
        }

        public static void I32RemS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            try
            {
                frame.Push(c1 % c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i32.rem_s: {e.Message}");
            }
        }

        public static void I64RemS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            try
            {
                frame.Push(c1 % c2);
            }
            catch (Exception e) when (e is DivideByZeroException || e is OverflowException)
            {
                throw new Trap($"i64.rem_s: {e.Message}");
            }
        }

        public static void I32And(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 & c2);
        }

        public static void I64And(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 & c2);
        }

        public static void I32Or(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 | c2);
        }

        public static void I64Or(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 | c2);
        }

        public static void I32Xor(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 ^ c2);
        }

        public static void I64Xor(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 ^ c2);
        }

        public static void I32Shl(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 << (int)(c2 & 31U));
        }

        public static void I64Shl(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 << (int)(c2 & 63U));
        }

        public static void I32ShrS(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            int c1 = frame.Pop().s32;
            frame.Push(c1 >> (int)(c2 & 31U));
        }

        public static void I64ShrS(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            long c1 = frame.Pop().s64;
            frame.Push(c1 >> (int)(c2 & 63U));
        }

        public static void I32ShrU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 >> (int)(c2 & 31U));
        }

        public static void I64ShrU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 >> (int)(c2 & 63U));
        }

        public static void I32Rotl(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            c2 &= 31;
            frame.Push(c1 << (int)c2 | c1 >> (int)(32 - c2));
        }

        public static void I64Rotl(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            c2 &= 63;
            frame.Push(c1 << (int)c2 | c1 >> (int)(64 - c2));
        }

        public static void I32Rotr(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            c2 &= 31;
            frame.Push(c1 >> (int)c2 | c1 << (int)(32 - c2));
        }

        public static void I64Rotr(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            c2 &= 63;
            frame.Push(c1 >> (int)c2 | c1 << (int)(64 - c2));
        }

        public static void I32Eqz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(frame.Pop().u32 == 0);

        public static void I64Eqz(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(frame.Pop().u64 == 0);

        public static void I32Eq(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 == c2);
        }

        public static void I64Eq(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 == c2);
        }

        public static void I32Ne(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 != c2);
        }

        public static void I64Ne(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 != c2);
        }

        public static void I32LtS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            frame.Push(c1 < c2);
        }

        public static void I32LtU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 < c2);
        }

        public static void I32GtS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            frame.Push(c1 > c2);
        }

        public static void I32GtU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 > c2);
        }

        public static void I32LeS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            frame.Push(c1 <= c2);
        }

        public static void I32LeU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 <= c2);
        }

        public static void I32GeS(Instruction instruction, Machine machine, Frame frame)
        {
            int c2 = frame.Pop().s32;
            int c1 = frame.Pop().s32;
            frame.Push(c1 >= c2);
        }

        public static void I32GeU(Instruction instruction, Machine machine, Frame frame)
        {
            uint c2 = frame.Pop().u32;
            uint c1 = frame.Pop().u32;
            frame.Push(c1 >= c2);
        }

        public static void I64LtS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            frame.Push(c1 < c2);
        }

        public static void I64LtU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 < c2);
        }

        public static void I64GtS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            frame.Push(c1 > c2);
        }

        public static void I64GtU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 > c2);
        }

        public static void I64LeS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            frame.Push(c1 <= c2);
        }

        public static void I64LeU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 <= c2);
        }

        public static void I64GeS(Instruction instruction, Machine machine, Frame frame)
        {
            long c2 = frame.Pop().s64;
            long c1 = frame.Pop().s64;
            frame.Push(c1 >= c2);
        }

        public static void I64GeU(Instruction instruction, Machine machine, Frame frame)
        {
            ulong c2 = frame.Pop().u64;
            ulong c1 = frame.Pop().u64;
            frame.Push(c1 >= c2);
        }

        public static void I32Extend8S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((int)(sbyte)(frame.Pop().u32 & 0xFF));

        public static void I32Extend16S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((int)(short)(frame.Pop().u32 & 0xFFFF));

        public static void I64Extend8S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)(sbyte)(frame.Pop().u32 & 0xFF));

        public static void I64Extend16S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)(short)(frame.Pop().u32 & 0xFFFF));

        public static void I64Extend32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)frame.Pop().s32);

        public static void I32WrapI64(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((uint)(frame.Pop().u64 & 0xFFFFFFFF));

        public static void I64ExtendI32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)frame.Pop().s32);

        public static void I64ExtendI32U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)frame.Pop().u32);

        public static void F32Abs(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Abs(frame.Pop().f32));

        public static void F64Abs(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Abs(frame.Pop().f64));

        public static void F32Neg(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(-frame.Pop().f32);

        public static void F64Neg(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(-frame.Pop().f64);

        public static void F32Sqrt(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Sqrt(frame.Pop().f32));

        public static void F64Sqrt(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Sqrt(frame.Pop().f64));

        public static void F32Ceil(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Ceiling(frame.Pop().f32));

        public static void F64Ceil(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Ceiling(frame.Pop().f64));

        public static void F32Floor(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Floor(frame.Pop().f32));

        public static void F64Floor(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Floor(frame.Pop().f64));

        public static void F32Trunc(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Truncate(frame.Pop().f32));

        public static void F64Trunc(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Truncate(frame.Pop().f64));

        public static void F32Nearest(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)Math.Round(frame.Pop().f32));

        public static void F64Nearest(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(Math.Round(frame.Pop().f64));

        public static void F32Add(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 + c2);
        }

        public static void F64Add(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 + c2);
        }

        public static void F32Sub(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 - c2);
        }

        public static void F64Sub(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 - c2);
        }

        public static void F32Mul(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 * c2);
        }

        public static void F64Mul(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 * c2);
        }

        public static void F32Div(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 / c2);
        }

        public static void F64Div(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 / c2);
        }

        public static void F32Min(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(Math.Min(c1, c2));
        }

        public static void F64Min(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(Math.Min(c1, c2));
        }

        public static void F32Max(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(Math.Max(c1, c2));
        }

        public static void F64Max(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(Math.Max(c1, c2));
        }

        public static void F32Copysign(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(Math.Sign(c2) * Math.Abs(c1));
        }

        public static void F64Copysign(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(Math.Sign(c2) * Math.Abs(c1));
        }

        public static void F32Eq(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 == c2);
        }

        public static void F64Eq(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 == c2);
        }

        public static void F32Ne(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 != c2);
        }

        public static void F64Ne(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 != c2);
        }

        public static void F32Lt(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 < c2);
        }

        public static void F64Lt(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 < c2);
        }

        public static void F32Gt(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 > c2);
        }

        public static void F64Gt(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 > c2);
        }

        public static void F32Le(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 <= c2);
        }

        public static void F64Le(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 <= c2);
        }

        public static void F32Ge(Instruction instruction, Machine machine, Frame frame)
        {
            float c2 = frame.Pop().f32;
            float c1 = frame.Pop().f32;
            frame.Push(c1 >= c2);
        }

        public static void F64Ge(Instruction instruction, Machine machine, Frame frame)
        {
            double c2 = frame.Pop().f64;
            double c1 = frame.Pop().f64;
            frame.Push(c1 >= c2);
        }

        public static void I32TruncF32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((int)frame.Pop().f32);

        public static void I32TruncF32U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((uint)frame.Pop().f32);

        public static void I32TruncF64S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((int)frame.Pop().f64);

        public static void I32TruncF64U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((uint)frame.Pop().f64);

        public static void I64TruncF32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)frame.Pop().f32);

        public static void I64TruncF32U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((ulong)frame.Pop().f32);

        public static void I64TruncF64S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((long)frame.Pop().f64);

        public static void I64TruncF64U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((ulong)frame.Pop().f64);

        public static void F32DemoteF64(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)frame.Pop().f64);

        public static void F64PromoteF32(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((double)frame.Pop().f32);

        public static void F32ConvertI32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)frame.Pop().s32);

        public static void F32ConvertI32U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)frame.Pop().u32);

        public static void F32ConvertI64S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)frame.Pop().s64);

        public static void F32ConvertI64U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((float)frame.Pop().u64);

        public static void F64ConvertI32S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((double)frame.Pop().s32);

        public static void F64ConvertI32U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((double)frame.Pop().u32);

        public static void F64ConvertI64S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((double)frame.Pop().s64);

        public static void F64ConvertI64U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push((double)frame.Pop().u64);

        // Reinterpretations are no-ops, because we don't have a separate type in Value.
        public static void I32ReinterpretF32(
            Instruction instruction,
            Machine machine,
            Frame frame
        ) { }

        public static void I64ReinterpretF64(
            Instruction instruction,
            Machine machine,
            Frame frame
        ) { }

        public static void F32ReinterpretI32(
            Instruction instruction,
            Machine machine,
            Frame frame
        ) { }

        public static void F64ReinterpretI64(
            Instruction instruction,
            Machine machine,
            Frame frame
        ) { }
    }
}
