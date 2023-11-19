namespace Derg
{
    public static class StackInstructions
    {
        public static void Const(Instruction instruction, IMachine machine) =>
            machine.Push(instruction.Operands[0]);

        public static void RefNull(Instruction instruction, IMachine machine) =>
            machine.Push(new Value(0, 0));

        public static void RefIsNull(Instruction instruction, IMachine machine)
        {
            Value v = machine.Pop();
            machine.Push(new Value(v.IsNullRef()));
        }

        public static void Ref(Instruction instruction, IMachine machine)
        {
            int idx = instruction.Operands[0].Int;
            int addr = machine.GetFuncAddrFromIndex(idx);
            machine.Push(Value.RefOfFuncAddr(addr));
        }

        public static void Drop(Instruction instruction, IMachine machine) => machine.Pop();

        public static void Select(Instruction instruction, IMachine machine)
        {
            bool cond = machine.Pop().Bool;
            Value v2 = machine.Pop();
            Value v1 = machine.Pop();
            machine.Push(cond ? v1 : v2);
        }
    }
}
