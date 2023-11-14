using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public class TestMachine : IMachine
    {
        public Stack<Frame> frame_stack = new Stack<Frame>();
        public List<Value> value_stack = new List<Value>();
        public Stack<Label> label_stack = new Stack<Label>();

        public Frame CurrentFrame()
        {
            return frame_stack.Peek();
        }

        public uint CurrentPC()
        {
            return frame_stack.Peek().pc;
        }

        public void IncrementPC()
        {
            frame_stack.Peek().pc++;
        }

        public Value Peek()
        {
            return value_stack.Last();
        }

        public Value Pop()
        {
            Value top = value_stack.Last();
            value_stack.RemoveRange(value_stack.Count - 1, 1);
            return top;
        }

        public void Push(Value val)
        {
            value_stack.Add(val);
        }

        public void RemoveStack(uint from_level, uint arity)
        {
            value_stack.RemoveRange((int)from_level, value_stack.Count - (int)from_level - (int)arity);
        }

        public Label PopLabel()
        {
            return label_stack.Pop();
        }

        public void PushLabel(uint args, uint arity, uint target)
        {
            throw new NotImplementedException();
        }

        public void SetPC(uint pc)
        {
            throw new NotImplementedException();
        }

        public uint StackLevel()
        {
            throw new NotImplementedException();
        }
    }

    public class InstructionEvaluationTests
    {
    }
}
