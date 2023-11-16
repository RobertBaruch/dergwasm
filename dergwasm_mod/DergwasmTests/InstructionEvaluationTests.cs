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

        public void PopFrame()
        {
            Frame last_frame = frame_stack.Pop();
            CurrentFrame()
                .value_stack
                .AddRange(
                    last_frame
                        .value_stack
                        .GetRange(last_frame.value_stack.Count - last_frame.Arity, last_frame.Arity)
                );
        }

        public int CurrentPC() => frame_stack.Peek().pc;

        public void IncrementPC() => frame_stack.Peek().pc++;

        public void SetPC(int pc) => frame_stack.Peek().pc = pc;

        public Value Peek() => CurrentFrame().value_stack.Last();

        public Value Pop()
        {
            Value top = CurrentFrame().value_stack.Last();
            CurrentFrame().value_stack.RemoveAt(CurrentFrame().value_stack.Count - 1);
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

        public Label PeekLabel() => CurrentFrame().label_stack.Peek();

        public void PushLabel(int arity, int target)
        {
            CurrentFrame().label_stack.Push(new Label(arity, target));
        }

        public FuncType GetFuncTypeFromIndex(int index)
        {
            switch (index)
            {
                case 1: // A func with an I32 arg and no return.
                    return new FuncType(
                        /*args=*/new ValueType[] { ValueType.I32 },
                        /*returns=*/new ValueType[] { }
                    );
                case 2: // A func with no args and an I32 return.
                    return new FuncType(
                        /*args=*/new ValueType[] { },
                        /*returns=*/new ValueType[] { ValueType.I32 }
                    );
                case 3: // A func with 2 I32 args and 2 I32 returns.
                    return new FuncType(
                        /*args=*/new ValueType[] { ValueType.I32, ValueType.I32 },
                        /*returns=*/new ValueType[] { ValueType.I32, ValueType.I32 }
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
            int arity = func.Signature.returns.Length;
            int args = func.Signature.args.Length;
            int num_locals = func.Locals.Length;
            int code_len = func.Code.Count;

            Frame next_frame = new Frame(new Value[args + num_locals], CurrentFrame().Module);

            // Remove args from stack and place in new frame's locals.
            CurrentFrame()
                .value_stack
                .CopyTo(0, next_frame.Locals, CurrentFrame().value_stack.Count - args, args);
            CurrentFrame().value_stack.RemoveRange(CurrentFrame().value_stack.Count - args, args);

            // Here we would initialize the other locals. But we assume they're I32, so they're
            // already defaulted to zero.

            next_frame.pc = -1; // So that incrementing PC goes to beginning.
            PushFrame(next_frame);
            PushLabel(arity, code_len);
        }
    }

    public class InstructionEvaluationTests
    {
        public TestMachine machine = new TestMachine();
        public List<Instruction> program;

        public InstructionEvaluationTests()
        {
            // This frame collects any return values.
            machine.PushFrame(new Frame(new Value[] { }, null));
        }

        // Sets the program up for execution, with a signature given by the idx (see GetFuncTypeFromIndex).
        // The program always has two I32 locals.
        private void SetProgram(int idx, params UnflattenedInstruction[] instructions)
        {
            FuncType signature = machine.GetFuncTypeFromIndex(idx);
            machine.PushFrame(new Frame(new Value[] { new Value(0), new Value(0) }, null));
            program = new List<UnflattenedInstruction>(instructions).Flatten(0);
            machine.CurrentFrame().Func = new ModuleFunc(
                signature,
                new ValueType[] { ValueType.I32, ValueType.I32 },
                program
            );
            machine.PushLabel(machine.CurrentFrame().Arity, program.Count);
        }

        private void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = machine.CurrentFrame().Code[machine.CurrentPC()];
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

        private UnflattenedInstruction Return() => Insn(InstructionType.RETURN);

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
                        new Value(0UL, (ulong)BlockType.TYPED_BLOCK | (1UL << 2)),
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
                        new Value(0UL, (ulong)BlockType.TYPED_BLOCK | (1UL << 2)),
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        [Fact]
        public void TestNop()
        {
            // Note that in most of these tests, we don't want to fall off the
            // end of the function, so we stick a NOP on the end. We could have
            // just as easily stuck an END on the end, which would probably be
            // more accurate, since all funcs end in END.

            // 0: NOP
            // 1: NOP
            SetProgram(0, Nop(), Nop());

            Step();

            Assert.Equal(1, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
        }

        [Fact]
        public void TestI32Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            SetProgram(0, I32Const(1), Nop());

            Step();

            Assert.Equal(1, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
        }

        [Fact]
        public void TestDrop()
        {
            // 0: I32_CONST 1
            // 1: DROP
            // 2: NOP
            SetProgram(0, I32Const(1), Drop(), Nop());

            Step(2);

            Assert.Equal(2, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
        }

        [Fact]
        public void TestBlock()
        {
            // 0: BLOCK
            // 1:   NOP
            // 2: END
            // 3: NOP
            SetProgram(0, VoidBlock(Nop(), End()), Nop());

            Step();

            Assert.Equal(1, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Equal(
                new Label(
                    0, /*arity*/
                    3
                ),
                machine.PeekLabel()
            );
        }

        [Fact]
        public void TestBlockEnd()
        {
            // 0: BLOCK
            // 1:   NOP
            // 2: END
            // 3: NOP
            SetProgram(0, VoidBlock(Nop(), End()), Nop());

            Step(3);

            Assert.Equal(3, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestI32Block()
        {
            // 0: BLOCK
            // 1:   I32_CONST 1
            // 2: END
            // 3: NOP
            SetProgram(0, I32Block(I32Const(1), End()), Nop());

            Step();

            Assert.Equal(1, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Equal(
                new Label(
                    1, /*arity*/
                    3 /*target*/
                ),
                machine.PeekLabel()
            );
        }

        [Fact]
        public void TestI32BlockEnd()
        {
            // 0: BLOCK
            // 1:   I32_CONST 1
            // 2: END
            // 3: NOP
            SetProgram(0, I32Block(I32Const(1), End()), Nop());

            Step(3);

            Assert.Equal(3, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestI32BlockEndWithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 0: BLOCK
            // 1:   I32_CONST 2
            // 2:   I32_CONST 1
            // 3: END
            // 4: NOP
            SetProgram(0, I32Block(I32Const(2), I32Const(1), End()), Nop());

            Step(4);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(2), e),
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestI32_VoidBlockEnd()
        {
            // This is an invalid program.  Normally blocks consume their args.
            // But if not, their args are left on the stack!
            //
            // 0: I32_CONST 1
            // 1: BLOCK
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(1), I32_VoidBlock(Nop(), End()), Nop());

            Step(4);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIf_True()
        {
            // 0: I32_CONST 1
            // 1: IF
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(1), VoidIf(Nop(), End()), Nop());

            Step(2);

            Assert.Equal(2, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Equal(
                new Label(
                    0, /*arity*/
                    4 /*target*/
                ),
                machine.PeekLabel()
            );
        }

        [Fact]
        public void TestIf_False()
        {
            // 0: I32_CONST 1
            // 1: IF
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(0), VoidIf(Nop(), End()), Nop());

            Step(2);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIfElse_False()
        {
            // 0: I32_CONST 0
            // 1: IF
            // 2:   NOP
            // 3: ELSE
            // 4:   NOP
            // 5: END
            // 6: NOP
            SetProgram(
                0,
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            Step(2);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Equal(
                new Label(
                    0, /*arity*/
                    6 /*target*/
                ),
                machine.PeekLabel()
            );
        }

        [Fact]
        public void TestIfElseEnd_False()
        {
            // 0: I32_CONST 0
            // 1: IF
            // 2:   NOP
            // 3: ELSE
            // 4:   NOP
            // 5: END
            // 6: NOP
            SetProgram(
                0,
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            Step(4);

            Assert.Equal(6, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestIfElseEnd_True()
        {
            // 0: I32_CONST 1
            // 1: IF
            // 2:   NOP
            // 3: ELSE
            // 4:   NOP
            // 5: END
            // 6: NOP
            SetProgram(
                0,
                I32Const(1),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            Step(4);

            Assert.Equal(6, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR0()
        {
            // 0: BLOCK
            // 1:   BR 0
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, VoidBlock(Br(0), Nop(), End()), Nop());

            Step(2);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR0WithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 0: BLOCK
            // 1:   I32_CONST 1
            // 2:   BR 0
            // 3:   NOP
            // 4: END
            // 5: NOP
            SetProgram(0, VoidBlock(I32Const(1), Br(0), Nop(), End()), Nop());

            Step(3);

            Assert.Equal(5, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestReturningBlockBR0()
        {
            // 0: BLOCK [i32]
            // 1:   I32_CONST 1
            // 2:   BR 0
            // 3:   NOP
            // 4: END
            // 5: NOP
            SetProgram(0, I32Block(I32Const(1), Br(0), Nop(), End()), Nop());

            Step(3);

            Assert.Equal(5, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR1()
        {
            // 0: BLOCK
            // 1:   BLOCK
            // 2:     BR 1
            // 3:     NOP
            // 4:   END
            // 5:   NOP
            // 6: END
            // 7: NOP
            SetProgram(0, VoidBlock(VoidBlock(Br(1), Nop(), End()), Nop(), End()), Nop());

            Step(3);

            Assert.Equal(7, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestBlockBR1WithExtraValues()
        {
            // This is an invalid program. Normally blocks add only arity values to the stack.
            // But if not, the extra values are left on the stack!
            //
            // 0: BLOCK [i32]
            // 1:   I32_CONST 1
            // 2:   BLOCK [i32]
            // 3:     I32_CONST 2
            // 4:     BR 1
            // 5:     NOP
            // 6:   END
            // 7:   NOP
            // 8: END
            // 9: NOP
            SetProgram(
                0,
                I32Block(I32Const(1), I32Block(I32Const(2), Br(1), Nop(), End()), Nop(), End()),
                Nop()
            );

            Step(5);

            Assert.Equal(9, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestLoop()
        {
            // 0: LOOP
            // 1:   BR 0
            // 2: END
            // 3: NOP
            SetProgram(0, VoidLoop(Br(0), End()), Nop());

            Step(1);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Equal(
                new Label(
                    0, /*arity*/
                    0 /*target*/
                ),
                machine.PeekLabel()
            );
        }

        [Fact]
        public void TestLoopBR0()
        {
            // 0: LOOP
            // 1:   BR 0
            // 2: END
            // 3: NOP
            SetProgram(0, VoidLoop(Br(0), End()), Nop());

            Step(2);

            Assert.Equal(0, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestLoopEnd()
        {
            // 0: LOOP
            // 1:   NOP
            // 2: END
            // 3: NOP
            SetProgram(0, VoidLoop(Nop(), End()), Nop());

            Step(3);

            Assert.Equal(3, machine.CurrentPC());
            Assert.Empty(machine.CurrentFrame().value_stack);
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoop()
        {
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   BR 0
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(1), I32Loop(Br(0), End()), Nop());

            Step(2);

            Assert.Equal(2, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Equal(new Label(1, 1), machine.PeekLabel());
        }

        [Fact]
        public void TestArgLoopBR0()
        {
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   BR 0
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(1), I32Loop(Br(0), End()), Nop());

            Step(3);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoopEndWithExtraValues()
        {
            // This is an invalid program, but only because the loop's signature says that
            // it returns nothing, but this program has the loop returning an i32.
            //
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, I32Const(1), I32Loop(Nop(), End()), Nop());

            Step(4);

            Assert.Equal(4, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestArgLoopBR0WithExtraValues()
        {
            // This is an invalid program. Normally blocks consume their args.
            // But if not, their args are left on the stack!
            //
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   I32_CONST 2
            // 3:   BR 0
            // 4: END
            // 5: NOP
            SetProgram(0, I32Const(1), I32Loop(I32Const(2), Br(0), End()), Nop());

            Step(4);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Single(machine.CurrentFrame().label_stack);
        }

        [Fact]
        public void TestReturnNoReturnValues()
        {
            // 0: NOP
            // 1: RETURN
            // 2: NOP
            SetProgram(0, Nop(), Return(), Nop());

            Step(2);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Single(machine.frame_stack);
        }

        [Fact]
        public void TestReturnFromBlock()
        {
            // 0: BLOCK
            // 1:   RETURN
            // 2:   NOP
            // 3: END
            // 4: NOP
            SetProgram(0, VoidBlock(Return(), Nop(), End()), Nop());

            Step(2);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Single(machine.frame_stack);
        }

        [Fact]
        public void TestReturnValues()
        {
            // 0: I32_CONST 1
            // 1: I32_CONST 2
            // 2: RETURN
            SetProgram(3, I32Const(1), I32Const(2), Return());

            Step(3);

            Assert.Equal(1, machine.CurrentPC());
            Assert.Single(machine.frame_stack);
            Assert.Collection(
                machine.CurrentFrame().value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
        }
    }
}
