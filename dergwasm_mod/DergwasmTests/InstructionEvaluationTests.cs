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
            label_stack.Push(new Label(arity, target, StackLevel() - args));
        }

        public FuncType GetFuncTypeFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return new FuncType(
                        /*args=*/new ValueType[] { ValueType.I32 },
                        /*returns=*/new ValueType[] { }
                    );
                default:
                    return new FuncType(
                        /*args=*/new ValueType[] { },
                        /*returns=*/new ValueType[] { }
                    );
            }
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

        private void SetProgram(params UnflattenedInstruction[] instructions)
        {
            program = new List<UnflattenedInstruction>(instructions).Flatten(start_pc);
        }

        private void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = program[machine.CurrentPC() - start_pc];
                InstructionEvaluation.Execute(insn, machine);
            }
        }

        private UnflattenedInstruction Insn(InstructionType type, params Value[] operands)
        {
            return new UnflattenedInstruction(
                type,
                (from operand in operands select new UnflattenedOperand(operand)).ToArray()
            );
        }

        private UnflattenedInstruction Nop() => Insn(InstructionType.NOP);

        private UnflattenedInstruction End() => Insn(InstructionType.END);

        private UnflattenedInstruction Drop() => Insn(InstructionType.DROP);

        private UnflattenedInstruction I32Const(int v) =>
            Insn(InstructionType.I32_CONST, new Value(v));

        // A block with zero args and zero returns.
        private UnflattenedInstruction VoidBlock(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value(0UL, (ulong)BlockType.VOID_BLOCK),
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // A block with zero args and an I32 return.
        private UnflattenedInstruction I32Block(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value(
                            0UL,
                            (ulong)BlockType.RETURNING_BLOCK | (ulong)ValueType.I32 << 2
                        ),
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // A block with an I32 arg and no returns.
        private UnflattenedInstruction I32_VoidBlock(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value(0UL, (ulong)BlockType.TYPED_BLOCK | (0UL << 2)),
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        [Fact]
        public void TestNop()
        {
            // 100: NOP
            SetProgram(Nop());

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Equal(0, machine.StackLevel());
        }

        [Fact]
        public void TestI32Const()
        {
            // 100: I32_CONST 1
            SetProgram(I32Const(1));

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Collection(machine.value_stack, e => Assert.Equal(new Value(1), e));
        }

        [Fact]
        public void TestDrop()
        {
            // 100: I32_CONST 1
            // 101: DROP
            SetProgram(I32Const(1), Drop());

            Step(2);

            Assert.Equal(102, machine.CurrentPC());
            Assert.Empty(machine.value_stack);
        }

        [Fact]
        public void TestBlock()
        {
            // 100: BLOCK
            // 101:   NOP
            // 102: END
            SetProgram(VoidBlock(Nop(), End()));

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Equal(0, machine.StackLevel());
            Assert.Collection(
                machine.label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            0 /*arity*/
                            ,
                            103 /*target*/
                            ,
                            0 /*stack_level*/
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void TestBlockEnd()
        {
            // 100: BLOCK
            // 101:   NOP
            // 102: END
            SetProgram(VoidBlock(Nop(), End()));

            Step(3);

            Assert.Equal(103, machine.CurrentPC());
            Assert.Empty(machine.value_stack);
            Assert.Empty(machine.label_stack);
        }

        [Fact]
        public void TestI32Block()
        {
            // 100: BLOCK
            // 101:   I32_CONST 1
            // 102: END
            SetProgram(I32Block(I32Const(1), End()));

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Equal(0, machine.StackLevel());
            Assert.Collection(
                machine.label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            1 /*arity*/
                            ,
                            103 /*target*/
                            ,
                            0 /*stack_level*/
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void TestI32BlockEnd()
        {
            // 100: BLOCK
            // 101:   I32_CONST 1
            // 102: END
            SetProgram(I32Block(I32Const(1), End()));

            Step(3);

            Assert.Equal(103, machine.CurrentPC());
            Assert.Collection(machine.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Empty(machine.label_stack);
        }

        [Fact]
        public void TestI32BlockEndRemovesNonReturnStackValues()
        {
            // 100: BLOCK
            // 101:   I32_CONST 2
            // 102:   I32_CONST 1
            // 103: END
            SetProgram(I32Block(I32Const(2), I32Const(1), End()));

            Step(4);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Collection(machine.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Empty(machine.label_stack);
        }

        [Fact]
        public void TestI32_VoidBlockEnd()
        {
            // 100: I32_CONST 1
            // 101: BLOCK
            // 102:   DROP  // The block is expected to consume its args.
            // 103: END
            SetProgram(I32Const(1), I32_VoidBlock(Drop(), End()));

            Step(4);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Empty(machine.value_stack);
            Assert.Empty(machine.label_stack);
        }
    }
}
