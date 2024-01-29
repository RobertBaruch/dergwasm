using Derg;
using Xunit;

namespace DergwasmTests.instructions
{
    public class ReferenceInstructionTests : InstructionTestFixture
    {
        [Fact]
        public void TestNullRef()
        {
            // 0: REF_NULL _
            // 1: NOP
            machine.SetProgram(0, Insn(InstructionType.REF_NULL, new Value { s32 = 0x6F }), Nop());

            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(new Value { u64 = 0, value_hi = 0 }, e)
            );
        }

        [Fact]
        public void TestRefFunc()
        {
            // 0: REF_FUNC 1
            // 1: NOP
            machine.SetProgram(0, Insn(InstructionType.REF_FUNC, new Value { s32 = 1 }), Nop());

            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(
                machine.Frame.value_stack,
                e =>
                    Assert.Equal(
                        new Value { u64 = 11UL, value_hi = (ulong)ReferenceValueType.FUNCREF },
                        e
                    )
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
                Insn(InstructionType.REF_NULL, new Value { s32 = 0x6F }),
                Insn(InstructionType.REF_IS_NULL),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(2, machine.Frame.PC);
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
                Insn(InstructionType.REF_FUNC, new Value { s32 = 0 }),
                Insn(InstructionType.REF_IS_NULL),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(2, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.False(e.Bool));
        }
    }
}
