using Derg;
using DergwasmTests.testing;
using Xunit;

namespace DergwasmTests.instructions
{
    public class VariableInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        public void TestLocalGet(int localidx, int expected)
        {
            // 0: LOCAL_GET localidx
            // 1: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                Insn(InstructionType.LOCAL_GET, new Value { s32 = localidx }),
                Nop()
            );

            machine.Frame.Locals[0] = new Value { s32 = 1 };
            machine.Frame.Locals[1] = new Value { s32 = 2 };

            machine.Step();

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.s32));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void TestLocalSet(int localidx)
        {
            // 0: I32_CONST 1
            // 1: LOCAL_SET localidx
            // 2: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                I32Const(1),
                Insn(InstructionType.LOCAL_SET, new Value { s32 = localidx }),
                Nop()
            );

            machine.Step(2);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(1, machine.Frame.Locals[localidx].s32);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void TestLocalTee(int localidx)
        {
            // 0: I32_CONST 1
            // 1: LOCAL_SET localidx
            // 2: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                I32Const(1),
                Insn(InstructionType.LOCAL_TEE, new Value { s32 = localidx }),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(1, machine.Frame.TopOfStack.s32);
            Assert.Equal(1, machine.Frame.Locals[localidx].s32);
        }

        [Fact]
        public void TestLocalTeeUsesTopOfStack()
        {
            // 0: I32_CONST 2
            // 1: I32_CONST 1
            // 2: LOCAL_TEE 0
            // 3: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                I32Const(2),
                I32Const(1),
                Insn(InstructionType.LOCAL_TEE, new Value { s32 = 0 }),
                Nop()
            );

            machine.Step(3);

            Assert.Equal(1, machine.Frame.TopOfStack.s32);
            Assert.Equal(1, machine.Frame.Locals[0].s32);
        }

        [Theory]
        [InlineData(10, 1)]
        [InlineData(11, 2)]
        public void TestGlobalGet(int globalidx, int expected)
        {
            // 0: GLOBAL_GET globalidx
            // 1: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                Insn(InstructionType.GLOBAL_GET, new Value { s32 = globalidx }),
                Nop()
            );

            machine.Globals[0] = new Value { s32 = 1 };
            machine.Globals[1] = new Value { s32 = 2 };

            machine.Step();

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.s32));
        }

        [Theory]
        [InlineData(10, 0)]
        [InlineData(11, 1)]
        public void TestGlobalSet(int globalidx, int globaladdr)
        {
            // 0: I32_CONST 1
            // 1: GLOBAL_SET globalidx
            // 2: NOP
            machine.SetProgram(
                TestMachine.VoidType,
                I32Const(1),
                Insn(InstructionType.GLOBAL_SET, new Value { s32 = globalidx }),
                Nop()
            );

            machine.Step(2);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(1, machine.Globals[globaladdr].s32);
        }
    }
}
