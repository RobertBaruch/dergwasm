using System;
using Derg;
using Xunit;

namespace DergwasmTests
{
    public class MemoryInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(0, 0, 0x12345678U)]
        [InlineData(0, 1, 0xFF123456U)]
        [InlineData(1, 0, 0xFF123456U)]
        public void TestI32Load(int offset, int base_addr, uint expected)
        {
            Array.Copy(new byte[] { 0x78, 0x56, 0x34, 0x12, 0xFF }, machine.Memory.Data, 5);

            // 0: I32_CONST offset
            // 1: I32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.I32_LOAD, new Value(0), new Value(base_addr)),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.TopOfStack.U32);
        }

        [Theory]
        [InlineData(0, 0, 0x0807060504030201UL)]
        [InlineData(0, 1, 0x0908070605040302UL)]
        [InlineData(1, 0, 0x0908070605040302UL)]
        public void TestI64Load(int offset, int base_addr, ulong expected)
        {
            Array.Copy(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, machine.Memory.Data, 9);

            // 0: I32_CONST offset
            // 1: I64_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.I64_LOAD, new Value(0), new Value(base_addr)),
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
