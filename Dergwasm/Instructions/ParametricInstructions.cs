using Derg.Runtime;
using Dergwasm.Runtime;

namespace Derg.Instructions
{
    public static class ParametricInstructions
    {
        public static void Drop(Instruction instruction, Machine machine, Frame frame) =>
            frame.Pop();

        public static void Select(Instruction instruction, Machine machine, Frame frame)
        {
            bool cond = frame.Pop().Bool;
            Value v2 = frame.Pop();
            Value v1 = frame.Pop();
            frame.Push(cond ? v1 : v2);
        }
    }
} // namespace Derg.Instructions
