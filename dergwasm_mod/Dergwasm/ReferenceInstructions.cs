namespace Derg
{
    public static class ReferenceInstructions
    {
        public static void RefNull(Instruction instruction, Machine machine, Frame frame) =>
            frame.Push(new Value { u64 = 0, value_hi = 0 });

        public static void RefIsNull(Instruction instruction, Machine machine, Frame frame)
        {
            Value v = frame.Pop();
            frame.Push(v.IsNullRef());
        }

        public static void Ref(Instruction instruction, Machine machine, Frame frame)
        {
            int idx = instruction.Operands[0].s32;
            int addr = frame.GetFuncAddrForIndex(idx);
            frame.Push(Value.RefOfFuncAddr(addr));
        }
    }
}
