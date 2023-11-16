using Derg;
using Xunit;

namespace DergwasmTests
{
    public class BasicStackInstructionTests : InstructionTestFixture
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
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1, e.S32));
        }

        [Fact]
        public void TestI64Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, I64Const(1L), Nop());
            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1L, e.S64));
        }

        [Fact]
        public void TestF32Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, F32Const(1.1f), Nop());
            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1.1f, e.F32));
        }

        [Fact]
        public void TestF64Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, F64Const(1.1), Nop());
            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1.1, e.F64));
        }

        [Fact]
        public void TestNullRef()
        {
            // 0: REF_NULL _
            // 1: NOP
            machine.SetProgram(0, Insn(InstructionType.REF_NULL, new Value(0x6F)), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(new Value(0, 0), e));
        }

        [Fact]
        public void TestRefFunc()
        {
            // 0: REF_FUNC 1
            // 1: NOP
            machine.SetProgram(0, Insn(InstructionType.REF_FUNC, new Value(1)), Nop());

            machine.Step();

            Assert.Equal(1, machine.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value(11UL, (ulong)ReferenceValueType.FUNCREF), e)
            );
        }

        [Fact]
        public void TestIsNullRef_True()
        {
            // 0: REF_NULL _
            // 1: REF_IS_NULL
            // 2: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.REF_NULL, new Value(0x6F)),
                Insn(InstructionType.REF_IS_NULL),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(2, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.True(e.Bool));
        }

        [Fact]
        public void TestIsNullRef_False()
        {
            // 0: REF_FUNC 0
            // 1: REF_IS_NULL
            // 2: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.REF_FUNC, new Value(0)),
                Insn(InstructionType.REF_IS_NULL),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(2, machine.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.False(e.Bool));
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
    }
}
