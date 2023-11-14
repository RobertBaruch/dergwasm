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

        private static void Block(Instruction instruction, IMachine machine) {
            uint args;
            uint arity;
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
                    FuncType func_type = machine.CurrentFrame().module.GetFuncType(
                        operand.GetReturningBlockTypeIndex());
                    args = (uint)func_type.args.Length;
                    arity = (uint)func_type.returns.Length;
                    break;
            }
            machine.PushLabel(args, arity, operand.GetTarget());
        }

        private static void Loop(Instruction instruction, IMachine machine) {
            // A loop differs from a block in that:
            // 1. BR 0 branches to the beginning of the loop.
            // 2. A loop's "arity" is its number of arguments. This is because a BR 0, which
            //    goes to the beginning of the loop, expects some number of values on the stack
            //    (the arity), and this must be the arguments to the loop.
            // 3. Loops don't return anything, regardless of its block type.
            uint args;
            Value operand = instruction.Operands[0];

            switch (operand.GetBlockType())
            {
                case BlockType.VOID_BLOCK:
                    args = 0;
                    break;

                case BlockType.RETURNING_BLOCK:
                    args = 0;
                    break;

                default:
                    FuncType func_type = machine.CurrentFrame().module.GetFuncType(
                        operand.GetReturningBlockTypeIndex());
                    args = (uint)func_type.args.Length;
                    break;
            }
            machine.PushLabel(args, 0, operand.GetTarget());
        }

        private static void If(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().AsI32_U() != 0;
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
            {
                Block(instruction, machine);
            }
            // Jump to the else target (minus one because we always add one at the
            // end of an instruction). This is equal to the instruction's target
            // if there was no else clause.
            machine.SetPC(operand.GetElseTarget() - 1);
        }

        private static void Else(Instruction instruction, IMachine machine)
        {
            Label label = machine.PopLabel();
            machine.SetPC(label.target - 1);
        }

        private static void End(Instruction instruction, IMachine machine)
        {
            Label label = machine.PopLabel();
            machine.SetPC(label.target - 1);
        }

        private static void BrLevels(IMachine machine, uint levels)
        {
            while (levels > 0)
            {
                levels--;
                machine.PopLabel();
            }
            Label label = machine.PopLabel();
            // We need to save the top arity values, pop everything else up to
            // the label's stack level, then restore the values. This is the equivalent
            // of removing everything from the label's stack_level up to current_stack_level - n.
            machine.RemoveStack(label.stack_level, label.arity);
            machine.SetPC(label.target - 1);
        }

        private static void Br(Instruction instruction, IMachine machine)
        {
            uint levels = instruction.Operands[0].AsI32_U();
            BrLevels(machine, levels);
        }

        private static void BrIf(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().AsI32_U() != 0;
            if (cond) Br(instruction, machine);
        }

        private static void BrTable(Instruction instruction, IMachine machine)
        {
            uint idx = Math.Min(machine.Pop().AsI32_U(), (uint)instruction.Operands.Length - 1);
            uint levels = instruction.Operands[idx].AsI32_U();
            BrLevels(machine, levels);
        }

        // Executes a single instruction. After the instruction is executed, the current
        // frame's program counter will be incremented. Therefore, instructions that don't
        // do that (e.g. branches) must set the program counter to the desired program counter
        // minus one.
        public static void Execute(Instruction instruction, IMachine machine)
        {
            if (!Map.TryGetValue(instruction.Type, out var implementation))
            {
                throw new ArgumentException($"Unimplemented instruction: {instruction.Type}");
            }
            implementation(instruction, machine);
            machine.IncrementPC();
        }

        private static IReadOnlyDictionary<InstructionType, Action<Instruction, IMachine>> Map = 
            new Dictionary<InstructionType, Action<Instruction, IMachine>>()
        {
            { InstructionType.NOP, Nop },
            { InstructionType.BLOCK, Block },
            { InstructionType.LOOP, Loop },
            { InstructionType.IF, If },
            { InstructionType.ELSE, Else },
            { InstructionType.END, End },
            { InstructionType.BR, Br },
            { InstructionType.BR_IF, BrIf },
            { InstructionType.BR_TABLE, BrTable },
        };
    }
}
