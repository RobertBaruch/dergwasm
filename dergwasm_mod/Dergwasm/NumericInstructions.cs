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
        public static void I32Clz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<uint>();
            machine.Push(new Value(clz(value, 32)));
        }

        // Count leading zeros.
        public static void I64Clz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<ulong>();
            machine.Push(new Value(clz(value, 64)));
        }

        // Count trailing zeros.
        public static void I32Ctz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<uint>();
            machine.Push(new Value(ctz(value, 32)));
        }

        // Count trailing zeros.
        public static void I64Ctz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<ulong>();
            machine.Push(new Value(ctz(value, 64)));
        }

        // Count ones.
        public static void I32Popcnt(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<uint>();
            machine.Push(new Value(popcnt(value)));
        }

        // Count ones.
        public static void I64Popcnt(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop<ulong>();
            machine.Push(new Value(popcnt(value)));
        }

        public static void I32Add(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 + c2));
        }

        public static void I64Add(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 + c2));
        }

        public static void I32Sub(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 - c2));
        }

        public static void I64Sub(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 - c2));
        }

        public static void I32Mul(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 * c2));
        }

        public static void I64Mul(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 * c2));
        }

        public static void I32DivU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            try
            {
                machine.Push(new Value(c1 / c2));
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
                machine.Push(new Value(c1 / c2));
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
                machine.Push(new Value(c1 / c2));
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
                machine.Push(new Value(c1 / c2));
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
                machine.Push(new Value(c1 % c2));
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
                machine.Push(new Value(c1 % c2));
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
                machine.Push(new Value(c1 % c2));
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
                machine.Push(new Value(c1 % c2));
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
            machine.Push(new Value(c1 & c2));
        }

        public static void I64And(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 & c2));
        }

        public static void I32Or(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 | c2));
        }

        public static void I64Or(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 | c2));
        }

        public static void I32Xor(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 ^ c2));
        }

        public static void I64Xor(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 ^ c2));
        }

        public static void I32Shl(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 << (int)(c2 & 31U)));
        }

        public static void I64Shl(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 << (int)(c2 & 63U)));
        }

        public static void I32ShrS(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            int c1 = machine.Pop<int>();
            machine.Push(new Value(c1 >> (int)(c2 & 31U)));
        }

        public static void I64ShrS(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            long c1 = machine.Pop<long>();
            machine.Push(new Value(c1 >> (int)(c2 & 63U)));
        }

        public static void I32ShrU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 >> (int)(c2 & 31U)));
        }

        public static void I64ShrU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 >> (int)(c2 & 63U)));
        }

        public static void I32Rotl(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            c2 &= 31;
            machine.Push(new Value((c1 << (int)c2) | (c1 >> (int)(32 - c2))));
        }

        public static void I64Rotl(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            c2 &= 63;
            machine.Push(new Value((c1 << (int)c2) | (c1 >> (int)(64 - c2))));
        }

        public static void I32Rotr(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            c2 &= 31;
            machine.Push(new Value((c1 >> (int)c2) | (c1 << (int)(32 - c2))));
        }

        public static void I64Rotr(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            c2 &= 63;
            machine.Push(new Value((c1 >> (int)c2) | (c1 << (int)(64 - c2))));
        }

        public static void I32Eqz(Instruction instruction, IMachine machine)
        {
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 == 0));
        }

        public static void I64Eqz(Instruction instruction, IMachine machine)
        {
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 == 0));
        }

        public static void I32Eq(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 == c2));
        }

        public static void I64Eq(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 == c2));
        }

        public static void I32Ne(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 != c2));
        }

        public static void I64Ne(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 != c2));
        }

        public static void I32LtS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(new Value(c1 < c2));
        }

        public static void I32LtU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 < c2));
        }

        public static void I32GtS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(new Value(c1 > c2));
        }

        public static void I32GtU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 > c2));
        }

        public static void I32LeS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(new Value(c1 <= c2));
        }

        public static void I32LeU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 <= c2));
        }

        public static void I32GeS(Instruction instruction, IMachine machine)
        {
            int c2 = machine.Pop<int>();
            int c1 = machine.Pop<int>();
            machine.Push(new Value(c1 >= c2));
        }

        public static void I32GeU(Instruction instruction, IMachine machine)
        {
            uint c2 = machine.Pop<uint>();
            uint c1 = machine.Pop<uint>();
            machine.Push(new Value(c1 >= c2));
        }

        public static void I64LtS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(new Value(c1 < c2));
        }

        public static void I64LtU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 < c2));
        }

        public static void I64GtS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(new Value(c1 > c2));
        }

        public static void I64GtU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 > c2));
        }

        public static void I64LeS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(new Value(c1 <= c2));
        }

        public static void I64LeU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 <= c2));
        }

        public static void I64GeS(Instruction instruction, IMachine machine)
        {
            long c2 = machine.Pop<long>();
            long c1 = machine.Pop<long>();
            machine.Push(new Value(c1 >= c2));
        }

        public static void I64GeU(Instruction instruction, IMachine machine)
        {
            ulong c2 = machine.Pop<ulong>();
            ulong c1 = machine.Pop<ulong>();
            machine.Push(new Value(c1 >= c2));
        }
    }
}
