namespace Derg
{
    public static class VariableInstructions
    {
        public static void LocalGet(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            frame.Push(frame.Locals[idx]);
        }

        public static void LocalSet(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            Value val = frame.Pop();
            frame.Locals[idx] = val;
        }

        public static void LocalTee(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            Value val = frame.TopOfStack;
            frame.Locals[idx] = val;
        }

        public static void GlobalGet(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            frame.Push(machine.Globals[frame.GetGlobalAddrForIndex(idx)]);
        }

        public static void GlobalSet(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            Value val = frame.Pop();
            machine.Globals[frame.GetGlobalAddrForIndex(idx)] = val;
        }
    }
}
