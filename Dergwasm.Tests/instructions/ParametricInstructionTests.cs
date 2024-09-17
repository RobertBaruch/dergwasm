﻿using Derg.Instructions;
using Xunit;

namespace DergwasmTests.instructions
{
    public class ParametricInstructionTests : InstructionTestFixture
    {
        [Fact]
        public void TestDrop()
        {
            // 0: I32_CONST 1
            // 1: DROP
            // 2: NOP
            machine.SetProgram(0, I32Const(1), Drop(), Nop());
            machine.Step(2);

            Assert.Equal(2, machine.Frame.PC);
            Assert.Empty(machine.Frame.value_stack);
        }

        [Theory]
        [InlineData(0, 200)]
        [InlineData(1, 100)]
        public void TestSelect(int value, int expected)
        {
            // 0: I32_CONST 100  // v1
            // 1: I32_CONST 200  // v2
            // 2: I32_CONST value
            // 3: SELECT
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(100),
                I32Const(200),
                I32Const(value),
                Insn(InstructionType.SELECT),
                Nop()
            );
            machine.Step(4);

            Assert.Equal(4, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.s32));
        }
    }
}
