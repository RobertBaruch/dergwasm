namespace Derg
{
    public static class VariableInstructions
    {
        public static void LocalGet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            machine.Push(machine.Locals[idx]);
        }

        public static void LocalSet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.Pop();
            machine.Locals[idx] = val;
        }

        public static void LocalTee(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.TopOfStack;
            machine.Locals[idx] = val;
        }

        public static void GlobalGet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            machine.Push(machine.Globals[machine.GetGlobalAddrForIndex(idx)]);
        }

        public static void GlobalSet(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            Value val = machine.Pop();
            machine.Globals[machine.GetGlobalAddrForIndex(idx)] = val;
        }
    }
}
