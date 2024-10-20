using System;
using Derg.Runtime;
using Dergwasm.Runtime;

namespace Derg.Instructions
{
    public static class MemoryInstructions
    {
        public static void I32Load(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u32 = machine.HeapGet<uint>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I32Load8_S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    s32 = machine.HeapGet<sbyte>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I32Load8_U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u32 = machine.HeapGet<byte>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I32Load16_S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    s32 = machine.HeapGet<short>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I32Load16_U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u32 = machine.HeapGet<ushort>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u64 = machine.HeapGet<ulong>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load8_S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    s64 = machine.HeapGet<sbyte>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load8_U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u64 = machine.HeapGet<byte>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load16_S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    s64 = machine.HeapGet<short>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load16_U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u64 = machine.HeapGet<ushort>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load32_S(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    s64 = machine.HeapGet<int>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I64Load32_U(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    u64 = machine.HeapGet<uint>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void F32Load(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    f32 = machine.HeapGet<float>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void F64Load(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(
                new Value
                {
                    f64 = machine.HeapGet<double>(frame.Pop().s32, instruction.Operands[1].s32)
                }
            );

        public static void I32Store(Instruction instruction, Machine machine, Frame frame)
        {
            uint val = frame.Pop().u32;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I32Store8(Instruction instruction, Machine machine, Frame frame)
        {
            byte val = (byte)frame.Pop().u32;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I32Store16(Instruction instruction, Machine machine, Frame frame)
        {
            ushort val = (ushort)frame.Pop().u32;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I64Store(Instruction instruction, Machine machine, Frame frame)
        {
            ulong val = frame.Pop().u64;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I64Store8(Instruction instruction, Machine machine, Frame frame)
        {
            byte val = (byte)frame.Pop().u64;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I64Store16(Instruction instruction, Machine machine, Frame frame)
        {
            ushort val = (ushort)frame.Pop().u64;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void I64Store32(Instruction instruction, Machine machine, Frame frame)
        {
            uint val = (uint)frame.Pop().u64;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void F32Store(Instruction instruction, Machine machine, Frame frame)
        {
            float val = frame.Pop().f32;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void F64Store(Instruction instruction, Machine machine, Frame frame)
        {
            double val = frame.Pop().f64;
            machine.HeapSet(frame.Pop().s32, instruction.Operands[1].s32, val);
        }

        public static void MemorySize(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(new Value { u32 = (uint)machine.Heap.Length >> 16 });

        public static void MemoryGrow(Instruction instruction, Machine machine, Frame frame)
        {
            int memidx = instruction.Operands[0].s32;
            if (memidx != 0)
            {
                throw new Trap("memory.grow: Non-zero memory segment accessed");
            }
            uint delta = frame.Pop().u32;
            Memory mem = machine.GetMemoryFromIndex(0);
            uint oldSize = (uint)mem.Data.Length >> 16;
            uint newSize = oldSize + delta;
            if (mem.Limits.Maximum.HasValue && newSize > mem.Limits.Maximum)
            {
                frame.Push(new Value { s32 = -1 });
                return;
            }
            // .NET has a limitation on value sizes to about 0x80000000 bytes.
            if (newSize > 0x7FFF)
            {
                frame.Push(new Value { s32 = -1 });
                return;
            }
            Array.Resize(ref mem.Data, (int)newSize << 16);
            mem.Limits.Minimum = newSize;
            frame.Push(new Value { u32 = oldSize });
        }

        public static void MemoryFill(Instruction instruction, Machine machine, Frame frame)
        {
            int memidx = instruction.Operands[0].s32;
            if (memidx != 0)
            {
                throw new Trap("memory.fill: Non-zero memory segment accessed");
            }
            uint n = frame.Pop().u32;
            byte val = (byte)frame.Pop().u32;
            uint d = frame.Pop().u32;
            Memory mem = machine.GetMemoryFromIndex(0);
            try
            {
                machine.Heap.AsSpan((int)d, (int)n).Fill(val);
            }
            catch (Exception)
            {
                throw new Trap(
                    $"memory.fill: Access out of bounds: offset 0x{d:X8} length 0x{n:X8} bytes"
                );
            }
        }

        public static void MemoryCopy(Instruction instruction, Machine machine, Frame frame)
        {
            int memidx = instruction.Operands[0].s32;
            if (memidx != 0)
            {
                throw new Trap("memory.copy: Non-zero memory segment accessed");
            }
            uint n = frame.Pop().u32;
            uint s = frame.Pop().u32;
            uint d = frame.Pop().u32;
            Memory mem = machine.GetMemoryFromIndex(0);
            try
            {
                Array.Copy(mem.Data, s, mem.Data, d, n);
            }
            catch (Exception)
            {
                throw new Trap(
                    $"memory.copy: Access out of bounds: source offset 0x{s:X8}, destination offset 0x{d:X8}, length 0x{n:X8} bytes"
                );
            }
        }

        public static void MemoryInit(Instruction instruction, Machine machine, Frame frame)
        {
            int memidx = instruction.Operands[1].s32;
            if (memidx != 0)
            {
                throw new Trap("memory.init: Non-zero memory segment accessed");
            }
            int dataidx = instruction.Operands[0].s32;
            byte[] data = machine.GetDataSegment(frame.GetDataSegmentAddrForIndex(dataidx));
            if (data == null)
            {
                throw new Trap($"memory.init: dropped data segment accessed (index {dataidx})");
            }
            uint n = frame.Pop().u32;
            uint s_offset = frame.Pop().u32;
            uint d_offset = frame.Pop().u32;
            try
            {
                Array.Copy(data, s_offset, machine.Heap, d_offset, n);
            }
            catch (Exception)
            {
                throw new Trap(
                    $"memory.init: Access out of bounds: source index {dataidx} offset 0x{s_offset:X8}, destination offset 0x{d_offset:X8}, length 0x{n:X8} bytes"
                );
            }
        }

        public static void DataDrop(Instruction instruction, Machine machine, Frame frame)
        {
            int elemidx = instruction.Operands[0].s32;
            machine.DropDataSegment(frame.GetDataSegmentAddrForIndex(elemidx));
        }
    }
} // namespace Derg.Instructions
