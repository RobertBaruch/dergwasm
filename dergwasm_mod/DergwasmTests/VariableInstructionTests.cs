﻿using Derg;
using Xunit;

namespace DergwasmTests
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
                Insn(InstructionType.LOCAL_GET, new Value(localidx)),
                Nop()
            );

            machine.Frame.Locals[0] = new Value(1);
            machine.Frame.Locals[1] = new Value(2);

            machine.Step();

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.S32));
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
                Insn(InstructionType.LOCAL_SET, new Value(localidx)),
                Nop()
            );

            machine.Step(2);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(1, machine.Frame.Locals[localidx].S32);
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
                Insn(InstructionType.LOCAL_TEE, new Value(localidx)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(1, machine.Frame.TopOfStack.S32);
            Assert.Equal(1, machine.Frame.Locals[localidx].S32);
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
                Insn(InstructionType.GLOBAL_GET, new Value(globalidx)),
                Nop()
            );

            machine.Globals[0] = new Value(1);
            machine.Globals[1] = new Value(2);

            machine.Step();

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.S32));
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
                Insn(InstructionType.GLOBAL_SET, new Value(globalidx)),
                Nop()
            );

            machine.Step(2);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(1, machine.Globals[globaladdr].S32);
        }
    }
}
