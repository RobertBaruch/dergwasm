using System;

namespace Derg
{
    public static class ControlInstructions
    {
        public static void Nop(Instruction instruction, IMachine machine, Frame frame) { }

        public static void Unreachable(Instruction instruction, IMachine machine, Frame frame) =>
            throw new Trap("Unreachable instruction reached!");

        public static void Block(Instruction instruction, IMachine machine, Frame frame)
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

        public static void Loop(Instruction instruction, IMachine machine, Frame frame)
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

        public static void If(Instruction instruction, IMachine machine, Frame frame)
        {
            bool cond = machine.Pop().Int != 0;
            if (cond)
            {
                Block(instruction, machine, frame);
                return;
            }
            // Start a new block if and only if there's an else clause.
            // We know that there's an else clause if the instruction's target is
            // not the same as the instruction's else_target.
            Value operand = instruction.Operands[0];
            if (operand.GetTarget() != operand.GetElseTarget())
                Block(instruction, machine, frame);
            // Jump to the else target (minus one because we always add one at the
            // end of an instruction). This is equal to the instruction's target
            // if there was no else clause.
            machine.PC = operand.GetElseTarget() - 1;
        }

        public static void JumpToTopLabel(IMachine machine, Frame frame)
        {
            Label label = machine.PopLabel();
            machine.PC = label.target - 1;
        }

        public static void Else(Instruction instruction, IMachine machine, Frame frame) =>
            JumpToTopLabel(machine, frame);

        public static void End(Instruction instruction, IMachine machine, Frame frame) =>
            machine.PopLabel();

        public static void BrLevels(IMachine machine, Frame frame, int levels)
        {
            for (; levels > 0; levels--)
                machine.PopLabel();
            JumpToTopLabel(machine, frame);
        }

        public static void Br(Instruction instruction, IMachine machine, Frame frame)
        {
            int levels = instruction.Operands[0].Int;
            BrLevels(machine, frame, levels);
        }

        public static void BrIf(Instruction instruction, IMachine machine, Frame frame)
        {
            bool cond = machine.Pop().Int != 0;
            if (cond)
                Br(instruction, machine, frame);
        }

        public static void BrTable(Instruction instruction, IMachine machine, Frame frame)
        {
            int idx = (int)Math.Min(machine.Pop().U32, (uint)instruction.Operands.Length - 1);
            int levels = instruction.Operands[idx].Int;
            BrLevels(machine, frame, levels);
        }

        public static void Call(Instruction instruction, IMachine machine, Frame frame)
        {
            int idx = instruction.Operands[0].Int;
            // I think this should actually do the call. This way, we can throw an exception
            // and have the machine's frame stack automatically unwind.
            machine.InvokeFuncFromIndex(idx);
        }

        public static void CallIndirect(Instruction instruction, IMachine machine, Frame frame)
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
                throw new Trap(
                    $"call_indirect: type mismatch calling function address {funcAddr.RefAddr} "
                        + $"({func.ModuleName}.{func.Name}). "
                        + $"Expected signature {funcType} but was {func.Signature}."
                );
            }
            machine.InvokeFunc(funcAddr.RefAddr);
        }

        public static void Return(Instruction instruction, IMachine machine, Frame frame)
        {
            // This guarantees we pop the current frame.
            machine.PC = machine.Frame.Code.Count;
        }
    }
}
