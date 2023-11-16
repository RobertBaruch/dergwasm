using Derg;
using Xunit;

namespace DergwasmTests
{
    public class InstructionEvaluationTests : InstructionTestFixture
    {
        [Fact]
        public void TestNop()
        {
            // Note that in most of these tests, we don't want to fall off the
            // end of the function, so we stick a NOP on the end. We could have
            // just as easily stuck an END on the end, which would probably be
            // more accurate, since all funcs end in END.

            // 0: NOP
            // 1: NOP
            machine.SetProgram(0, Nop(), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Fact]
        public void TestI32Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, I32Const(1), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
        }

        [Fact]
        public void TestDrop()
        {
            // 0: I32_CONST 1
            // 1: DROP
            // 2: NOP
            machine.SetProgram(0, I32Const(1), Drop(), Nop());

            machine.Step(2);

            Assert.Equal(2, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Fact]
        public void TestBlock()
        {
            // 0: BLOCK
            // 1:   NOP
            // 2: END
            // 3: NOP
            machine.SetProgram(0, VoidBlock(Nop(), End()), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(new Label(0, 3), machine.Label);
        }

        [Fact]
        public void TestBlockEnd()
        {
            // 0: BLOCK
            // 1:   NOP
            // 2: END
            // 3: NOP
            machine.SetProgram(0, VoidBlock(Nop(), End()), Nop());

            machine.Step(3);

            Assert.Equal(3, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestI32Block()
        {
            // 0: BLOCK
            // 1:   I32_CONST 1
            // 2: END
            // 3: NOP
            machine.SetProgram(0, I32Block(I32Const(1), End()), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(new Label(1, 3), machine.Label);
        }

        [Fact]
        public void TestI32BlockEnd()
        {
            // 0: BLOCK
            // 1:   I32_CONST 1
            // 2: END
            // 3: NOP
            machine.SetProgram(0, I32Block(I32Const(1), End()), Nop());

            machine.Step(3);

            Assert.Equal(3, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, I32Block(I32Const(2), I32Const(1), End()), Nop());

            machine.Step(4);

            Assert.Equal(4, machine.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(2), e),
                e => Assert.Equal(new Value(1), e)
            );
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, I32Const(1), I32_VoidBlock(Nop(), End()), Nop());

            machine.Step(4);

            Assert.Equal(4, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestIf_True()
        {
            // 0: I32_CONST 1
            // 1: IF
            // 2:   NOP
            // 3: END
            // 4: NOP
            machine.SetProgram(0, I32Const(1), VoidIf(Nop(), End()), Nop());

            machine.Step(2);

            Assert.Equal(2, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(new Label(0, 4), machine.Label);
        }

        [Fact]
        public void TestIf_False()
        {
            // 0: I32_CONST 1
            // 1: IF
            // 2:   NOP
            // 3: END
            // 4: NOP
            machine.SetProgram(0, I32Const(0), VoidIf(Nop(), End()), Nop());

            machine.Step(2);

            Assert.Equal(4, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(
                0,
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(4, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(new Label(0, 6), machine.Label);
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
            machine.SetProgram(
                0,
                I32Const(0),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            machine.Step(4);

            Assert.Equal(6, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(
                0,
                I32Const(1),
                VoidIfElse(
                    new UnflattenedInstruction[] { Nop(), Else() },
                    new UnflattenedInstruction[] { Nop(), End() }
                ),
                Nop()
            );

            machine.Step(4);

            Assert.Equal(6, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestBlockBR0()
        {
            // 0: BLOCK
            // 1:   BR 0
            // 2:   NOP
            // 3: END
            // 4: NOP
            machine.SetProgram(0, VoidBlock(Br(0), Nop(), End()), Nop());

            machine.Step(2);

            Assert.Equal(4, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, VoidBlock(I32Const(1), Br(0), Nop(), End()), Nop());

            machine.Step(3);

            Assert.Equal(5, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, I32Block(I32Const(1), Br(0), Nop(), End()), Nop());

            machine.Step(3);

            Assert.Equal(5, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, VoidBlock(VoidBlock(Br(1), Nop(), End()), Nop(), End()), Nop());

            machine.Step(3);

            Assert.Equal(7, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(
                0,
                I32Block(I32Const(1), I32Block(I32Const(2), Br(1), Nop(), End()), Nop(), End()),
                Nop()
            );

            machine.Step(5);

            Assert.Equal(9, machine.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestLoop()
        {
            // 0: LOOP
            // 1:   BR 0
            // 2: END
            // 3: NOP
            machine.SetProgram(0, VoidLoop(Br(0), End()), Nop());

            machine.Step(1);

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(new Label(0, 0), machine.Label);
        }

        [Fact]
        public void TestLoopBR0()
        {
            // 0: LOOP
            // 1:   BR 0
            // 2: END
            // 3: NOP
            machine.SetProgram(0, VoidLoop(Br(0), End()), Nop());

            machine.Step(2);

            Assert.Equal(0, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestLoopEnd()
        {
            // 0: LOOP
            // 1:   NOP
            // 2: END
            // 3: NOP
            machine.SetProgram(0, VoidLoop(Nop(), End()), Nop());

            machine.Step(3);

            Assert.Equal(3, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestArgLoop()
        {
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   BR 0
            // 3: END
            // 4: NOP
            machine.SetProgram(0, I32Const(1), I32Loop(Br(0), End()), Nop());

            machine.Step(2);

            Assert.Equal(2, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Equal(new Label(1, 1), machine.Label);
        }

        [Fact]
        public void TestArgLoopBR0()
        {
            // 0: I32_CONST 1
            // 1: LOOP [i32]
            // 2:   BR 0
            // 3: END
            // 4: NOP
            machine.SetProgram(0, I32Const(1), I32Loop(Br(0), End()), Nop());

            machine.Step(3);

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, I32Const(1), I32Loop(Nop(), End()), Nop());

            machine.Step(4);

            Assert.Equal(4, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(1), e));
            Assert.Single(machine.Frame.label_stack);
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
            machine.SetProgram(0, I32Const(1), I32Loop(I32Const(2), Br(0), End()), Nop());

            machine.Step(4);

            Assert.Equal(1, machine.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
            Assert.Single(machine.Frame.label_stack);
        }

        [Fact]
        public void TestReturnNoReturnValues()
        {
            // 0: NOP
            // 1: RETURN
            // 2: NOP
            machine.SetProgram(0, Nop(), Return(), Nop());

            machine.Step(2);

            Assert.Equal(1, machine.PC);
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
            machine.SetProgram(0, VoidBlock(Return(), Nop(), End()), Nop());

            machine.Step(2);

            Assert.Equal(1, machine.PC);
            Assert.Single(machine.frame_stack);
        }

        [Fact]
        public void TestReturnValues()
        {
            // 0: I32_CONST 1
            // 1: I32_CONST 2
            // 2: RETURN
            machine.SetProgram(3, I32Const(1), I32Const(2), Return());

            machine.Step(3);

            Assert.Equal(1, machine.PC);
            Assert.Single(machine.frame_stack);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
        }

        [Fact]
        public void TestCall()
        {
            // 0: CALL 10
            // 1: NOP
            //
            // Func 10:
            // 0: NOP
            // 1: END
            machine.AddFunction(10, Nop(), End());
            machine.SetProgram(0, Call(10), Nop());

            machine.Step();

            Assert.Equal(0, machine.PC);
            Assert.Empty(machine.Frame.value_stack);

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Fact]
        public void TestCallWithOneArg()
        {
            // 0: I32_CONST 1
            // 1: CALL 1
            // 2: NOP
            //
            // Func 1:
            // 0: NOP
            // 1: END
            machine.AddFunction(1, Nop(), End());
            machine.SetProgram(0, I32Const(1), Call(1), Nop());

            machine.Step(2);

            Assert.Equal(0, machine.PC);
            Assert.Equal(new Value(1), machine.Frame.Locals[0]);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Fact]
        public void TestCallWithTwoArgs()
        {
            // 0: I32_CONST 1
            // 1: I32_CONST 2
            // 2: CALL 1
            // 3: NOP
            //
            // Func 3:
            // 0: NOP
            // 1: END
            machine.AddFunction(3, Nop(), End());
            machine.SetProgram(0, I32Const(1), I32Const(2), Call(3), Nop());

            machine.Step(3);

            Assert.Equal(0, machine.PC);
            Assert.Equal(new Value(1), machine.Frame.Locals[0]);
            Assert.Equal(new Value(2), machine.Frame.Locals[1]);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Fact]
        public void TestCallWithOneReturn()
        {
            // 0: CALL 2
            // 1: NOP
            //
            // Func 2:
            // 0: I32_CONST 1
            // 1: END
            machine.AddFunction(2, I32Const(1), End());
            machine.SetProgram(0, Call(2), Nop());

            machine.Step(3);

            Assert.Equal(1, machine.PC);
            Assert.Equal(new Value(1), machine.TopOfStack);
        }

        [Fact]
        public void TestCallWithTwoReturns()
        {
            // 0: I32_CONST 10
            // 1: I32_CONST 20
            // 2: CALL 3
            // 3: NOP
            //
            // Func 3:
            // 0: I32_CONST 1
            // 1: I32_CONST 2
            // 2: END
            machine.AddFunction(3, I32Const(1), I32Const(2), End());
            machine.SetProgram(0, I32Const(10), I32Const(20), Call(3), Nop());

            machine.Step(6);

            Assert.Equal(3, machine.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(1), e),
                e => Assert.Equal(new Value(2), e)
            );
        }

        [Fact]
        public void TestCallIndirect()
        {
            // 0: I32_CONST 1
            // 1: CALL_INDIRECT 2, 3  // Should call Func 201.
            // 2: END
            //
            // Func 200:
            // 0: I32_CONST 1
            // 1: END
            //
            // Func 201:
            // 0: I32_CONST 2
            // 1: END
            machine.AddFunction(200, I32Const(1), End());
            machine.AddFunction(201, I32Const(2), End());
            machine.SetProgram(0, I32Const(1), CallIndirect(2, 3), End());
            machine.AddTable(3, new Table(new TableType(new Limits(2), ValueType.FUNCREF)));
            machine.tables[3].Elements[0] = Value.RefOfFuncAddr(200);
            machine.tables[3].Elements[1] = Value.RefOfFuncAddr(201);

            machine.Step(3);

            Assert.Equal(1, machine.PC);
            Assert.Equal(new Value(2), machine.TopOfStack);
        }
    }
}
