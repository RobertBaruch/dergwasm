using System;
using System.Collections.Generic;
using System.IO;
using Derg;
using LEB128;
using Xunit;

namespace DergwasmTests
{
    internal static class Extensions
    {
        internal static void WriteOpcode(this MemoryStream stream, InstructionType opcode)
        {
            if ((uint)opcode <= 0xFF)
            {
                stream.WriteByte((byte)opcode);
                return;
            }
            stream.WriteByte((byte)((uint)opcode >> 8));
            stream.WriteLEB128Unsigned((uint)opcode & 0xFF);
        }
    }

    public class InstructionDecodingTests
    {
        [Fact]
        public void TestDecodeNoOperandInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I32_ADD);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I32_ADD, insn.Type);
            Assert.Empty(insn.Operands);
        }

        [Fact]
        public void TestDecodeByteInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.REF_NULL);
            stream.WriteByte(1);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.REF_NULL, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(1U, e.value.u32));
        }

        [Fact]
        public void TestDecodeByte8Instruction()
        {
            // Also tests decoding of opcodes > 0xFF.
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.V128_CONST);
            stream.Write(BitConverter.GetBytes(0x8877665544332211UL), 0, 8);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.V128_CONST, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(0x8877665544332211UL, e.value.u64));
        }

        [Fact]
        public void TestDecodeU32Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BR);
            stream.WriteLEB128Unsigned(1);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BR, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(1U, e.value.u32));
        }

        [Fact]
        public void TestDecodeU32X2Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.TABLE_INIT);
            stream.WriteLEB128Unsigned(1);
            stream.WriteLEB128Unsigned(2);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.TABLE_INIT, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32)
            );
        }

        [Fact]
        public void TestDecodeMemargInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I32_LOAD);
            stream.WriteLEB128Unsigned(1);
            stream.WriteLEB128Unsigned(2);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I32_LOAD, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32)
            );
        }

        [Fact]
        public void TestDecodeMemargLaneInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.V128_LOAD8_LANE);
            stream.WriteLEB128Unsigned(1);
            stream.WriteLEB128Unsigned(2);
            stream.WriteLEB128Unsigned(3);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.V128_LOAD8_LANE, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32),
                e => Assert.Equal(3U, e.value.u32)
            );
        }

        [Fact]
        public void TestDecodeLaneInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I8X16_EXTRACT_LANE_S);
            stream.WriteLEB128Unsigned(1);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I8X16_EXTRACT_LANE_S, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(1U, e.value.u32));
        }

        [Fact]
        public void TestDecodeLane8Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I8X16_SHUFFLE);
            stream.WriteLEB128Unsigned(1);
            stream.WriteLEB128Unsigned(2);
            stream.WriteLEB128Unsigned(3);
            stream.WriteLEB128Unsigned(4);
            stream.WriteLEB128Unsigned(5);
            stream.WriteLEB128Unsigned(6);
            stream.WriteLEB128Unsigned(7);
            stream.WriteLEB128Unsigned(8);
            stream.WriteLEB128Unsigned(9);
            stream.WriteLEB128Unsigned(10);
            stream.WriteLEB128Unsigned(11);
            stream.WriteLEB128Unsigned(12);
            stream.WriteLEB128Unsigned(13);
            stream.WriteLEB128Unsigned(14);
            stream.WriteLEB128Unsigned(15);
            stream.WriteLEB128Unsigned(16);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I8X16_SHUFFLE, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32),
                e => Assert.Equal(3U, e.value.u32),
                e => Assert.Equal(4U, e.value.u32),
                e => Assert.Equal(5U, e.value.u32),
                e => Assert.Equal(6U, e.value.u32),
                e => Assert.Equal(7U, e.value.u32),
                e => Assert.Equal(8U, e.value.u32),
                e => Assert.Equal(9U, e.value.u32),
                e => Assert.Equal(10U, e.value.u32),
                e => Assert.Equal(11U, e.value.u32),
                e => Assert.Equal(12U, e.value.u32),
                e => Assert.Equal(13U, e.value.u32),
                e => Assert.Equal(14U, e.value.u32),
                e => Assert.Equal(15U, e.value.u32),
                e => Assert.Equal(16U, e.value.u32)
            );
        }

        [Fact]
        public void TestDecodeValtypeVectorInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.SELECT_VEC);
            stream.WriteLEB128Unsigned(4);
            stream.WriteByte(1);
            stream.WriteByte(2);
            stream.WriteByte(3);
            stream.WriteByte(4);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.SELECT_VEC, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32),
                e => Assert.Equal(3U, e.value.u32),
                e => Assert.Equal(4U, e.value.u32)
            );
        }

        [Theory]
        [InlineData(0x12345678U)]
        [InlineData(0xFFFFFFFFU)]
        public void TestDecodeI32Instruction(uint data)
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I32_CONST);
            stream.WriteLEB128Signed((int)data);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I32_CONST, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(data, e.value.u32));
        }

        [Theory]
        [InlineData(0x123456789ABCDEF0UL)]
        [InlineData(0xFFFFFFFFFFFFFFFFUL)]
        public void TestDecodeI64Instruction(ulong data)
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I64_CONST);
            stream.WriteLEB128Unsigned(data);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I64_CONST, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(data, e.value.u64));
        }

        [Fact]
        public void TestDecodeF32Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.F32_CONST);
            stream.Write(BitConverter.GetBytes(3.14159f), 0, 4);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.F32_CONST, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Collection(insn.Operands, e => Assert.Equal(3.14159f, e.value.f32));
        }

        [Fact]
        public void TestDecodeF64Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.F64_CONST);
            stream.Write(BitConverter.GetBytes(3.14159), 0, 8);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.F64_CONST, insn.Type);
            Assert.Collection(insn.Operands, e => Assert.Equal(3.14159, e.value.f64));
        }

        [Fact]
        public void TestDecodeSwitchInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BR_TABLE);
            stream.WriteLEB128Unsigned(4);
            stream.WriteLEB128Unsigned(1);
            stream.WriteLEB128Unsigned(2);
            stream.WriteLEB128Unsigned(3);
            stream.WriteLEB128Unsigned(4);
            stream.WriteLEB128Unsigned(5);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BR_TABLE, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1U, e.value.u32),
                e => Assert.Equal(2U, e.value.u32),
                e => Assert.Equal(3U, e.value.u32),
                e => Assert.Equal(4U, e.value.u32),
                e => Assert.Equal(5U, e.value.u32)
            );
        }

        [Fact]
        public void TestDecodeBlockInstruction_VoidBlock()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BLOCK);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BLOCK, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(BlockType.VOID_BLOCK, e.value.GetBlockType())
            );
            Assert.Collection(insn.Operands, e => Assert.IsType<UnflattenedBlockOperand>(e));
            Assert.Collection(
                ((UnflattenedBlockOperand)insn.Operands[0]).instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }

        [Fact]
        public void TestDecodeBlockInstruction_ReturningBlock()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BLOCK);
            stream.WriteLEB128Signed((int)Derg.ValueType.F32 - 0x80); // returning block, returns F32.
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BLOCK, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(BlockType.RETURNING_BLOCK, e.value.GetBlockType())
            );
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(Derg.ValueType.F32, e.value.GetReturningBlockValueType())
            );
            Assert.Collection(insn.Operands, e => Assert.IsType<UnflattenedBlockOperand>(e));
            Assert.Collection(
                ((UnflattenedBlockOperand)insn.Operands[0]).instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }

        [Fact]
        public void TestDecodeBlockInstruction_TypedBlock()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BLOCK);
            stream.WriteLEB128Signed(1); // typed block, type index 1.
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BLOCK, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(BlockType.TYPED_BLOCK, e.value.GetBlockType())
            );
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(1, e.value.GetReturningBlockTypeIndex())
            );
            Assert.Collection(insn.Operands, e => Assert.IsType<UnflattenedBlockOperand>(e));
            Assert.Collection(
                ((UnflattenedBlockOperand)insn.Operands[0]).instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }

        [Fact]
        public void TestDecodeBlockInstruction_IfElse()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.IF);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.ELSE);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.IF, insn.Type);
            Assert.Collection(
                insn.Operands,
                e => Assert.Equal(BlockType.VOID_BLOCK, e.value.GetBlockType())
            );
            Assert.Collection(insn.Operands, e => Assert.IsType<UnflattenedBlockOperand>(e));
            Assert.Collection(
                ((UnflattenedBlockOperand)insn.Operands[0]).instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.ELSE, e.Type)
            );
            Assert.Collection(
                ((UnflattenedBlockOperand)insn.Operands[0]).else_instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }
    }

    public class InstructionFlattenTests
    {
        [Fact]
        public void TestDecodeExpr()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<UnflattenedInstruction> instructions = Expr.Decode(new BinaryReader(stream));

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }

        [Fact]
        public void TestNonBlockInstructions()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<Instruction> instructions = Expr.Decode(new BinaryReader(stream)).Flatten(0);

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.NOP, e.Type),
                e => Assert.Equal(InstructionType.END, e.Type)
            );
        }

        [Fact]
        public void TestBlockInstructionTarget()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BLOCK);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<Instruction> instructions = Expr.Decode(new BinaryReader(stream)).Flatten(100);

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.BLOCK, e.Type), // 100
                e => Assert.Equal(InstructionType.NOP, e.Type), // 101
                e => Assert.Equal(InstructionType.END, e.Type), // 102
                e => Assert.Equal(InstructionType.NOP, e.Type), // 103
                e => Assert.Equal(InstructionType.END, e.Type) // 104
            );

            Assert.Equal(103, instructions[0].Operands[0].GetTarget());
        }

        [Fact]
        public void TestLoopInstructionTarget()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.LOOP);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<Instruction> instructions = Expr.Decode(new BinaryReader(stream)).Flatten(100);

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.LOOP, e.Type), // 100
                e => Assert.Equal(InstructionType.NOP, e.Type), // 101
                e => Assert.Equal(InstructionType.END, e.Type), // 102
                e => Assert.Equal(InstructionType.NOP, e.Type), // 103
                e => Assert.Equal(InstructionType.END, e.Type) // 104
            );

            Assert.Equal(100, instructions[0].Operands[0].GetTarget());
        }

        [Fact]
        public void TestIfInstructionTarget()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.IF);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<Instruction> instructions = Expr.Decode(new BinaryReader(stream)).Flatten(100);

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.IF, e.Type), // 100
                e => Assert.Equal(InstructionType.NOP, e.Type), // 101
                e => Assert.Equal(InstructionType.END, e.Type), // 102
                e => Assert.Equal(InstructionType.NOP, e.Type), // 103
                e => Assert.Equal(InstructionType.END, e.Type) // 104
            );

            Assert.Equal(103, instructions[0].Operands[0].GetTarget());
            Assert.Equal(103, instructions[0].Operands[0].GetElseTarget());
        }

        [Fact]
        public void TestIfElseInstructionTarget()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.IF);
            stream.WriteLEB128Signed(-0x40); // void block
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.ELSE);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.WriteOpcode(InstructionType.NOP);
            stream.WriteOpcode(InstructionType.END);
            stream.Position = 0;

            List<Instruction> instructions = Expr.Decode(new BinaryReader(stream)).Flatten(100);

            Assert.Collection(
                instructions,
                e => Assert.Equal(InstructionType.IF, e.Type), // 100
                e => Assert.Equal(InstructionType.NOP, e.Type), // 101
                e => Assert.Equal(InstructionType.ELSE, e.Type), // 102
                e => Assert.Equal(InstructionType.NOP, e.Type), // 103
                e => Assert.Equal(InstructionType.END, e.Type), // 104
                e => Assert.Equal(InstructionType.NOP, e.Type), // 105
                e => Assert.Equal(InstructionType.END, e.Type) // 106
            );

            Assert.Equal(105, instructions[0].Operands[0].GetTarget());
            Assert.Equal(103, instructions[0].Operands[0].GetElseTarget());
        }
    }
}
