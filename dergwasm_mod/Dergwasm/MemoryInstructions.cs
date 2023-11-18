using System;

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

        private static unsafe T Convert<T>(Span<byte> bytes)
            where T : unmanaged
        {
            fixed (byte* ptr = bytes)
            {
                return *(T*)ptr;
            }
        }

        private static Span<byte> Span0(Instruction instruction, IMachine machine, int sz)
        {
            int offset = machine.Pop().S32;
            // Ignore Operands[0], the alignment.
            int base_addr = instruction.Operands[1].S32;
            return machine.Span0(base_addr + offset, sz);
        }

        public static void I32Load(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(Convert<uint>(Span0(instruction, machine, 4))));

        public static void I32Load8_S(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((int)(sbyte)Span0(instruction, machine, 1)[0]));

        public static void I32Load8_U(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((uint)Span0(instruction, machine, 1)[0]));

        public static void I32Load16_S(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((int)Convert<short>(Span0(instruction, machine, 2))));

        public static void I32Load16_U(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((uint)Convert<ushort>(Span0(instruction, machine, 2))));

        public static void I64Load(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(Convert<ulong>(Span0(instruction, machine, 8))));

        public static void I64Load8_S(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((long)(sbyte)Span0(instruction, machine, 1)[0]));

        public static void I64Load8_U(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((ulong)Span0(instruction, machine, 1)[0]));

        public static void I64Load16_S(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((long)Convert<short>(Span0(instruction, machine, 2))));

        public static void I64Load16_U(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((ulong)Convert<ushort>(Span0(instruction, machine, 2))));

        public static void I64Load32_S(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((long)Convert<int>(Span0(instruction, machine, 2))));

        public static void I64Load32_U(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((ulong)Convert<uint>(Span0(instruction, machine, 2))));

        public static void F32Load(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(Convert<float>(Span0(instruction, machine, 4))));

        public static void F64Load(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(Convert<double>(Span0(instruction, machine, 8))));
    }
}
