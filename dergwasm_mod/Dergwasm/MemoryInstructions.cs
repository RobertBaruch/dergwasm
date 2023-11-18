namespace Derg
{
    public static class MemoryInstructions
    {
        private static unsafe T Convert<T>(byte[] mem, uint ea)
            where T : unmanaged
        {
            fixed (byte* ptr = &mem[ea])
            {
                return *(T*)ptr;
            }
        }

        public static void I32Load(Instruction instruction, IMachine machine)
        {
            uint offset = machine.Pop().U32;
            byte[] mem = machine.Memory0;
            // Ignore Operands[0], the alignment.
            uint base_addr = instruction.Operands[1].U32;
            uint ea = base_addr + offset;
            machine.Push(new Value(Convert<uint>(mem, ea)));
        }

        public static void I64Load(Instruction instruction, IMachine machine)
        {
            uint offset = machine.Pop().U32;
            byte[] mem = machine.Memory0;
            // Ignore Operands[0], the alignment.
            uint base_addr = instruction.Operands[1].U32;
            uint ea = base_addr + offset;
            machine.Push(new Value(Convert<ulong>(mem, ea)));
        }

        public static void F32Load(Instruction instruction, IMachine machine)
        {
            uint offset = machine.Pop().U32;
            byte[] mem = machine.Memory0;
            // Ignore Operands[0], the alignment.
            uint base_addr = instruction.Operands[1].U32;
            uint ea = base_addr + offset;
            machine.Push(new Value(Convert<float>(mem, ea)));
        }

        public static void F64Load(Instruction instruction, IMachine machine)
        {
            uint offset = machine.Pop().U32;
            byte[] mem = machine.Memory0;
            // Ignore Operands[0], the alignment.
            uint base_addr = instruction.Operands[1].U32;
            uint ea = base_addr + offset;
            machine.Push(new Value(Convert<double>(mem, ea)));
        }
    }
}
