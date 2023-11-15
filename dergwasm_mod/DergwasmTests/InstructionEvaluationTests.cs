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
        public List<ModuleFunc> module_funcs = new List<ModuleFunc>();

        public Frame CurrentFrame() => frame_stack.Peek();

        public void PushFrame(Frame frame) => frame_stack.Push(frame);

        public int CurrentPC() => frame_stack.Peek().pc;

        public void IncrementPC() => frame_stack.Peek().pc++;

        public void SetPC(int pc) => frame_stack.Peek().pc = pc;

        public Value Peek() => CurrentFrame().value_stack.Last();

        public Value Pop()
        {
            Value top = CurrentFrame().value_stack.Last();
            CurrentFrame().value_stack.RemoveRange(CurrentFrame().value_stack.Count - 1, 1);
            return top;
        }

        public void Push(Value val) => CurrentFrame().value_stack.Add(val);

        public int StackLevel() => CurrentFrame().value_stack.Count;

        public void RemoveStack(int from_level, int arity)
        {
            CurrentFrame()
                .value_stack
                .RemoveRange(from_level, CurrentFrame().value_stack.Count - from_level - arity);
        }

        public Label PopLabel() => CurrentFrame().label_stack.Pop();

        public void PushLabel(int arity, int target)
        {
            CurrentFrame().label_stack.Push(new Label(arity, target));
        }

        public FuncType GetFuncTypeFromIndex(int index)
        {
            switch (index)
            {
                case 0: // A func with an I32 arg and no return.
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

        public void InvokeFuncFromIndex(int index)
        {
            ModuleFunc func = module_funcs[index];
            int arity = func.signature.returns.Length;
            int args = func.signature.args.Length;
            int num_locals = func.locals.Length;
            int code_len = func.code.Count;

            Frame frame = new Frame(arity, new Value[args + num_locals], CurrentFrame().module);

            // Remove args from stack and place in new frame's locals.
            CurrentFrame()
                .value_stack
                .CopyTo(0, frame.locals, CurrentFrame().value_stack.Count - args, args);
            CurrentFrame().value_stack.RemoveRange(CurrentFrame().value_stack.Count - args, args);

            // Here we would initialize the other locals. But we assume they're I32, so they're
            // already defaulted to zero.

            frame.pc = -1; // So that incrementing PC goes to beginning.
            PushFrame(frame);
            PushLabel(arity, code_len);
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

        private UnflattenedInstruction Else() => Insn(InstructionType.ELSE);

        private UnflattenedInstruction Drop() => Insn(InstructionType.DROP);

        private UnflattenedInstruction I32Const(int v) =>
            Insn(InstructionType.I32_CONST, new Value(v));

        private UnflattenedInstruction Br(int levels) =>
            Insn(InstructionType.BR, new Value(levels));

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

        // An IF with no ELSE, zero args and zero returns.
        private UnflattenedInstruction VoidIf(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.IF,
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

        // An IF with an ELSE, zero args and zero returns.
        private UnflattenedInstruction VoidIfElse(
            UnflattenedInstruction[] instructions,
            UnflattenedInstruction[] else_instructions
        )
        {
            return new UnflattenedInstruction(
                InstructionType.IF,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value(0UL, (ulong)BlockType.VOID_BLOCK),
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>(else_instructions)
                    ),
                }
            );
        }

        // A loop with zero args and zero returns.
        private UnflattenedInstruction VoidLoop(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.LOOP,
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

        // A loop with an I32 arg and no return.
        private UnflattenedInstruction I32Loop(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.LOOP,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value(0UL, (ulong)BlockType.TYPED_BLOCK | ((ulong)0 << 2)),
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
            Assert.Empty(machine.CurrentFrame().value_stack);
        }

        [Fact]
        public void TestI32Const()
        {
            // 100: I32_CONST 1
            SetProgram(I32Const(1));

            Step();

            Assert.Equal(101, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
        }

        [Fact]
        public void TestDrop()
        {
            // 100: I32_CONST 1
            // 101: DROP
            SetProgram(I32Const(1), Drop());

            Step(2);

            Assert.Equal(102, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
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
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Collection(
                machine.CurrentFrame().label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            0, /*arity*/
                            103
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
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
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
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Collection(
                machine.CurrentFrame().label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            1, /*arity*/
                            103 /*target*/
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
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestI32BlockEndWithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 100: BLOCK
            // 101:   I32_CONST 2
            // 102:   I32_CONST 1
            // 103: END
            SetProgram(I32Block(I32Const(2), I32Const(1), End()));

            Step(4);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(2), e),
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestI32_VoidBlockEnd()
        {
            // This is an invalid program.  Normally blocks consume their args.
            // But if not, their args are left on the stack!
            //
            // 100: I32_CONST 1
            // 101: BLOCK
            // 102:   NOP
            // 103: END
            SetProgram(I32Const(1), I32_VoidBlock(Nop(), End()));

            Step(4);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIf_True()
        {
            // 100: I32_CONST 1
            // 101: IF
            // 102:   NOP
            // 103: END
            SetProgram(I32Const(1), VoidIf(Nop(), End()));

            Step(2);

            Assert.Equal(102, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Collection(
                machine.CurrentFrame().label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            0, /*arity*/
                            104 /*target*/
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void TestIf_False()
        {
            // 100: I32_CONST 1
            // 101: IF
            // 102:   NOP
            // 103: END
            SetProgram(I32Const(0), VoidIf(Nop(), End()));

            Step(2);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIfElse_False()
        {
            // 100: I32_CONST 0
            // 101: IF
            // 102:   NOP
            // 103: ELSE
            // 104:   NOP
            // 105: END
            SetProgram(
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                )
            );

            Step(2);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Collection(
                machine.CurrentFrame().label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            0, /*arity*/
                            106 /*target*/
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void TestIfElseEnd_False()
        {
            // 100: I32_CONST 0
            // 101: IF
            // 102:   NOP
            // 103: ELSE
            // 104:   NOP
            // 105: END
            SetProgram(
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                )
            );

            Step(4);

            Assert.Equal(106, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIfElseEnd_True()
        {
            // 100: I32_CONST 1
            // 101: IF
            // 102:   NOP
            // 103: ELSE
            // 104:   NOP
            // 105: END
            SetProgram(
                I32Const(1),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                )
            );

            Step(4);

            Assert.Equal(106, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR0()
        {
            // 100: BLOCK
            // 101:   BR 0
            // 102:   NOP
            // 103: END
            SetProgram(VoidBlock(Br(0), Nop(), End()));

            Step(2);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR0WithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 100: BLOCK
            // 101:   I32_CONST 1
            // 102:   BR 0
            // 103:   NOP
            // 104: END
            SetProgram(VoidBlock(I32Const(1), Br(0), Nop(), End()));

            Step(3);

            Assert.Equal(105, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestReturningBlockBR0()
        {
            // 100: BLOCK [i32]
            // 101:   I32_CONST 1
            // 102:   BR 0
            // 103:   NOP
            // 104: END
            SetProgram(I32Block(I32Const(1), Br(0), Nop(), End()));

            Step(3);

            Assert.Equal(105, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR1()
        {
            // 100: BLOCK
            // 101:   BLOCK
            // 102:     BR 1
            // 103:     NOP
            // 104:   END
            // 105:   NOP
            // 106: END
            SetProgram(VoidBlock(VoidBlock(Br(1), Nop(), End()), Nop(), End()));

            Step(3);

            Assert.Equal(107, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR1WithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 100: BLOCK [i32]
            // 101:   I32_CONST 1
            // 102:   BLOCK [i32]
            // 103:     I32_CONST 2
            // 104:     BR 1
            // 105:     NOP
            // 106:   END
            // 107:   NOP
            // 108: END
            SetProgram(
                I32Block(I32Const(1), I32Block(I32Const(2), Br(1), Nop(), End()), Nop(), End())
            );

            Step(5);

            Assert.Equal(109, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestLoop()
        {
            // 100: LOOP
            // 101:   BR 0
            // 102: END
            SetProgram(VoidLoop(Br(0), End()));

            Step(1);

            Assert.Equal(101, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Collection(
                machine.CurrentFrame().label_stack,
                e =>
                    Assert.Equal(
                        new Label(
                            0, /*arity*/
                            100 /*target*/
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void TestLoopBR0()
        {
            // 100: LOOP
            // 101:   BR 0
            // 102: END
            SetProgram(VoidLoop(Br(0), End()));

            Step(2);

            Assert.Equal(100, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestLoopEnd()
        {
            // 100: LOOP
            // 101:   NOP
            // 102: END
            SetProgram(VoidLoop(Nop(), End()));

            Step(3);

            Assert.Equal(103, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoop()
        {
            // 100: I32_CONST 1
            // 101: LOOP
            // 102:   BR 0
            // 103: END
            SetProgram(I32Const(1), I32Loop(Br(0), End()));

            Step(2);

            Assert.Equal(102, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Collection(machine.CurrentFrame().label_stack, e => Assert.Equal(1, e.arity));
            Assert.Collection(machine.CurrentFrame().label_stack, e => Assert.Equal(101, e.target));
        }

        [Fact]
        public void TestArgLoopBR0()
        {
            // 100: I32_CONST 1
            // 101: LOOP
            // 102:   BR 0
            // 103: END
            SetProgram(I32Const(1), I32Loop(Br(0), End()));

            Step(3);

            Assert.Equal(101, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoopEndWithExtraValues()
        {
            // This is an invalid program, but only because the loop's signature says that
            // it returns nothing, but this program has the loop returning an i32.
            //
            // 100: I32_CONST 1
            // 101: LOOP
            // 102:   NOP
            // 103: END
            SetProgram(I32Const(1), I32Loop(Nop(), End()));

            Step(4);

            Assert.Equal(104, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoopBR0WithExtraValues()
        {
            // This is an invalid program. Normally blocks consume their args.
            // But if not, their args are left on the stack!
            //
            // 100: I32_CONST 1
            // 101: LOOP
            // 102:   I32_CONST 2
            // 103:   BR 0
            // 104: END
            SetProgram(I32Const(1), I32Loop(I32Const(2), Br(0), End()));

            Step(4);

            Assert.Equal(101, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Empty(machine.CurrentFrame().label_stack);
        }
    }
}
