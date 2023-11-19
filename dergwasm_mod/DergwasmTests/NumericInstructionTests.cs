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

        [Theory]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 0UL, 32UL)]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 1UL, 31UL)]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 0x80000000UL, 0UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 0UL, 32UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 1UL, 0UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 0x80000000UL, 31UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 0UL, 64UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 1UL, 63UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 0x8000000000000000UL, 0UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 0UL, 64UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 1UL, 0UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 0x8000000000000000UL, 63UL)]
        public void TestIUnops(
            InstructionType insn,
            InstructionType const_insn,
            ulong input,
            ulong expected_output
        )
        {
            // 0: const_insn input
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, Insn(const_insn, new Value(input)), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected_output, e.U64));
        }
    }
}
