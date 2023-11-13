using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using LEB128;
using Xunit;

namespace Derg
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

    public class InstructionsTests
    {
        [Fact]
        public void TestDecodeByteInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.REF_NULL);
            stream.WriteByte(0xF1);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.REF_NULL, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0xF1U, insn.Operands[0].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeByte8Instruction()
        {
            // Also tests decoding of opcodes > 0xFF.
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.V128_CONST);
            stream.Write(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0, 8);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.V128_CONST, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0x8877665544332211UL, insn.Operands[0].value.AsI64_U());
        }

        [Fact]
        public void TestDecodeU32Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.BR);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.BR, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeU32X2Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.TABLE_INIT);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.WriteLEB128Unsigned(0x99887766U);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.TABLE_INIT, insn.Type);
            Assert.Equal(2, insn.Operands.Length);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
            Assert.Equal(0x99887766U, insn.Operands[1].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeMemargInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I32_LOAD);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.WriteLEB128Unsigned(0x99887766U);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I32_LOAD, insn.Type);
            Assert.Equal(2, insn.Operands.Length);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
            Assert.Equal(0x99887766U, insn.Operands[1].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeMemargLaneInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.V128_LOAD8_LANE);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.WriteLEB128Unsigned(0x99887766U);
            stream.WriteLEB128Unsigned(0xAABBCCDDU);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.V128_LOAD8_LANE, insn.Type);
            Assert.Equal(3, insn.Operands.Length);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
            Assert.Equal(0x99887766U, insn.Operands[1].value.AsI32_U());
            Assert.Equal(0xAABBCCDDU, insn.Operands[2].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeLaneInstruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I8X16_EXTRACT_LANE_S);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I8X16_EXTRACT_LANE_S, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
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
            Assert.Equal(16, insn.Operands.Length);
            Assert.Equal(1U, insn.Operands[0].value.AsI32_U());
            Assert.Equal(2U, insn.Operands[1].value.AsI32_U());
            Assert.Equal(3U, insn.Operands[2].value.AsI32_U());
            Assert.Equal(4U, insn.Operands[3].value.AsI32_U());
            Assert.Equal(5U, insn.Operands[4].value.AsI32_U());
            Assert.Equal(6U, insn.Operands[5].value.AsI32_U());
            Assert.Equal(7U, insn.Operands[6].value.AsI32_U());
            Assert.Equal(8U, insn.Operands[7].value.AsI32_U());
            Assert.Equal(9U, insn.Operands[8].value.AsI32_U());
            Assert.Equal(10U, insn.Operands[9].value.AsI32_U());
            Assert.Equal(11U, insn.Operands[10].value.AsI32_U());
            Assert.Equal(12U, insn.Operands[11].value.AsI32_U());
            Assert.Equal(13U, insn.Operands[12].value.AsI32_U());
            Assert.Equal(14U, insn.Operands[13].value.AsI32_U());
            Assert.Equal(15U, insn.Operands[14].value.AsI32_U());
            Assert.Equal(16U, insn.Operands[15].value.AsI32_U());
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
            Assert.Equal(4, insn.Operands.Length);
            Assert.Equal(1U, insn.Operands[0].value.AsI32_U());
            Assert.Equal(2U, insn.Operands[1].value.AsI32_U());
            Assert.Equal(3U, insn.Operands[2].value.AsI32_U());
            Assert.Equal(4U, insn.Operands[3].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeI32Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I32_CONST);
            stream.WriteLEB128Unsigned(0x11223344U);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I32_CONST, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0x11223344U, insn.Operands[0].value.AsI32_U());
        }

        [Fact]
        public void TestDecodeI64Instruction()
        {
            MemoryStream stream = new MemoryStream();

            stream.WriteOpcode(InstructionType.I64_CONST);
            stream.WriteLEB128Unsigned(0x1122334455667788UL);
            stream.Position = 0;

            UnflattenedInstruction insn = UnflattenedInstruction.Decode(new BinaryReader(stream));

            Assert.Equal(InstructionType.I64_CONST, insn.Type);
            Assert.Single(insn.Operands);
            Assert.Equal(0x1122334455667788UL, insn.Operands[0].value.AsI64_U());
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
            Assert.Equal(3.14159f, insn.Operands[0].value.AsF32());
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
            Assert.Single(insn.Operands);
            Assert.Equal(3.14159, insn.Operands[0].value.AsF64());
        }

    }
}
