using System;
using Derg;
using Xunit;

namespace DergwasmTests
{
    public class MemoryInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(InstructionType.I32_LOAD, 0, 0, 0x1234A6A8U)]
        [InlineData(InstructionType.I32_LOAD, 0, 1, 0xFF1234A6U)]
        [InlineData(InstructionType.I32_LOAD, 1, 0, 0xFF1234A6U)]
        [InlineData(InstructionType.I32_LOAD8_S, 0, 0, 0xFFFFFFA8)]
        [InlineData(InstructionType.I32_LOAD8_U, 0, 0, 0xA8)]
        [InlineData(InstructionType.I32_LOAD16_S, 0, 0, 0xFFFFA6A8)]
        [InlineData(InstructionType.I32_LOAD16_U, 0, 0, 0xA6A8)]
        public void TestI32Load(InstructionType insn, int offset, int base_addr, uint expected)
        {
            Array.Copy(new byte[] { 0xA8, 0xA6, 0x34, 0x12, 0xFF }, machine.Memory.Data, 5);

            // 0: I32_CONST offset
            // 1: I32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(insn, new Value(0), new Value(base_addr)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.TopOfStack.U32);
        }

        [Theory]
        [InlineData(InstructionType.I64_LOAD, 0, 0, 0x08070605A4A3A2A1UL)]
        [InlineData(InstructionType.I64_LOAD, 0, 1, 0x0908070605A4A3A2UL)]
        [InlineData(InstructionType.I64_LOAD, 1, 0, 0x0908070605A4A3A2UL)]
        [InlineData(InstructionType.I64_LOAD8_S, 0, 0, 0xFFFFFFFFFFFFFFA1UL)]
        [InlineData(InstructionType.I64_LOAD8_U, 0, 0, 0x00000000000000A1UL)]
        [InlineData(InstructionType.I64_LOAD16_S, 0, 0, 0xFFFFFFFFFFFFA2A1UL)]
        [InlineData(InstructionType.I64_LOAD16_U, 0, 0, 0x000000000000A2A1UL)]
        [InlineData(InstructionType.I64_LOAD32_S, 0, 0, 0xFFFFFFFFA4A3A2A1UL)]
        [InlineData(InstructionType.I64_LOAD32_U, 0, 0, 0x00000000A4A3A2A1UL)]
        public void TestI64Load(InstructionType insn, int offset, int base_addr, ulong expected)
        {
            Array.Copy(new byte[] { 0xA1, 0xA2, 0xA3, 0xA4, 5, 6, 7, 8, 9 }, machine.Memory.Data, 9);

            // 0: I32_CONST offset
            // 1: I64_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(insn, new Value(0), new Value(base_addr)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.TopOfStack.U64);
        }

        [Theory]
        [InlineData(0, 0, 5.69045661E-28f)]
        [InlineData(0, 1, -1.94339031E+38f)]
        [InlineData(1, 0, -1.94339031E+38f)]
        public void TestF32Load(int offset, int base_addr, float expected)
        {
            Array.Copy(new byte[] { 0x78, 0x56, 0x34, 0x12, 0xFF }, machine.Memory.Data, 5);

            // 0: I32_CONST offset
            // 1: F32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.F32_LOAD, new Value(0), new Value(base_addr)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.TopOfStack.F32);
        }

        [Theory]
        [InlineData(0, 0, 5.447603722011605E-270)]
        [InlineData(0, 1, 3.7258146895053074E-265)]
        [InlineData(1, 0, 3.7258146895053074E-265)]
        public void TestF64Load(int offset, int base_addr, double expected)
        {
            Array.Copy(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, machine.Memory.Data, 9);

            // 0: I32_CONST offset
            // 1: F64_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.F64_LOAD, new Value(0), new Value(base_addr)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.TopOfStack.F64);
        }
    }
}
