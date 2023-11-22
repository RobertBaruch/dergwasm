using System;

namespace Derg
{
    public static class NumericInstructions
    {
        public static void Const(Instruction instruction, IMachine machine) =>
            machine.Push(instruction.Operands[0]);

        private static int clz(ulong value, int numbits)
        {
            // .Net Framework 4.7.2 doesn't have a built-in clz, so we have to do it ourselves.
            // There are O(log N) algorithms for this, but it's not worth it -- I don't think
            // a lot of software is really going to use this instruction.
            int count = 0;
            ulong mask = 1UL << (numbits - 1);
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
        public static void I32Clz(Instruction instruction, IMachine machine) =>
            machine.Push(clz(machine.Pop<uint>(), 32));

        // Count leading zeros.
        public static void I64Clz(Instruction instruction, IMachine machine) =>
            machine.Push(clz(machine.Pop<ulong>(), 64));

        // Count trailing zeros.
        public static void I32Ctz(Instruction instruction, IMachine machine) =>
            machine.Push(ctz(machine.Pop<uint>(), 32));

        // Count trailing zeros.
        public static void I64Ctz(Instruction instruction, IMachine machine) =>
            machine.Push(ctz(machine.Pop<ulong>(), 64));

        // Count ones.
        public static void I32Popcnt(Instruction instruction, IMachine machine) =>
            machine.Push(popcnt(machine.Pop<uint>()));

        // Count ones.
        public static void I64Popcnt(Instruction instruction, IMachine machine) =>
            machine.Push(popcnt(machine.Pop<ulong>()));

        public static void I32Add(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 + c2);
        }

        public static void I64Add(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 + c2);
        }

