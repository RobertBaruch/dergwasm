using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public static class InstructionEvaluation
    {
        private static void Nop(Instruction instruction, IMachine machine) { }

        private static void Unreachable(Instruction instruction, IMachine machine) =>
            throw new Trap("Unreachable instruction reached!");

        private static void Const(Instruction instruction, IMachine machine) =>
            machine.Push(instruction.Operands[0]);

        private static void RefNull(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(0, 0));

        private static void RefIsNull(Instruction instruction, IMachine machine)
        {
            Value v = machine.Pop();
            machine.Push(new Value(v.IsNullRef()));
        }

        private static void Ref(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            int addr = machine.GetFuncAddrFromIndex(idx);
            machine.Push(Value.RefOfFuncAddr(addr));
        }

        private static void Drop(Instruction instruction, IMachine machine) => machine.Pop();

        private static void Select(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().Bool;
            Value v2 = machine.Pop();
            Value v1 = machine.Pop();
            machine.Push(cond ? v1 : v2);
        }

        private static void LocalGet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            machine.Push(machine.Locals[idx]);
        }

        private static void LocalSet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.Pop();
            machine.Locals[idx] = val;
        }

        private static void LocalTee(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.TopOfStack;
            machine.Locals[idx] = val;
        }

        private static void GlobalGet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            machine.Push(machine.Globals[machine.GetGlobalAddrForIndex(idx)]);
        }

        private static void GlobalSet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.Pop();
            machine.Globals[machine.GetGlobalAddrForIndex(idx)] = val;
        }

        private static void TableGet(Instruction instruction, IMachine machine)
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

        private static void TableSet(Instruction instruction, IMachine machine)
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

        private static void TableSize(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            machine.Push(new Value(table.Elements.Length));
        }

        private static void TableGrow(Instruction instruction, IMachine machine)
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

        private static void TableInit(Instruction instruction, IMachine machine)
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

        private static void TableFill(Instruction instruction, IMachine machine)
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

        private static void TableCopy(Instruction instruction, IMachine machine)
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

        private static void ElemDrop(Instruction instruction, IMachine machine)
        {
            int elemidx = instruction.Operands[0].Int;
            machine.DropElementSegmentFromIndex(elemidx);
        }

        private static void Block(Instruction instruction, IMachine machine)
        {
            // A block's args are what it expects to be on the stack upon entry.
            // A block's arity are what it leaves on the stack upon exit.
            int args;
            int arity;
            Value operand = instruction.Operands[0];

            switch (operand.GetBlockType())
            {
                case BlockType.VOID_BLOCK:
                    args = 0;
                    arity = 0;
                    break;

                case BlockType.RETURNING_BLOCK:
                    args = 0;
                    arity = 1;
                    break;

                default:
                    FuncType func_type = machine.GetFuncTypeFromIndex(
                        operand.GetReturningBlockTypeIndex()
                    );
                    args = func_type.args.Length;
                    arity = func_type.returns.Length;
                    break;
            }
            machine.Label = new Label(arity, operand.GetTarget());
        }

        private static void Loop(Instruction instruction, IMachine machine)
        {
            // A loop differs from a block in that:
            // 1. BR 0 branches to the beginning of the loop.
            // 2. A loop's "arity" is its number of arguments. This is because a BR 0, which
            //    goes to the beginning of the loop, expects some number of values on the stack
            //    (the arity) to continue, and this must be the arguments to the loop.
            int arity;
            Value operand = instruction.Operands[0];

            switch (operand.GetBlockType())
            {
                case BlockType.VOID_BLOCK:
                    arity = 0;
                    break;

                case BlockType.RETURNING_BLOCK:
                    arity = 0;
                    break;

                default:
                    FuncType func_type = machine.GetFuncTypeFromIndex(
                        operand.GetReturningBlockTypeIndex()
                    );
                    arity = func_type.args.Length;
                    break;
            }
            machine.Label = new Label(
                arity, /*arity*/
                operand.GetTarget()
            );
        }

        private static void If(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().Int != 0;
            if (cond)
            {
                Block(instruction, machine);
                return;
            }
            // Start a new block if and only if there's an else clause.
            // We know that there's an else clause if the instruction's target is
            // not the same as the instruction's else_target.
            Value operand = instruction.Operands[0];
            if (operand.GetTarget() != operand.GetElseTarget())
                Block(instruction, machine);
            // Jump to the else target (minus one because we always add one at the
            // end of an instruction). This is equal to the instruction's target
            // if there was no else clause.
            machine.PC = operand.GetElseTarget() - 1;
        }

        private static void JumpToTopLabel(IMachine machine)
        {
            Label label = machine.PopLabel();
            machine.PC = label.target - 1;
        }

        private static void Else(Instruction instruction, IMachine machine) =>
            JumpToTopLabel(machine);

        private static void End(Instruction instruction, IMachine machine) => machine.PopLabel();

        private static void BrLevels(IMachine machine, int levels)
        {
            for (; levels > 0; levels--)
                machine.PopLabel();
            JumpToTopLabel(machine);
        }

        private static void Br(Instruction instruction, IMachine machine)
        {
            int levels = instruction.Operands[0].Int;
            BrLevels(machine, levels);
        }

        private static void BrIf(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().Int != 0;
            if (cond)
                Br(instruction, machine);
        }

        private static void BrTable(Instruction instruction, IMachine machine)
        {
            int idx = (int)Math.Min(machine.Pop().U32, (uint)instruction.Operands.Length - 1);
            int levels = instruction.Operands[idx].Int;
            BrLevels(machine, levels);
        }

        private static void Call(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            machine.InvokeFuncFromIndex(idx);
        }

        private static void CallIndirect(Instruction instruction, IMachine machine)
        {
            int tableidx = instruction.Operands[1].Int;
            int typeidx = instruction.Operands[0].Int;
            Table table = machine.GetTableFromIndex(tableidx);
            FuncType funcType = machine.GetFuncTypeFromIndex(typeidx);
            uint i = machine.Pop().U32;
            if (i >= table.Elements.LongLength)
            {
                throw new Trap(
                    $"call_indirect: access out of bounds (index {i}, table len {table.Elements.LongLength})"
                );
            }
            Value funcAddr = table.Elements[i];
            if (funcAddr.IsNullRef())
            {
                throw new Trap("call_indirect: null reference");
            }
            Func func = machine.GetFunc(funcAddr.RefAddr);
            if (func.Signature != funcType)
            {
                throw new Trap("call_indirect: type mismatch");
            }
            machine.InvokeFunc(funcAddr.RefAddr);
        }

        private static void Return(Instruction instruction, IMachine machine)
        {
            // This guarantees we pop the current frame.
            machine.PC = machine.Frame.Code.Count;
        }

        // Executes a single instruction. After the instruction is executed, the current
        // frame's program counter will be incremented. Therefore, instructions that don't
        // do that (e.g. branches) must set the program counter to the desired program counter
        // minus one.
        public static void Execute(Instruction instruction, IMachine machine)
        {
            if (!Map.TryGetValue(instruction.Type, out var implementation))
                throw new ArgumentException($"Unimplemented instruction: {instruction.Type}");
            implementation(instruction, machine);
            machine.PC++;

            // If we ran off the end of the function, we return from the function.

            if (machine.PC >= machine.Frame.Code.Count)
            {
                machine.PopFrame();
                machine.PC++;
            }
        }

        private static IReadOnlyDictionary<InstructionType, Action<Instruction, IMachine>> Map =
            new Dictionary<InstructionType, Action<Instruction, IMachine>>()
            {
                // Please maintain these sorted alphanumerically by InstructionType.
                { InstructionType.BR, Br },
                { InstructionType.BR_IF, BrIf },
                { InstructionType.BR_TABLE, BrTable },
                { InstructionType.BLOCK, Block },
                { InstructionType.CALL, Call },
                { InstructionType.CALL_INDIRECT, CallIndirect },
                { InstructionType.DROP, Drop },
                { InstructionType.ELEM_DROP, ElemDrop },
                { InstructionType.ELSE, Else },
                { InstructionType.END, End },
                { InstructionType.F32_CONST, Const },
                { InstructionType.F64_CONST, Const },
                { InstructionType.GLOBAL_GET, GlobalGet },
                { InstructionType.GLOBAL_SET, GlobalSet },
                { InstructionType.I32_CONST, Const },
                { InstructionType.I64_CONST, Const },
                { InstructionType.IF, If },
                { InstructionType.LOCAL_GET, LocalGet },
                { InstructionType.LOCAL_SET, LocalSet },
                { InstructionType.LOCAL_TEE, LocalTee },
                { InstructionType.LOOP, Loop },
                { InstructionType.NOP, Nop },
                { InstructionType.REF_IS_NULL, RefIsNull },
                { InstructionType.REF_FUNC, Ref },
                { InstructionType.REF_NULL, RefNull },
                { InstructionType.RETURN, Return },
                { InstructionType.SELECT, Select },
                { InstructionType.TABLE_COPY, TableCopy },
                { InstructionType.TABLE_GET, TableGet },
                { InstructionType.TABLE_GROW, TableGrow },
                { InstructionType.TABLE_INIT, TableInit },
                { InstructionType.TABLE_FILL, TableFill },
                { InstructionType.TABLE_SET, TableSet },
                { InstructionType.TABLE_SIZE, TableSize },
                { InstructionType.UNREACHABLE, Unreachable },
            };
    }
}
