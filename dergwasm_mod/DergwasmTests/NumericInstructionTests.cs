using Derg;
using Xunit;

namespace DergwasmTests
{
    public class NumericInstructionTests : InstructionTestFixture
    {
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
    }
}
