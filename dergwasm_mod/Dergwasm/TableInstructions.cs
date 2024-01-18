using System;

namespace Derg
{
    public static class TableInstructions
    {
        public static void TableGet(Instruction instruction, Machine machine, Frame frame)
        {
            int tableidx = instruction.Operands[0].s32;
            uint elemidx = frame.Pop().u32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            if (elemidx >= table.Elements.LongLength)
            {
                throw new Trap($"table_get: element index {elemidx} out of bounds");
            }
            frame.Push(table.Elements[elemidx]);
        }

        public static void TableSet(Instruction instruction, Machine machine, Frame frame)
        {
            Value val = frame.Pop();
            int tableidx = instruction.Operands[0].s32;
            uint elemidx = frame.Pop().u32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            if (elemidx >= table.Elements.LongLength)
            {
                throw new Trap($"table_set: element index {elemidx} out of bounds");
            }
            table.Elements[elemidx] = val;
        }

        public static void TableSize(Instruction instruction, Machine machine, Frame frame)
        {
            int tableidx = instruction.Operands[0].s32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            frame.Push(new Value { s32 = table.Elements.Length });
        }

        public static void TableGrow(Instruction instruction, Machine machine, Frame frame)
        {
            int tableidx = instruction.Operands[0].s32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            uint delta = frame.Pop().u32;
            Value val = frame.Pop();
            uint oldSize = (uint)table.Elements.Length;
            uint newSize = oldSize + delta;
            if (table.Type.Limits.Maximum.HasValue && newSize > table.Type.Limits.Maximum)
            {
                frame.Push(new Value { s32 = -1 });
                return;
            }
            if (newSize > 0xFFFF)
            {
                frame.Push(new Value { s32 = -1 });
                return;
            }
            Array.Resize(ref table.Elements, (int)newSize);
            // Array.Fill not available in .NET Framework 4.7.2.
            for (uint i = oldSize; i < newSize; i++)
                table.Elements[i] = val;
            table.Type.Limits.Minimum = newSize;
            frame.Push(new Value { u32 = oldSize });
        }

        public static void TableInit(Instruction instruction, Machine machine, Frame frame)
        {
            int tableidx = instruction.Operands[0].s32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            int elemidx = instruction.Operands[1].s32;
            ElementSegment element = machine.GetElementSegment(
                frame.GetElementSegmentAddrForIndex(elemidx)
            );
            uint n = frame.Pop().u32;
            uint s = frame.Pop().u32;
            uint d = frame.Pop().u32;
            if (s + n > element.Elements.Length || d + n > table.Elements.Length)
            {
                throw new Trap("table.init: access out of bounds");
            }
            if (n > 0)
            {
                Array.Copy(element.Elements, s, table.Elements, d, n);
            }
        }

        public static void TableFill(Instruction instruction, Machine machine, Frame frame)
        {
            int tableidx = instruction.Operands[0].s32;
            Table table = machine.GetTable(frame.GetTableAddrForIndex(tableidx));
            uint n = frame.Pop().u32;
            Value val = frame.Pop();
            uint i = frame.Pop().u32;
            if (i + n > table.Elements.Length)
            {
                throw new Trap("table.fill: access out of bounds");
            }
            if (n > 0)
            {
                for (uint j = i; j < i + n; j++)
                    table.Elements[j] = val;
            }
        }

        public static void TableCopy(Instruction instruction, Machine machine, Frame frame)
        {
            int dtableidx = instruction.Operands[0].s32;
            Table dtable = machine.GetTable(frame.GetTableAddrForIndex(dtableidx));
            int stableidx = instruction.Operands[1].s32;
            Table stable = machine.GetTable(frame.GetTableAddrForIndex(stableidx));
            uint n = frame.Pop().u32;
            uint s = frame.Pop().u32;
            uint d = frame.Pop().u32;
            if (s + n > stable.Elements.Length || d + n > dtable.Elements.Length)
            {
                throw new Trap("table.copy: access out of bounds");
            }
            if (n > 0)
            {
                Array.Copy(stable.Elements, s, dtable.Elements, d, n);
            }
        }

        public static void ElemDrop(Instruction instruction, Machine machine, Frame frame)
        {
            int elemidx = instruction.Operands[0].s32;
            machine.DropElementSegment(frame.GetElementSegmentAddrForIndex(elemidx));
        }
    }
}
