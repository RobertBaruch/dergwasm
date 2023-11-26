using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Derg
{
    public static class MemoryInstructions
    {
        public static unsafe T Convert<T>(byte[] mem, uint ea)
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

        private static unsafe void Store<T>(Span<byte> bytes, T value)
            where T : unmanaged
        {
            fixed (byte* ptr = bytes)
            {
                *(T*)ptr = value;
            }
        }

        private static Span<byte> Span0(Instruction instruction, IMachine machine, int sz)
        {
            int offset = machine.Pop().S32;
            // Ignore Operands[0], the alignment.
            int base_addr = instruction.Operands[1].S32;
            try
            {
                return machine.Span0(base_addr + offset, sz);
            }
            catch (Exception e)
            {
                throw new Trap(
                    $"Memory access out of bounds: base {(uint)base_addr:8X} offset {(uint)offset:8X}"
                );
            }
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

        public static void I32Store(Instruction instruction, IMachine machine)
        {
            uint val = machine.Pop().U32;
            Span<byte> span = Span0(instruction, machine, 4);
            Store<uint>(span, val);
        }

        public static void I32Store8(Instruction instruction, IMachine machine)
        {
            uint val = machine.Pop().U32;
            Span0(instruction, machine, 1)[0] = (byte)val;
        }

        public static void I32Store16(Instruction instruction, IMachine machine)
        {
            uint val = machine.Pop().U32;
            Span<byte> span = Span0(instruction, machine, 2);
            Store<ushort>(span, (ushort)val);
        }

        public static void I64Store(Instruction instruction, IMachine machine)
        {
            ulong val = machine.Pop().U64;
            Span<byte> span = Span0(instruction, machine, 8);
            Store<ulong>(span, val);
        }

        public static void I64Store8(Instruction instruction, IMachine machine)
        {
            ulong val = machine.Pop().U64;
            Span0(instruction, machine, 1)[0] = (byte)val;
        }

        public static void I64Store16(Instruction instruction, IMachine machine)
        {
            ulong val = machine.Pop().U64;
            Span<byte> span = Span0(instruction, machine, 2);
            Store<ushort>(span, (ushort)val);
        }

        public static void I64Store32(Instruction instruction, IMachine machine)
        {
            ulong val = machine.Pop().U64;
            Span<byte> span = Span0(instruction, machine, 4);
            Store<uint>(span, (uint)val);
        }

        public static void F32Store(Instruction instruction, IMachine machine)
        {
            float val = machine.Pop().F32;
            Span<byte> span = Span0(instruction, machine, 4);
            Store<float>(span, val);
        }

        public static void F64Store(Instruction instruction, IMachine machine)
        {
            double val = machine.Pop().F64;
            Span<byte> span = Span0(instruction, machine, 8);
            Store<double>(span, val);
        }

        public static void MemorySize(Instruction instruction, IMachine machine) =>
            machine.Push(new Value((uint)machine.Memory0.Length >> 16));

        public static void MemoryGrow(Instruction instruction, IMachine machine)
        {
            int memidx = instruction.Operands[0].Int;
            if (memidx != 0)
            {
                throw new Trap("memory.grow: Non-zero memory segment accessed");
            }
            uint delta = machine.Pop().U32;
            Memory mem = machine.GetMemoryFromIndex(0);
            uint oldSize = (uint)mem.Data.Length >> 16;
            uint newSize = oldSize + delta;
            if (mem.Limits.Maximum.HasValue && newSize > mem.Limits.Maximum)
            {
                machine.Push(new Value(-1));
                return;
            }
            // .NET has a limitation on value sizes to about 0x80000000 bytes.
            if (newSize > 0x7FFF)
            {
                machine.Push(new Value(-1));
                return;
            }
            Array.Resize(ref mem.Data, (int)newSize << 16);
            mem.Limits.Minimum = newSize;
            machine.Push(new Value(oldSize));
        }

        public static void MemoryFill(Instruction instruction, IMachine machine)
        {
            int memidx = instruction.Operands[0].Int;
            if (memidx != 0)
            {
                throw new Trap("memory.fill: Non-zero memory segment accessed");
            }
            uint n = machine.Pop().U32;
            byte val = (byte)machine.Pop().U32;
            uint d = machine.Pop().U32;
            Memory mem = machine.GetMemoryFromIndex(0);
            try
            {
                machine.Memory0.AsSpan<byte>((int)d, (int)n).Fill(val);
            }
            catch (Exception e)
            {
                throw new Trap(
                    $"memory.fill: Access out of bounds: offset 0x{d:8X} length 0x{n:8X} bytes"
                );
            }
        }

        public static void MemoryCopy(Instruction instruction, IMachine machine)
        {
            int memidx = instruction.Operands[0].Int;
            if (memidx != 0)
            {
                throw new Trap("memory.copy: Non-zero memory segment accessed");
            }
            uint n = machine.Pop().U32;
            uint s = machine.Pop().U32;
            uint d = machine.Pop().U32;
            Memory mem = machine.GetMemoryFromIndex(0);
            try
            {
                Array.Copy(mem.Data, s, mem.Data, d, n);
            }
            catch (Exception e)
            {
                throw new Trap(
                    $"memory.copy: Access out of bounds: source offset 0x{s:8X}, destination offset 0x{d:8X}, length 0x{n:8X} bytes"
                );
            }
        }

        public static void MemoryInit(Instruction instruction, IMachine machine)
        {
            int memidx = instruction.Operands[1].Int;
            if (memidx != 0)
            {
                throw new Trap("memory.init: Non-zero memory segment accessed");
            }
            int dataidx = instruction.Operands[0].Int;
            byte[] data = machine.GetDataSegmentFromIndex(dataidx);
            if (data == null)
            {
                throw new Trap($"memory.init: dropped data segment accessed (index {dataidx})");
            }
            uint n = machine.Pop().U32;
            uint s_offset = machine.Pop().U32;
            uint d_offset = machine.Pop().U32;
            try
            {
                Array.Copy(data, s_offset, machine.Memory0, d_offset, n);
            }
            catch (Exception e)
            {
                throw new Trap(
                    $"memory.init: Access out of bounds: source index {dataidx} offset 0x{s_offset:8X}, destination offset 0x{d_offset:8X}, length 0x{n:8X} bytes"
                );
            }
        }

        public static void DataDrop(Instruction instruction, IMachine machine)
        {
            int elemidx = instruction.Operands[0].Int;
            machine.DropDataSegmentFromIndex(elemidx);
        }
    }
}