        public static void I32Sub(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 - c2);
        }

        public static void I64Sub(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 - c2);
        }

        public static void I32Mul(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 * c2);
        }

        public static void I64Mul(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 * c2);
        }

        public static void I32DivU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            try
            {
                machine.Push(c1 / c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i32.div_u: {e.Message}");
            }
        }

        public static void I64DivU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            try
            {
                machine.Push(c1 / c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i64.div_u: {e.Message}");
            }
        }

        public static void I32DivS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            try
            {
                machine.Push(c1 / c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i32.div_s: {e.Message}");
            }
        }

        public static void I64DivS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            try
            {
                machine.Push(c1 / c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i64.div_s: {e.Message}");
            }
        }

        public static void I32RemU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            try
            {
                machine.Push(c1 % c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i32.rem_u: {e.Message}");
            }
        }

        public static void I64RemU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            try
            {
                machine.Push(c1 % c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i64.rem_u: {e.Message}");
            }
        }

        public static void I32RemS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            try
            {
                machine.Push(c1 % c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i32.rem_s: {e.Message}");
            }
        }

        public static void I64RemS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            try
            {
                machine.Push(c1 % c2);
            }
            catch (Exception e)
            {
                throw new Trap($"i64.rem_s: {e.Message}");
            }
        }

        public static void I32And(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 & c2);
        }

        public static void I64And(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 & c2);
        }

        public static void I32Or(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 | c2);
        }

        public static void I64Or(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 | c2);
        }

        public static void I32Xor(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 ^ c2);
        }

        public static void I64Xor(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 ^ c2);
        }

        public static void I32Shl(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 << (int)(c2 & 31U));
        }

        public static void I64Shl(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 << (int)(c2 & 63U));
        }

        public static void I32ShrS(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            int c1 = machine.Pop<int>();
            machine.Push(c1 >> (int)(c2 & 31U));
        }

        public static void I64ShrS(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            long c1 = machine.Pop<long>();
            machine.Push(c1 >> (int)(c2 & 63U));
        }

        public static void I32ShrU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 >> (int)(c2 & 31U));
        }

        public static void I64ShrU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 >> (int)(c2 & 63U));
        }

        public static void I32Rotl(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            c2 &= 31;
            machine.Push((c1 << (int)c2) | (c1 >> (int)(32 - c2)));
        }

        public static void I64Rotl(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            c2 &= 63;
            machine.Push((c1 << (int)c2) | (c1 >> (int)(64 - c2)));
        }

        public static void I32Rotr(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            c2 &= 31;
            machine.Push((c1 >> (int)c2) | (c1 << (int)(32 - c2)));
        }

        public static void I64Rotr(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            c2 &= 63;
            machine.Push((c1 >> (int)c2) | (c1 << (int)(64 - c2)));
        }

        public static void I32Eqz(Instruction instruction, IMachine machine) =>
            machine.Push(machine.Pop<uint>() == 0);

        public static void I64Eqz(Instruction instruction, IMachine machine) =>
            machine.Push(machine.Pop<ulong>() == 0);

        public static void I32Eq(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 == c2);
        }

        public static void I64Eq(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 == c2);
        }

        public static void I32Ne(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 != c2);
        }

        public static void I64Ne(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 != c2);
        }

        public static void I32LtS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(c1 < c2);
        }

        public static void I32LtU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 < c2);
        }

        public static void I32GtS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(c1 > c2);
        }

        public static void I32GtU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 > c2);
        }

        public static void I32LeS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(c1 <= c2);
        }

        public static void I32LeU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 <= c2);
        }

        public static void I32GeS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(c1 >= c2);
        }

        public static void I32GeU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(c1 >= c2);
        }

        public static void I64LtS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(c1 < c2);
        }

        public static void I64LtU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 < c2);
        }

        public static void I64GtS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(c1 > c2);
        }

        public static void I64GtU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 > c2);
        }

        public static void I64LeS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(c1 <= c2);
        }

        public static void I64LeU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 <= c2);
        }

        public static void I64GeS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(c1 >= c2);
        }

        public static void I64GeU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(c1 >= c2);
        }

        public static void I32Extend8S(Instruction instruction, IMachine machine) =>
            machine.Push((int)machine.Pop<sbyte>());

        public static void I32Extend16S(Instruction instruction, IMachine machine) =>
            machine.Push((int)machine.Pop<short>());

        public static void I64Extend8S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<sbyte>());

        public static void I64Extend16S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<short>());

        public static void I64Extend32S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<int>());

        public static void I32WrapI64(Instruction instruction, IMachine machine) =>
            machine.Push((uint)(machine.Pop<ulong>() & 0xFFFFFFFF));

        public static void I64ExtendI32S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<int>());

        public static void I64ExtendI32U(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<uint>());

        public static void F32Abs(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Abs(machine.Pop<float>()));

        public static void F64Abs(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Abs(machine.Pop<double>()));

        public static void F32Neg(Instruction instruction, IMachine machine) =>
            machine.Push(-machine.Pop<float>());

        public static void F64Neg(Instruction instruction, IMachine machine) =>
            machine.Push(-machine.Pop<double>());

        public static void F32Sqrt(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Sqrt(machine.Pop<float>()));

        public static void F64Sqrt(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Sqrt(machine.Pop<double>()));

        public static void F32Ceil(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Ceiling(machine.Pop<float>()));

        public static void F64Ceil(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Ceiling(machine.Pop<double>()));

        public static void F32Floor(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Floor(machine.Pop<float>()));

        public static void F64Floor(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Floor(machine.Pop<double>()));

        public static void F32Trunc(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Truncate(machine.Pop<float>()));

        public static void F64Trunc(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Truncate(machine.Pop<double>()));

        public static void F32Nearest(Instruction instruction, IMachine machine) =>
            machine.Push((float)Math.Round(machine.Pop<float>()));

        public static void F64Nearest(Instruction instruction, IMachine machine) =>
            machine.Push(Math.Round(machine.Pop<double>()));

        public static void F32Add(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 + c2);
        }

        public static void F64Add(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 + c2);
        }

        public static void F32Sub(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 - c2);
        }

        public static void F64Sub(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 - c2);
        }

        public static void F32Mul(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 * c2);
        }

        public static void F64Mul(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 * c2);
        }

        public static void F32Div(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 / c2);
        }

        public static void F64Div(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 / c2);
        }

        public static void F32Min(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(Math.Min(c1, c2));
        }

        public static void F64Min(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(Math.Min(c1, c2));
        }

        public static void F32Max(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(Math.Max(c1, c2));
        }

        public static void F64Max(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(Math.Max(c1, c2));
        }

        public static void F32Copysign(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(Math.Sign(c2) * Math.Abs(c1));
        }

        public static void F64Copysign(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(Math.Sign(c2) * Math.Abs(c1));
        }

        public static void F32Eq(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 == c2);
        }

        public static void F64Eq(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 == c2);
        }

        public static void F32Ne(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 != c2);
        }

        public static void F64Ne(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 != c2);
        }

        public static void F32Lt(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 < c2);
        }

        public static void F64Lt(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 < c2);
        }

        public static void F32Gt(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 > c2);
        }

        public static void F64Gt(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 > c2);
        }

        public static void F32Le(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 <= c2);
        }

        public static void F64Le(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 <= c2);
        }

        public static void F32Ge(Instruction instruction, IMachine machine)
        {
            float c2 = machine.Pop<float>();
            float c1 = machine.Pop<float>();
            machine.Push(c1 >= c2);
        }

        public static void F64Ge(Instruction instruction, IMachine machine)
        {
            double c2 = machine.Pop<double>();
            double c1 = machine.Pop<double>();
            machine.Push(c1 >= c2);
        }

        public static void I32TruncF32S(Instruction instruction, IMachine machine) =>
            machine.Push((int)machine.Pop<float>());

        public static void I32TruncF32U(Instruction instruction, IMachine machine) =>
            machine.Push((uint)machine.Pop<float>());

        public static void I32TruncF64S(Instruction instruction, IMachine machine) =>
            machine.Push((int)machine.Pop<double>());

        public static void I32TruncF64U(Instruction instruction, IMachine machine) =>
            machine.Push((uint)machine.Pop<double>());

        public static void I64TruncF32S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<float>());

        public static void I64TruncF32U(Instruction instruction, IMachine machine) =>
            machine.Push((ulong)machine.Pop<float>());

        public static void I64TruncF64S(Instruction instruction, IMachine machine) =>
            machine.Push((long)machine.Pop<double>());

        public static void I64TruncF64U(Instruction instruction, IMachine machine) =>
            machine.Push((ulong)machine.Pop<double>());

        public static void F32DemoteF64(Instruction instruction, IMachine machine) =>
            machine.Push((float)machine.Pop<double>());

        public static void F64PromoteF32(Instruction instruction, IMachine machine) =>
            machine.Push((double)machine.Pop<float>());

        public static void F32ConvertI32S(Instruction instruction, IMachine machine) =>
            machine.Push((float)machine.Pop<int>());

        public static void F32ConvertI32U(Instruction instruction, IMachine machine) =>
            machine.Push((float)machine.Pop<uint>());

        public static void F32ConvertI64S(Instruction instruction, IMachine machine) =>
            machine.Push((float)machine.Pop<long>());

        public static void F32ConvertI64U(Instruction instruction, IMachine machine) =>
            machine.Push((float)machine.Pop<ulong>());

        public static void F64ConvertI32S(Instruction instruction, IMachine machine) =>
            machine.Push((double)machine.Pop<int>());

        public static void F64ConvertI32U(Instruction instruction, IMachine machine) =>
            machine.Push((double)machine.Pop<uint>());

        public static void F64ConvertI64S(Instruction instruction, IMachine machine) =>
            machine.Push((double)machine.Pop<long>());

        public static void F64ConvertI64U(Instruction instruction, IMachine machine) =>
            machine.Push((double)machine.Pop<ulong>());

        // Reinterpretations are no-ops, because we don't have a separate type in Value.
        public static void I32ReinterpretF32(Instruction instruction, IMachine machine) { }

        public static void I64ReinterpretF64(Instruction instruction, IMachine machine) { }

        public static void F32ReinterpretI32(Instruction instruction, IMachine machine) { }

        public static void F64ReinterpretI64(Instruction instruction, IMachine machine) { }
    }
}
