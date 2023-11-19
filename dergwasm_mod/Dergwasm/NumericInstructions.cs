namespace Derg
{
    public static class NumericInstructions
    {
        public static void Const(Instruction instruction, IMachine machine) =>
            machine.Push(instruction.Operands[0]);
    }
}
