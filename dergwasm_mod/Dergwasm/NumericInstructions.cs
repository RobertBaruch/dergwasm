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

        // Count leading zeros.
        public static void I32Clz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop().U32;
            machine.Push(new Value(clz(value, 32)));
        }

        // Count leading zeros.
        public static void I64Clz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop().U64;
            machine.Push(new Value(clz(value, 64)));
        }

        // Count trailing zeros.
        public static void I32Ctz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop().U32;
            machine.Push(new Value(ctz(value, 32)));
        }

        // Count trailing zeros.
        public static void I64Ctz(Instruction instruction, IMachine machine)
        {
            ulong value = machine.Pop().U64;
            machine.Push(new Value(ctz(value, 64)));
        }
    }
}
