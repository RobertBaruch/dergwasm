using System;

namespace Derg
{
    public static class TableInstructions
    {
        public static void TableGet(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            uint elemidx = machine.Pop().U32;
            Table table = machine.GetTableFromIndex(tableidx);
            if (elemidx >= table.Elements.LongLength)
            {
                throw new Trap($"table_get: element index {elemidx} out of bounds");
            }
            machine.Push(table.Elements[elemidx]);
        }

        public static void TableSet(Instruction instruction, IMachine machine)
        {
            Value val = machine.Pop();
            int tableidx = instruction.Operands[0].Int;
            uint elemidx = machine.Pop().U32;
            Table table = machine.GetTableFromIndex(tableidx);
            if (elemidx >= table.Elements.LongLength)
            {
                throw new Trap($"table_set: element index {elemidx} out of bounds");
            }
            table.Elements[elemidx] = val;
        }

        public static void TableSize(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            machine.Push(new Value(table.Elements.Length));
        }

        public static void TableGrow(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            uint delta = machine.Pop().U32;
            Value val = machine.Pop();
            uint oldSize = (uint)table.Elements.Length;
            uint newSize = oldSize + delta;
            if (table.Type.Limits.Maximum.HasValue && newSize > table.Type.Limits.Maximum)
            {
                machine.Push(new Value(-1));
                return;
            }
            if (newSize > 0xFFFF)
            {
                machine.Push(new Value(-1));
                return;
            }
            Array.Resize(ref table.Elements, (int)newSize);
            // Array.Fill not available in .NET Framework 4.7.2.
            for (uint i = oldSize; i < newSize; i++)
                table.Elements[i] = val;
            table.Type.Limits.Minimum = newSize;
            machine.Push(new Value(oldSize));
        }

        public static void TableInit(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            int elemidx = instruction.Operands[1].Int;
            ElementSegment element = machine.GetElementSegmentFromIndex(elemidx);
            uint n = machine.Pop().U32;
            uint s = machine.Pop().U32;
            uint d = machine.Pop().U32;
            if (s + n > element.Elements.Length || d + n > table.Elements.Length)
            {
                throw new Trap("table.init: access out of bounds");
            }
            if (n > 0)
            {
                Array.Copy(element.Elements, s, table.Elements, d, n);
            }
        }

        public static void TableFill(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            uint n = machine.Pop().U32;
            Value val = machine.Pop();
            uint i = machine.Pop().U32;
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

        public static void TableCopy(Instruction instruction, IMachine machine)
        {
            int dtableidx = instruction.Operands[0].Int;
            Table dtable = machine.GetTableFromIndex(dtableidx);
            int stableidx = instruction.Operands[1].Int;
            Table stable = machine.GetTableFromIndex(stableidx);
            uint n = machine.Pop().U32;
            uint s = machine.Pop().U32;
            uint d = machine.Pop().U32;
            if (s + n > stable.Elements.Length || d + n > dtable.Elements.Length)
            {
                throw new Trap("table.copy: access out of bounds");
            }
            if (n > 0)
            {
                Array.Copy(stable.Elements, s, dtable.Elements, d, n);
            }
        }

        public static void ElemDrop(Instruction instruction, IMachine machine)
        {
            int elemidx = instruction.Operands[0].Int;
            machine.DropElementSegmentFromIndex(elemidx);
        }
    }
}
