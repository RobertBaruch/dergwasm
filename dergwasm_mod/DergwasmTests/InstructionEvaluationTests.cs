using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Derg
{
    public class TestMachine : IMachine
    {
        public Stack<Frame> frame_stack = new Stack<Frame>();
        public List<Value> value_stack = new List<Value>();
        public Stack<Label> label_stack = new Stack<Label>();

        public Frame CurrentFrame() => frame_stack.Peek();
        public void PushFrame(Frame frame) => frame_stack.Push(frame);
        public int CurrentPC() => frame_stack.Peek().pc;
        public void IncrementPC() => frame_stack.Peek().pc++;
        public void SetPC(int pc) => frame_stack.Peek().pc = pc;

        public Value Peek() => value_stack.Last();
        public Value Pop()
        {
            Value top = value_stack.Last();
            value_stack.RemoveRange(value_stack.Count - 1, 1);
            return top;
        }

        public void Push(Value val) => value_stack.Add(val);
        public int StackLevel() => value_stack.Count;

        public void RemoveStack(int from_level, int arity)
        {
            value_stack.RemoveRange(from_level, value_stack.Count - from_level - arity);
        }
        public Label PopLabel() => label_stack.Pop();
        public void PushLabel(int args, int arity, int target)
        {
            label_stack.Push(new Label(arity, target, StackLevel()));
        }

    }

    public class InstructionEvaluationTests
    {
        public int start_pc = 100;
        public TestMachine machine = new TestMachine();
        public List<Instruction> program;
        public InstructionEvaluationTests()
        {
            machine.PushFrame(new Frame(0, new Value[] { }, null));
            machine.SetPC(start_pc);
        }

        private void Step()
        {
            Instruction insn = program[(int)(machine.CurrentPC() - start_pc)];
            InstructionEvaluation.Execute(insn, machine);
        }

        [Fact]
        public void TestNop()
        {
            program = new List<Instruction>()
            {
                new Instruction(InstructionType.NOP, new Value[]{ }),  // 100
            };

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Equal(0, machine.StackLevel());
        }
    }
}
