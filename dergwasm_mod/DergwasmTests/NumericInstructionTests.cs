using System;
using System.Security.Cryptography;
using Derg;
using Xunit;

namespace DergwasmTests
{
    public class NumericInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(0x12345678U)]
        [InlineData(0xFFFFFFFFU)]
        public void TestI32Const(uint val)
        {
            // 0: I32_CONST val
            // 1: NOP
            machine.SetProgram(0, I32Const(val), Nop());
            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(val, e.u32));
        }

        [Fact]
        public void TestI64Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, I64Const(1L), Nop());
            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1L, e.s64));
        }

        [Fact]
        public void TestF32Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, F32Const(1.1f), Nop());
            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1.1f, e.f32));
        }

        [Fact]
        public void TestF64Const()
        {
            // 0: I32_CONST 1
            // 1: NOP
            machine.SetProgram(0, F64Const(1.1), Nop());
            machine.Step();

            Assert.Equal(1, machine.Frame.PC);
            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1.1, e.f64));
        }

        [Theory]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 0UL, 32UL)]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 1UL, 31UL)]
        [InlineData(InstructionType.I32_CLZ, InstructionType.I32_CONST, 0x80000000UL, 0UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 0UL, 32UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 1UL, 0UL)]
        [InlineData(InstructionType.I32_CTZ, InstructionType.I32_CONST, 0x80000000UL, 31UL)]
        [InlineData(InstructionType.I32_POPCNT, InstructionType.I32_CONST, 0UL, 0UL)]
        [InlineData(InstructionType.I32_POPCNT, InstructionType.I32_CONST, 0x80000000UL, 1UL)]
        [InlineData(InstructionType.I32_POPCNT, InstructionType.I32_CONST, 0xFFFFFFFFUL, 32UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 0UL, 64UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 1UL, 63UL)]
        [InlineData(InstructionType.I64_CLZ, InstructionType.I64_CONST, 0x8000000000000000UL, 0UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 0UL, 64UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 1UL, 0UL)]
        [InlineData(InstructionType.I64_CTZ, InstructionType.I64_CONST, 0x8000000000000000UL, 63UL)]
        [InlineData(InstructionType.I64_POPCNT, InstructionType.I64_CONST, 0UL, 0UL)]
        [InlineData(
            InstructionType.I64_POPCNT,
            InstructionType.I64_CONST,
            0x8000000000000000UL,
            1UL
        )]
        [InlineData(
            InstructionType.I64_POPCNT,
            InstructionType.I64_CONST,
            0xFFFFFFFFFFFFFFFFUL,
            64UL
        )]
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
            machine.SetProgram(0, Insn(const_insn, new Value { u64 = input }), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected_output, e.u64));
        }

        [Theory]
        [InlineData(InstructionType.I32_ADD, 1U, 2U, 3U)]
        [InlineData(InstructionType.I32_SUB, 1U, 2U, 0xFFFFFFFFU)]
        [InlineData(InstructionType.I32_MUL, 2U, 3U, 6U)]
        [InlineData(InstructionType.I32_DIV_S, 2U, 3U, 0U)]
        [InlineData(InstructionType.I32_DIV_S, 5U, 3U, 1U)]
        [InlineData(InstructionType.I32_DIV_S, 99U, 100U, 0U)] // 99/100 = 0
        [InlineData(InstructionType.I32_DIV_S, 0xFFFFFF9DU, 100U, 0U)] // -99/100 = 0
        [InlineData(InstructionType.I32_DIV_S, 0xFFFFFF9BU, 100U, 0xFFFFFFFFU)] // -101/100 = -1
        [InlineData(InstructionType.I32_DIV_S, 101U, 0xFFFFFF9CU, 0xFFFFFFFFU)] // 101/-100 = -1
        [InlineData(InstructionType.I32_DIV_U, 2U, 3U, 0U)]
        [InlineData(InstructionType.I32_DIV_U, 5U, 3U, 1U)]
        [InlineData(InstructionType.I32_DIV_U, 99U, 100U, 0U)] // 99/100 = 0
        [InlineData(InstructionType.I32_DIV_U, 0xFFFFFF9DU, 100U, 0x028F5C27U)]
        [InlineData(InstructionType.I32_DIV_U, 0xFFFFFF9BU, 100U, 0x028F5C27U)]
        [InlineData(InstructionType.I32_DIV_U, 101U, 0xFFFFFF9CU, 0U)]
        [InlineData(InstructionType.I32_REM_U, 6U, 4U, 2U)]
        [InlineData(InstructionType.I32_REM_U, 99U, 100U, 99U)]
        [InlineData(InstructionType.I32_REM_U, 101U, 100U, 1U)]
        [InlineData(InstructionType.I32_REM_S, 13U, 3U, 1U)]
        [InlineData(InstructionType.I32_REM_S, 0xFFFFFFF3U, 3U, 0xFFFFFFFFU)] // -13%3 = -1
        [InlineData(InstructionType.I32_REM_S, 13U, 0xFFFFFFFDU, 1U)] // 13%-3 = 1
        [InlineData(InstructionType.I32_REM_S, 0xFFFFFFF3U, 0xFFFFFFFDU, 0xFFFFFFFFU)] // -13%-3 = -1
        [InlineData(InstructionType.I32_AND, 0xFF00FF00U, 0x12345678U, 0x12005600U)]
        [InlineData(InstructionType.I32_OR, 0xFF00FF00U, 0x12345678U, 0xFF34FF78U)]
        [InlineData(InstructionType.I32_XOR, 0xFF00FF00U, 0xFFFF0000U, 0x00FFFF00U)]
        [InlineData(InstructionType.I32_SHL, 0xFF00FF00U, 4U, 0xF00FF000U)]
        [InlineData(InstructionType.I32_SHL, 0xFF00FF00U, 36U, 0xF00FF000U)]
        [InlineData(InstructionType.I32_SHR_S, 0x0F00FF00U, 4U, 0x00F00FF0U)]
        [InlineData(InstructionType.I32_SHR_S, 0xFF00FF00U, 4U, 0xFFF00FF0U)]
        [InlineData(InstructionType.I32_SHR_S, 0xFF00FF00U, 36U, 0xFFF00FF0U)]
        [InlineData(InstructionType.I32_SHR_U, 0x0F00FF00U, 4U, 0x00F00FF0U)]
        [InlineData(InstructionType.I32_SHR_U, 0xFF00FF00U, 4U, 0x0FF00FF0U)]
        [InlineData(InstructionType.I32_SHR_U, 0xFF00FF00U, 36U, 0x0FF00FF0U)]
        [InlineData(InstructionType.I32_ROTL, 0xF000000FU, 1U, 0xE000001FU)]
        [InlineData(InstructionType.I32_ROTL, 0xF000000FU, 33U, 0xE000001FU)]
        [InlineData(InstructionType.I32_ROTL, 0x7000000FU, 1U, 0xE000001EU)]
        [InlineData(InstructionType.I32_ROTR, 0xF000000FU, 1U, 0xF8000007U)]
        [InlineData(InstructionType.I32_ROTR, 0xF000000FU, 33U, 0xF8000007U)]
        [InlineData(InstructionType.I32_ROTR, 0xF000000EU, 1U, 0x78000007U)]
        public void TestI32Binops(InstructionType insn, uint v1, uint v2, uint expected)
        {
            // 0: I32_CONST v1
            // 1: I32_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I32Const(v1), I32Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u32));
        }

        [Theory]
        [InlineData(InstructionType.I32_DIV_U, 6U, 0U)]
        [InlineData(InstructionType.I32_DIV_S, 6U, 0U)]
        [InlineData(InstructionType.I32_DIV_S, 0x80000000U, 0xFFFFFFFFU)] // -2^31/-1
        [InlineData(InstructionType.I32_REM_U, 6U, 0U)]
        [InlineData(InstructionType.I32_REM_S, 6U, 0U)]
        public void TestI32BinopsTraps(InstructionType instructionType, uint v1, uint v2)
        {
            // 0: I32_CONST v1
            // 1: I32_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I32Const(v1), I32Const(v2), Insn(instructionType), Nop());

            Assert.Throws<Trap>(() => machine.Step(3));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0xFFFF, false)]
        public void TestI32Testops(uint v, bool expected)
        {
            // 0: I32_CONST v
            // 1: I32_EQZ
            // 2: NOP
            machine.SetProgram(0, I32Const(v), Insn(InstructionType.I32_EQZ), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Theory]
        [InlineData(InstructionType.I32_EQ, 0U, 1U, false)]
        [InlineData(InstructionType.I32_EQ, 1U, 1U, true)]
        [InlineData(InstructionType.I32_NE, 0U, 1U, true)]
        [InlineData(InstructionType.I32_NE, 1U, 1U, false)]
        [InlineData(InstructionType.I32_LT_U, 1U, 1U, false)]
        [InlineData(InstructionType.I32_LT_U, 0U, 1U, true)]
        [InlineData(InstructionType.I32_LT_U, 0xFFFFFFFFU, 1U, false)]
        [InlineData(InstructionType.I32_LT_S, 1U, 1U, false)]
        [InlineData(InstructionType.I32_LT_S, 0U, 1U, true)]
        [InlineData(InstructionType.I32_LT_S, 0xFFFFFFFFU, 1U, true)]
        [InlineData(InstructionType.I32_LE_U, 2U, 1U, false)]
        [InlineData(InstructionType.I32_LE_U, 1U, 1U, true)]
        [InlineData(InstructionType.I32_LE_U, 0U, 1U, true)]
        [InlineData(InstructionType.I32_LE_U, 0xFFFFFFFFU, 1U, false)]
        [InlineData(InstructionType.I32_LE_S, 2U, 1U, false)]
        [InlineData(InstructionType.I32_LE_S, 1U, 1U, true)]
        [InlineData(InstructionType.I32_LE_S, 0U, 1U, true)]
        [InlineData(InstructionType.I32_LE_S, 0xFFFFFFFFU, 1U, true)]
        [InlineData(InstructionType.I32_GT_U, 1U, 1U, false)]
        [InlineData(InstructionType.I32_GT_U, 1U, 0U, true)]
        [InlineData(InstructionType.I32_GT_U, 1U, 0xFFFFFFFFU, false)]
        [InlineData(InstructionType.I32_GT_S, 1U, 1U, false)]
        [InlineData(InstructionType.I32_GT_S, 1U, 0U, true)]
        [InlineData(InstructionType.I32_GT_S, 1U, 0xFFFFFFFFU, true)]
        [InlineData(InstructionType.I32_GE_U, 1U, 2U, false)]
        [InlineData(InstructionType.I32_GE_U, 1U, 1U, true)]
        [InlineData(InstructionType.I32_GE_U, 1U, 0U, true)]
        [InlineData(InstructionType.I32_GE_U, 1U, 0xFFFFFFFFU, false)]
        [InlineData(InstructionType.I32_GE_S, 1U, 2U, false)]
        [InlineData(InstructionType.I32_GE_S, 1U, 1U, true)]
        [InlineData(InstructionType.I32_GE_S, 1U, 0U, true)]
        [InlineData(InstructionType.I32_GE_S, 1U, 0xFFFFFFFFU, true)]
        public void TestI32Relops(InstructionType insn, uint v1, uint v2, bool expected)
        {
            // 0: I32_CONST v1
            // 1: I32_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I32Const(v1), I32Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Theory]
        [InlineData(InstructionType.I64_ADD, 1UL, 2UL, 3UL)]
        [InlineData(InstructionType.I64_SUB, 1UL, 2UL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(InstructionType.I64_MUL, 2UL, 3UL, 6UL)]
        [InlineData(InstructionType.I64_DIV_S, 2UL, 3UL, 0UL)]
        [InlineData(InstructionType.I64_DIV_S, 5UL, 3UL, 1UL)]
        [InlineData(InstructionType.I64_DIV_S, 99UL, 100UL, 0UL)] // 99/100 = 0
        [InlineData(InstructionType.I64_DIV_S, 0xFFFFFFFFFFFFFF9DUL, 100UL, 0UL)] // -99/100 = 0
        [InlineData(InstructionType.I64_DIV_S, 0xFFFFFFFFFFFFFF9BUL, 100UL, 0xFFFFFFFFFFFFFFFFUL)] // -101/100 = -1
        [InlineData(InstructionType.I64_DIV_S, 101UL, 0xFFFFFFFFFFFFFF9CUL, 0xFFFFFFFFFFFFFFFFUL)] // 101/-100 = -1
        [InlineData(InstructionType.I64_DIV_U, 2UL, 3UL, 0UL)]
        [InlineData(InstructionType.I64_DIV_U, 5UL, 3UL, 1UL)]
        [InlineData(InstructionType.I64_DIV_U, 99UL, 100UL, 0UL)] // 99/100 = 0
        [InlineData(InstructionType.I64_DIV_U, 0xFFFFFFFFFFFFFF9DUL, 100UL, 0x28F5C28F5C28F5BUL)]
        [InlineData(InstructionType.I64_DIV_U, 0xFFFFFFFFFFFFFF9BUL, 100UL, 0x28F5C28F5C28F5BUL)]
        [InlineData(InstructionType.I64_DIV_U, 101UL, 0xFFFFFFFFFFFFFF9CUL, 0UL)]
        [InlineData(InstructionType.I64_REM_U, 6UL, 4UL, 2UL)]
        [InlineData(InstructionType.I64_REM_U, 99UL, 100UL, 99UL)]
        [InlineData(InstructionType.I64_REM_U, 101UL, 100UL, 1UL)]
        [InlineData(InstructionType.I64_REM_S, 13UL, 3UL, 1UL)]
        [InlineData(InstructionType.I64_REM_S, 0xFFFFFFFFFFFFFFF3UL, 3UL, 0xFFFFFFFFFFFFFFFFUL)] // -13%3 = -1
        [InlineData(InstructionType.I64_REM_S, 13UL, 0xFFFFFFFFFFFFFFFDUL, 1UL)] // 13%-3 = 1
        [InlineData(
            InstructionType.I64_REM_S,
            0xFFFFFFFFFFFFFFF3UL,
            0xFFFFFFFFFFFFFFFDUL,
            0xFFFFFFFFFFFFFFFFUL
        )] // -13%-3 = -1
        [InlineData(InstructionType.I64_AND, 0xFF00FF00UL, 0x12345678UL, 0x12005600UL)]
        [InlineData(InstructionType.I64_OR, 0xFF00FF00UL, 0x12345678UL, 0xFF34FF78UL)]
        [InlineData(InstructionType.I64_XOR, 0xFF00FF00UL, 0xFFFF0000UL, 0x00FFFF00UL)]
        [InlineData(InstructionType.I64_SHL, 0xFF00FF00UL, 4UL, 0xFF00FF000UL)]
        [InlineData(InstructionType.I64_SHL, 0xFF00FF00UL, 68UL, 0xFF00FF000UL)]
        [InlineData(InstructionType.I64_SHR_S, 0x0F00FF00UL, 4UL, 0x00F00FF0UL)]
        [InlineData(InstructionType.I64_SHR_S, 0xFFFFFFFFFF00FF00UL, 4UL, 0xFFFFFFFFFFF00FF0UL)]
        [InlineData(InstructionType.I64_SHR_S, 0xFFFFFFFFFF00FF00UL, 68UL, 0xFFFFFFFFFFF00FF0UL)]
        [InlineData(InstructionType.I64_SHR_U, 0x0F00FF00UL, 4UL, 0x00F00FF0UL)]
        [InlineData(InstructionType.I64_SHR_U, 0xFFFFFFFFFF00FF00UL, 4UL, 0x0FFFFFFFFFF00FF0UL)]
        [InlineData(InstructionType.I64_SHR_U, 0xFFFFFFFFFF00FF00UL, 68UL, 0x0FFFFFFFFFF00FF0UL)]
        [InlineData(InstructionType.I64_ROTL, 0xFFFFFFFFF000000FUL, 1UL, 0xFFFFFFFFE000001FUL)]
        [InlineData(InstructionType.I64_ROTL, 0xFFFFFFFFF000000FUL, 65UL, 0xFFFFFFFFE000001FUL)]
        [InlineData(InstructionType.I64_ROTL, 0x7FFFFFFF7000000FUL, 1UL, 0xFFFFFFFEE000001EUL)]
        [InlineData(InstructionType.I64_ROTR, 0xFFFFFFFFF000000FUL, 1UL, 0xFFFFFFFFF8000007UL)]
        [InlineData(InstructionType.I64_ROTR, 0xFFFFFFFFF000000FUL, 65UL, 0xFFFFFFFFF8000007UL)]
        [InlineData(InstructionType.I64_ROTR, 0xFFFFFFFFF000000EUL, 1UL, 0x7FFFFFFFF8000007UL)]
        public void TestI64Binops(InstructionType insn, ulong v1, ulong v2, ulong expected)
        {
            // 0: I64_CONST v1
            // 1: I64_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I64Const(v1), I64Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u64));
        }

        [Theory]
        [InlineData(InstructionType.I64_DIV_U, 6UL, 0UL)]
        [InlineData(InstructionType.I64_DIV_S, 6UL, 0UL)]
        [InlineData(InstructionType.I64_DIV_S, 0x8000000000000000UL, 0xFFFFFFFFFFFFFFFFUL)] // -2^63/-1
        [InlineData(InstructionType.I64_REM_U, 6UL, 0UL)]
        [InlineData(InstructionType.I64_REM_S, 6UL, 0UL)]
        public void TestI64BinopsTraps(InstructionType instructionType, ulong v1, ulong v2)
        {
            // 0: I64_CONST v1
            // 1: I64_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I64Const(v1), I64Const(v2), Insn(instructionType), Nop());

            Assert.Throws<Trap>(() => machine.Step(3));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0xFFFF, false)]
        public void TestI64Testops(ulong v, bool expected)
        {
            // 0: I64_CONST v
            // 1: I64_EQZ
            // 2: NOP
            machine.SetProgram(0, I64Const(v), Insn(InstructionType.I64_EQZ), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Theory]
        [InlineData(InstructionType.I64_EQ, 0UL, 1UL, false)]
        [InlineData(InstructionType.I64_EQ, 1UL, 1UL, true)]
        [InlineData(InstructionType.I64_NE, 0UL, 1UL, true)]
        [InlineData(InstructionType.I64_NE, 1UL, 1UL, false)]
        [InlineData(InstructionType.I64_LT_U, 1UL, 1UL, false)]
        [InlineData(InstructionType.I64_LT_U, 0UL, 1UL, true)]
        [InlineData(InstructionType.I64_LT_U, 0xFFFFFFFFFFFFFFFFUL, 1UL, false)]
        [InlineData(InstructionType.I64_LT_S, 1UL, 1UL, false)]
        [InlineData(InstructionType.I64_LT_S, 0UL, 1UL, true)]
        [InlineData(InstructionType.I64_LT_S, 0xFFFFFFFFFFFFFFFFUL, 1UL, true)]
        [InlineData(InstructionType.I64_LE_U, 2UL, 1UL, false)]
        [InlineData(InstructionType.I64_LE_U, 1UL, 1UL, true)]
        [InlineData(InstructionType.I64_LE_U, 0UL, 1UL, true)]
        [InlineData(InstructionType.I64_LE_U, 0xFFFFFFFFFFFFFFFFUL, 1UL, false)]
        [InlineData(InstructionType.I64_LE_S, 2UL, 1UL, false)]
        [InlineData(InstructionType.I64_LE_S, 1UL, 1UL, true)]
        [InlineData(InstructionType.I64_LE_S, 0UL, 1UL, true)]
        [InlineData(InstructionType.I64_LE_S, 0xFFFFFFFFFFFFFFFFUL, 1UL, true)]
        [InlineData(InstructionType.I64_GT_U, 1UL, 1UL, false)]
        [InlineData(InstructionType.I64_GT_U, 1UL, 0UL, true)]
        [InlineData(InstructionType.I64_GT_U, 1UL, 0xFFFFFFFFFFFFFFFFUL, false)]
        [InlineData(InstructionType.I64_GT_S, 1UL, 1UL, false)]
        [InlineData(InstructionType.I64_GT_S, 1UL, 0UL, true)]
        [InlineData(InstructionType.I64_GT_S, 1UL, 0xFFFFFFFFFFFFFFFFUL, true)]
        [InlineData(InstructionType.I64_GE_U, 1UL, 2UL, false)]
        [InlineData(InstructionType.I64_GE_U, 1UL, 1UL, true)]
        [InlineData(InstructionType.I64_GE_U, 1UL, 0UL, true)]
        [InlineData(InstructionType.I64_GE_U, 1UL, 0xFFFFFFFFFFFFFFFFUL, false)]
        [InlineData(InstructionType.I64_GE_S, 1UL, 2UL, false)]
        [InlineData(InstructionType.I64_GE_S, 1UL, 1UL, true)]
        [InlineData(InstructionType.I64_GE_S, 1UL, 0UL, true)]
        [InlineData(InstructionType.I64_GE_S, 1UL, 0xFFFFFFFFFFFFFFFFUL, true)]
        public void TestI64Relops(InstructionType insn, ulong v1, ulong v2, bool expected)
        {
            // 0: I64_CONST v1
            // 1: I64_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, I64Const(v1), I64Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Theory]
        [InlineData(InstructionType.I32_EXTEND8_S, 0x01U, 0x01U)]
        [InlineData(InstructionType.I32_EXTEND8_S, 0xF1U, 0xFFFFFFF1U)]
        [InlineData(InstructionType.I32_EXTEND16_S, 0xFF1U, 0xFF1U)]
        [InlineData(InstructionType.I32_EXTEND16_S, 0xFFF1U, 0xFFFFFFF1U)]
        public void TestI32ExtendOps(InstructionType insn, uint v, uint expected)
        {
            // 0: I32_CONST v
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I32Const(v), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u32));
        }

        [Theory]
        [InlineData(InstructionType.I64_EXTEND8_S, 0x01UL, 0x01UL)]
        [InlineData(InstructionType.I64_EXTEND8_S, 0xF1UL, 0xFFFFFFFFFFFFFFF1UL)]
        [InlineData(InstructionType.I64_EXTEND16_S, 0xFF1UL, 0xFF1UL)]
        [InlineData(InstructionType.I64_EXTEND16_S, 0xFFF1UL, 0xFFFFFFFFFFFFFFF1UL)]
        [InlineData(InstructionType.I64_EXTEND32_S, 0xFFFFFF1UL, 0xFFFFFF1UL)]
        [InlineData(InstructionType.I64_EXTEND32_S, 0xFFFFFFF1UL, 0xFFFFFFFFFFFFFFF1UL)]
        public void TestI64ExtendOps(InstructionType insn, ulong v, ulong expected)
        {
            // 0: I64_CONST v
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I64Const(v), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u64));
        }

        [Theory]
        [InlineData(0xFFUL, 0xFFU)]
        [InlineData(0x1FFFFFFFFUL, 0xFFFFFFFFU)]
        public void TestI32WrapI64(ulong v, uint expected)
        {
            // 0: I64_CONST v
            // 1: I32_WRAP_I64
            // 2: NOP
            machine.SetProgram(0, I64Const(v), Insn(InstructionType.I32_WRAP_I64), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u32));
        }

        [Theory]
        [InlineData(InstructionType.I64_EXTEND_I32_S, 0xFFU, 0xFFUL)]
        [InlineData(InstructionType.I64_EXTEND_I32_S, 0xFFFFFFFFU, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(InstructionType.I64_EXTEND_I32_U, 0xFFU, 0xFFUL)]
        [InlineData(InstructionType.I64_EXTEND_I32_U, 0xFFFFFFFFU, 0xFFFFFFFFUL)]
        public void TestI64ExtendI32(InstructionType insn, uint v, ulong expected)
        {
            // 0: I32_CONST v
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I32Const(v), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.u64));
        }

        [Theory]
        [InlineData(InstructionType.F32_ABS, 4f, 4f)]
        [InlineData(InstructionType.F32_ABS, -4f, 4f)]
        [InlineData(InstructionType.F32_NEG, 4f, -4f)]
        [InlineData(InstructionType.F32_SQRT, 4f, 2f)]
        [InlineData(InstructionType.F32_SQRT, -4f, Single.NaN)]
        [InlineData(InstructionType.F32_CEIL, 2.2f, 3f)]
        [InlineData(InstructionType.F32_CEIL, -2.2f, -2f)]
        [InlineData(InstructionType.F32_FLOOR, 2.2f, 2f)]
        [InlineData(InstructionType.F32_FLOOR, -2.2f, -3f)]
        [InlineData(InstructionType.F32_TRUNC, 2.2f, 2f)]
        [InlineData(InstructionType.F32_TRUNC, -2.2f, -2f)]
        [InlineData(InstructionType.F32_NEAREST, -4.5f, -4f)]
        [InlineData(InstructionType.F32_NEAREST, -5.5f, -6f)]
        public void TestF32Unops(InstructionType insn, float v, float expected)
        {
            // 0: F32_CONST v
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F32Const(v), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.f32));
        }

        [Theory]
        [InlineData(InstructionType.F64_ABS, 4f, 4f)]
        [InlineData(InstructionType.F64_ABS, -4f, 4f)]
        [InlineData(InstructionType.F64_NEG, 4f, -4f)]
        [InlineData(InstructionType.F64_SQRT, 4f, 2f)]
        [InlineData(InstructionType.F64_SQRT, -4f, Single.NaN)]
        [InlineData(InstructionType.F64_CEIL, 2.2f, 3f)]
        [InlineData(InstructionType.F64_CEIL, -2.2f, -2f)]
        [InlineData(InstructionType.F64_FLOOR, 2.2f, 2f)]
        [InlineData(InstructionType.F64_FLOOR, -2.2f, -3f)]
        [InlineData(InstructionType.F64_TRUNC, 2.2f, 2f)]
        [InlineData(InstructionType.F64_TRUNC, -2.2f, -2f)]
        [InlineData(InstructionType.F64_NEAREST, -4.5f, -4f)]
        [InlineData(InstructionType.F64_NEAREST, -5.5f, -6f)]
        public void TestF64Unops(InstructionType insn, double v, double expected)
        {
            // 0: F64_CONST v
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F64Const(v), Insn(insn), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.f64));
        }

        [Theory]
        [InlineData(InstructionType.F32_ADD, 1f, 2f, 3f)]
        [InlineData(InstructionType.F32_SUB, 1f, 2f, -1f)]
        [InlineData(InstructionType.F32_MUL, 3f, 2f, 6f)]
        [InlineData(InstructionType.F32_DIV, 1f, 2f, 0.5f)]
        [InlineData(InstructionType.F32_MIN, 1f, 2f, 1f)]
        [InlineData(InstructionType.F32_MIN, 2f, 1f, 1f)]
        [InlineData(InstructionType.F32_MAX, 1f, 2f, 2f)]
        [InlineData(InstructionType.F32_MAX, 2f, 1f, 2f)]
        [InlineData(InstructionType.F32_COPYSIGN, 2f, 1f, 2f)]
        [InlineData(InstructionType.F32_COPYSIGN, -2f, -1f, -2f)]
        [InlineData(InstructionType.F32_COPYSIGN, 2f, -1f, -2f)]
        [InlineData(InstructionType.F32_COPYSIGN, -2f, 1f, 2f)]
        public void TestF32Binops(InstructionType insn, float v1, float v2, float expected)
        {
            // 0: F32_CONST v1
            // 1: F32_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, F32Const(v1), F32Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.f32));
        }

        [Theory]
        [InlineData(InstructionType.F64_ADD, 1f, 2f, 3f)]
        [InlineData(InstructionType.F64_SUB, 1f, 2f, -1f)]
        [InlineData(InstructionType.F64_MUL, 3f, 2f, 6f)]
        [InlineData(InstructionType.F64_DIV, 1f, 2f, 0.5f)]
        [InlineData(InstructionType.F64_MIN, 1f, 2f, 1f)]
        [InlineData(InstructionType.F64_MIN, 2f, 1f, 1f)]
        [InlineData(InstructionType.F64_MAX, 1f, 2f, 2f)]
        [InlineData(InstructionType.F64_MAX, 2f, 1f, 2f)]
        [InlineData(InstructionType.F64_COPYSIGN, 2f, 1f, 2f)]
        [InlineData(InstructionType.F64_COPYSIGN, -2f, -1f, -2f)]
        [InlineData(InstructionType.F64_COPYSIGN, 2f, -1f, -2f)]
        [InlineData(InstructionType.F64_COPYSIGN, -2f, 1f, 2f)]
        public void TestF64Binops(InstructionType insn, double v1, double v2, float expected)
        {
            // 0: F64_CONST v1
            // 1: F64_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, F64Const(v1), F64Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.f64));
        }

        [Theory]
        [InlineData(InstructionType.F32_EQ, 1f, 1f, true)]
        [InlineData(InstructionType.F32_EQ, 1f, 0f, false)]
        [InlineData(InstructionType.F32_NE, 1f, 1f, false)]
        [InlineData(InstructionType.F32_NE, 1f, 0f, true)]
        [InlineData(InstructionType.F32_LT, 1f, 1f, false)]
        [InlineData(InstructionType.F32_LT, 0f, 1f, true)]
        [InlineData(InstructionType.F32_LT, 1f, 0f, false)]
        [InlineData(InstructionType.F32_LE, 1f, 1f, true)]
        [InlineData(InstructionType.F32_LE, 0f, 1f, true)]
        [InlineData(InstructionType.F32_LE, 1f, 0f, false)]
        [InlineData(InstructionType.F32_GT, 1f, 1f, false)]
        [InlineData(InstructionType.F32_GT, 0f, 1f, false)]
        [InlineData(InstructionType.F32_GT, 1f, 0f, true)]
        [InlineData(InstructionType.F32_GE, 1f, 1f, true)]
        [InlineData(InstructionType.F32_GE, 0f, 1f, false)]
        [InlineData(InstructionType.F32_GE, 1f, 0f, true)]
        public void TestF32Relops(InstructionType insn, float v1, float v2, bool expected)
        {
            // 0: F32_CONST v1
            // 1: F32_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, F32Const(v1), F32Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Theory]
        [InlineData(InstructionType.F64_EQ, 1f, 1f, true)]
        [InlineData(InstructionType.F64_EQ, 1f, 0f, false)]
        [InlineData(InstructionType.F64_NE, 1f, 1f, false)]
        [InlineData(InstructionType.F64_NE, 1f, 0f, true)]
        [InlineData(InstructionType.F64_LT, 1f, 1f, false)]
        [InlineData(InstructionType.F64_LT, 0f, 1f, true)]
        [InlineData(InstructionType.F64_LT, 1f, 0f, false)]
        [InlineData(InstructionType.F64_LE, 1f, 1f, true)]
        [InlineData(InstructionType.F64_LE, 0f, 1f, true)]
        [InlineData(InstructionType.F64_LE, 1f, 0f, false)]
        [InlineData(InstructionType.F64_GT, 1f, 1f, false)]
        [InlineData(InstructionType.F64_GT, 0f, 1f, false)]
        [InlineData(InstructionType.F64_GT, 1f, 0f, true)]
        [InlineData(InstructionType.F64_GE, 1f, 1f, true)]
        [InlineData(InstructionType.F64_GE, 0f, 1f, false)]
        [InlineData(InstructionType.F64_GE, 1f, 0f, true)]
        public void TestF64Relops(InstructionType insn, double v1, double v2, bool expected)
        {
            // 0: F64_CONST v1
            // 1: F64_CONST v2
            // 2: insn
            // 3: NOP
            machine.SetProgram(0, F64Const(v1), F64Const(v2), Insn(insn), Nop());

            machine.Step(3);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(expected, e.Bool));
        }

        [Fact]
        public void TestI32TruncF32S()
        {
            // 0: F32_CONST -1.58f
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F32Const(-1.58f), Insn(InstructionType.I32_TRUNC_F32_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(0xFFFFFFFFU, e.u32));
        }

        [Fact]
        public void TestI32TruncF32U()
        {
            // 0: F32_CONST 2200000000.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                F32Const(2200000000.58f),
                Insn(InstructionType.I32_TRUNC_F32_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(0x83215600U, e.u32));
        }

        [Fact]
        public void TestI32TruncF64S()
        {
            // 0: F64_CONST -1.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F64Const(-1.58), Insn(InstructionType.I32_TRUNC_F64_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(0xFFFFFFFFU, e.u32));
        }

        [Fact]
        public void TestI32TruncF64U()
        {
            // 0: F64_CONST 2200000000.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                F64Const(2200000000.58),
                Insn(InstructionType.I32_TRUNC_F64_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(0x83215600U, e.u32));
        }

        [Fact]
        public void TestI64TruncF32S()
        {
            // 0: F32_CONST -1.58f
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F32Const(-1.58f), Insn(InstructionType.I64_TRUNC_F32_S), Nop());

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(0xFFFFFFFFFFFFFFFFU, e.u64)
            );
        }

        [Fact]
        public void TestI64TruncF32U()
        {
            // 0: F32_CONST 9.5e18
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F32Const(9.5e18f), Insn(InstructionType.I64_TRUNC_F32_U), Nop());

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(0x83D6C80000000000UL, e.u64)
            );
        }

        [Fact]
        public void TestI64TruncF64S()
        {
            // 0: F64_CONST -1.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F64Const(-1.58), Insn(InstructionType.I64_TRUNC_F64_S), Nop());

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(0xFFFFFFFFFFFFFFFFU, e.u64)
            );
        }

        [Fact]
        public void TestI64TruncF64U()
        {
            // 0: F64_CONST 9.5e18
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F64Const(9.5e18), Insn(InstructionType.I64_TRUNC_F64_U), Nop());

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(0x83D6C7AAB6360000UL, e.u64)
            );
        }

        [Fact]
        public void TestF32DemoteF64()
        {
            // 0: F64_CONST 1.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F64Const(1.58), Insn(InstructionType.F32_DEMOTE_F64), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(1.58f, e.f32));
        }

        [Fact]
        public void TestF64PromoteF32()
        {
            // 0: F32_CONST 1.58f  // There's no exact representation for this.
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, F32Const(1.58f), Insn(InstructionType.F64_PROMOTE_F32), Nop());

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(1.5800000429153442, e.f64)
            );
        }

        [Fact]
        public void TestF32ConvertI32S()
        {
            // 0: I32_CONST -1
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I32Const(-1), Insn(InstructionType.F32_CONVERT_I32_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1f, e.f32));
        }

        [Fact]
        public void TestF32ConvertI32U()
        {
            // 0: I32_CONST 0xFFFFFFFF
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(0xFFFFFFFFU),
                Insn(InstructionType.F32_CONVERT_I32_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(4294967296f, e.f32));
        }

        [Fact]
        public void TestF32ConvertI64S()
        {
            // 0: I64_CONST -1
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I64Const(-1), Insn(InstructionType.F32_CONVERT_I64_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1f, e.f32));
        }

        [Fact]
        public void TestF32ConvertI64U()
        {
            // 0: I64_CONST 0xFFFFFFFFFFFFFFFF
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I64Const(0xFFFFFFFFFFFFFFFFUL),
                Insn(InstructionType.F32_CONVERT_I64_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(18446744073709551616f, e.f32)
            );
        }

        [Fact]
        public void TestF64ConvertI32S()
        {
            // 0: I32_CONST -1
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I32Const(-1), Insn(InstructionType.F64_CONVERT_I32_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1, e.f64));
        }

        [Fact]
        public void TestF64ConvertI32U()
        {
            // 0: I32_CONST 0xFFFFFFFF
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(0xFFFFFFFFU),
                Insn(InstructionType.F64_CONVERT_I32_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(4294967295, e.f64));
        }

        [Fact]
        public void TestF64ConvertI64S()
        {
            // 0: I64_CONST -1
            // 1: insn
            // 2: NOP
            machine.SetProgram(0, I64Const(-1), Insn(InstructionType.F64_CONVERT_I64_S), Nop());

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1, e.f64));
        }

        [Fact]
        public void TestF64ConvertI64U()
        {
            // 0: I64_CONST 0xFFFFFFFFFFFFFFFF
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I64Const(0xFFFFFFFFFFFFFFFFUL),
                Insn(InstructionType.F64_CONVERT_I64_U),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(18446744073709551615.0, e.f64)
            );
        }

        [Fact]
        public void TestI32ReinterpretF32()
        {
            // 0: F32_CONST -1.5f
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                F32Const(-1.5f),
                Insn(InstructionType.I32_REINTERPRET_F32),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(0xBFC00000U, e.u32));
        }

        [Fact]
        public void TestI64ReinterpretF64()
        {
            // 0: F64_CONST -1.58
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                F64Const(-1.58),
                Insn(InstructionType.I64_REINTERPRET_F64),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(0xBFF947AE147AE148UL, e.u64)
            );
        }

        [Fact]
        public void TestF32ReinterpretI32()
        {
            // 0: I32_CONST 0xBFC00000
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(0xBFC00000U),
                Insn(InstructionType.F32_REINTERPRET_I32),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1.5f, e.f32));
        }

        [Fact]
        public void TestF64ReinterpretI64()
        {
            // 0: I64_CONST 0xBFF947AE147AE148
            // 1: insn
            // 2: NOP
            machine.SetProgram(
                0,
                I64Const(0xBFF947AE147AE148UL),
                Insn(InstructionType.F64_REINTERPRET_I64),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, e => Assert.Equal(-1.58, e.f64));
        }
    }
}
