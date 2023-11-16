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

        private static void I32Const(Instruction instruction, IMachine machine)
        {
            machine.Push(instruction.Operands[0]);
        }

        private static void Drop(Instruction instruction, IMachine machine) => machine.Pop();

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
            bool cond = machine.Pop().Int() != 0;
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

        private static void End(Instruction instruction, IMachine machine)
        {
            machine.PopLabel();
        }

        private static void BrLevels(IMachine machine, int levels)
        {
            for (; levels > 0; levels--)
                machine.PopLabel();
            JumpToTopLabel(machine);
        }

        private static void Br(Instruction instruction, IMachine machine)
        {
            int levels = instruction.Operands[0].Int();
            BrLevels(machine, levels);
        }

        private static void BrIf(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().Int() != 0;
            if (cond)
                Br(instruction, machine);
        }

        private static void BrTable(Instruction instruction, IMachine machine)
        {
            int idx = Math.Min(machine.Pop().Int(), instruction.Operands.Length - 1);
            int levels = instruction.Operands[idx].Int();
            BrLevels(machine, levels);
        }

        private static void Call(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int();
            machine.InvokeFuncFromIndex(idx);
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
                { InstructionType.DROP, Drop },
                { InstructionType.ELSE, Else },
                { InstructionType.END, End },
                { InstructionType.I32_CONST, I32Const },
                { InstructionType.IF, If },
                { InstructionType.LOOP, Loop },
                { InstructionType.NOP, Nop },
                { InstructionType.RETURN, Return },
            };
    }
}
