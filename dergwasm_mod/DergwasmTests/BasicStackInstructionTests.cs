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
    }
}
