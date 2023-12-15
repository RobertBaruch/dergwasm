namespace Derg
{
    public static class ReferenceInstructions
    {
        public static void RefNull(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(new Value(0, 0));

        public static void RefIsNull(Instruction instruction, Machine machine, Frame frame)
        {
            Value v = frame.Pop();
            frame.Push(new Value(v.IsNullRef()));
        }

        public static void Ref(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].Int;
            int addr = machine.GetFuncAddrFromIndex(idx);
            frame.Push(Value.RefOfFuncAddr(addr));
        }
    }
}
