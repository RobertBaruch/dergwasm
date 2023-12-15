namespace Derg
{
    public static class ParametricInstructions
    {
        public static void Drop(Instruction instruction, IMachine machine, Frame frame) =>
            machine.Pop();

        public static void Select(Instruction instruction, IMachine machine, Frame frame)
        {
            bool cond = machine.Pop().Bool;
            Value v2 = machine.Pop();
            Value v1 = machine.Pop();
            machine.Push(cond ? v1 : v2);
        }
    }
}
