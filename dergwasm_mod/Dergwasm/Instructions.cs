using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LEB128;

namespace Derg
{
    public enum InstructionType : uint
    {
        // Control instructions
        UNREACHABLE = 0x00,
        NOP = 0x01,
        BLOCK = 0x02,
        LOOP = 0x03,
        IF = 0x04,
        ELSE = 0x05,
        END = 0x0B,
        BR = 0x0C,
        BR_IF = 0x0D,
        BR_TABLE = 0x0E,
        RETURN = 0x0F,
        CALL = 0x10,
        CALL_INDIRECT = 0x11,

        // Reference instructions
        REF_NULL = 0xD0,
        REF_IS_NULL = 0xD1,
        REF_FUNC = 0xD2,

        // Parametric instructions
        DROP = 0x1A,
        SELECT = 0x1B,
        SELECT_VEC = 0x1C,

        // Variable instructions
        LOCAL_GET = 0x20,
        LOCAL_SET = 0x21,
        LOCAL_TEE = 0x22,
        GLOBAL_GET = 0x23,
        GLOBAL_SET = 0x24,

        // Table instructions
        TABLE_GET = 0x25,
        TABLE_SET = 0x26,
        TABLE_INIT = 0xFC0C,
        ELEM_DROP = 0xFC0D,
        TABLE_COPY = 0xFC0E,
        TABLE_GROW = 0xFC0F,
        TABLE_SIZE = 0xFC10,
        TABLE_FILL = 0xFC11,

        // Memory instructions
        I32_LOAD = 0x28,
        I64_LOAD = 0x29,
        F32_LOAD = 0x2A,
        F64_LOAD = 0x2B,
        I32_LOAD8_S = 0x2C,
        I32_LOAD8_U = 0x2D,
        I32_LOAD16_S = 0x2E,
        I32_LOAD16_U = 0x2F,
        I64_LOAD8_S = 0x30,
        I64_LOAD8_U = 0x31,
        I64_LOAD16_S = 0x32,
        I64_LOAD16_U = 0x33,
        I64_LOAD32_S = 0x34,
        I64_LOAD32_U = 0x35,
        I32_STORE = 0x36,
        I64_STORE = 0x37,
        F32_STORE = 0x38,
        F64_STORE = 0x39,
        I32_STORE8 = 0x3A,
        I32_STORE16 = 0x3B,
        I64_STORE8 = 0x3C,
        I64_STORE16 = 0x3D,
        I64_STORE32 = 0x3E,
        MEMORY_SIZE = 0x3F,
        MEMORY_GROW = 0x40,
        MEMORY_INIT = 0xFC08,
        DATA_DROP = 0xFC09,
        MEMORY_COPY = 0xFC0A,
        MEMORY_FILL = 0xFC0B,

        // Numeric instructions
        I32_CONST = 0x41,
        I64_CONST = 0x42,
        F32_CONST = 0x43,
        F64_CONST = 0x44,

        I32_EQZ = 0x45,
        I32_EQ = 0x46,
        I32_NE = 0x47,
        I32_LT_S = 0x48,
        I32_LT_U = 0x49,
        I32_GT_S = 0x4A,
        I32_GT_U = 0x4B,
        I32_LE_S = 0x4C,
        I32_LE_U = 0x4D,
        I32_GE_S = 0x4E,
        I32_GE_U = 0x4F,

        I64_EQZ = 0x50,
        I64_EQ = 0x51,
        I64_NE = 0x52,
        I64_LT_S = 0x53,
        I64_LT_U = 0x54,
        I64_GT_S = 0x55,
        I64_GT_U = 0x56,
        I64_LE_S = 0x57,
        I64_LE_U = 0x58,
        I64_GE_S = 0x59,
        I64_GE_U = 0x5A,
        F32_EQ = 0x5B,
        F32_NE = 0x5C,
        F32_LT = 0x5D,
        F32_GT = 0x5E,
        F32_LE = 0x5F,
        F32_GE = 0x60,

        F64_EQ = 0x61,
        F64_NE = 0x62,
        F64_LT = 0x63,
        F64_GT = 0x64,
        F64_LE = 0x65,
        F64_GE = 0x66,

        I32_CLZ = 0x67,
        I32_CTZ = 0x68,
        I32_POPCNT = 0x69,
        I32_ADD = 0x6A,
        I32_SUB = 0x6B,
        I32_MUL = 0x6C,
        I32_DIV_S = 0x6D,
        I32_DIV_U = 0x6E,
        I32_REM_S = 0x6F,
        I32_REM_U = 0x70,
        I32_AND = 0x71,
        I32_OR = 0x72,
        I32_XOR = 0x73,
        I32_SHL = 0x74,
        I32_SHR_S = 0x75,
        I32_SHR_U = 0x76,
        I32_ROTL = 0x77,
        I32_ROTR = 0x78,

        I64_CLZ = 0x79,
        I64_CTZ = 0x7A,
        I64_POPCNT = 0x7B,
        I64_ADD = 0x7C,
        I64_SUB = 0x7D,
        I64_MUL = 0x7E,
        I64_DIV_S = 0x7F,
        I64_DIV_U = 0x80,
        I64_REM_S = 0x81,
        I64_REM_U = 0x82,
        I64_AND = 0x83,
        I64_OR = 0x84,
        I64_XOR = 0x85,
        I64_SHL = 0x86,
        I64_SHR_S = 0x87,
        I64_SHR_U = 0x88,
        I64_ROTL = 0x89,
        I64_ROTR = 0x8A,

        F32_ABS = 0x8B,
        F32_NEG = 0x8C,
        F32_CEIL = 0x8D,
        F32_FLOOR = 0x8E,
        F32_TRUNC = 0x8F,
        F32_NEAREST = 0x90,
        F32_SQRT = 0x91,
        F32_ADD = 0x92,
        F32_SUB = 0x93,
        F32_MUL = 0x94,
        F32_DIV = 0x95,
        F32_MIN = 0x96,
        F32_MAX = 0x97,
        F32_COPYSIGN = 0x98,

        F64_ABS = 0x99,
        F64_NEG = 0x9A,
        F64_CEIL = 0x9B,
        F64_FLOOR = 0x9C,
        F64_TRUNC = 0x9D,
        F64_NEAREST = 0x9E,
        F64_SQRT = 0x9F,
        F64_ADD = 0xA0,
        F64_SUB = 0xA1,
        F64_MUL = 0xA2,
        F64_DIV = 0xA3,
        F64_MIN = 0xA4,
        F64_MAX = 0xA5,
        F64_COPYSIGN = 0xA6,

        I32_WRAP_I64 = 0xA7,
        I32_TRUNC_F32_S = 0xA8,
        I32_TRUNC_F32_U = 0xA9,
        I32_TRUNC_F64_S = 0xAA,
        I32_TRUNC_F64_U = 0xAB,
        I64_EXTEND_I32_S = 0xAC,
        I64_EXTEND_I32_U = 0xAD,
        I64_TRUNC_F32_S = 0xAE,
        I64_TRUNC_F32_U = 0xAF,
        I64_TRUNC_F64_S = 0xB0,
        I64_TRUNC_F64_U = 0xB1,
        F32_CONVERT_I32_S = 0xB2,
        F32_CONVERT_I32_U = 0xB3,
        F32_CONVERT_I64_S = 0xB4,
        F32_CONVERT_I64_U = 0xB5,
        F32_DEMOTE_F64 = 0xB6,
        F64_CONVERT_I32_S = 0xB7,
        F64_CONVERT_I32_U = 0xB8,
        F64_CONVERT_I64_S = 0xB9,
        F64_CONVERT_I64_U = 0xBA,
        F64_PROMOTE_F32 = 0xBB,
        I32_REINTERPRET_F32 = 0xBC,
        I64_REINTERPRET_F64 = 0xBD,
        F32_REINTERPRET_I32 = 0xBE,
        F64_REINTERPRET_I64 = 0xBF,

        I32_EXTEND8_S = 0xC0,
        I32_EXTEND16_S = 0xC1,
        I64_EXTEND8_S = 0xC2,
        I64_EXTEND16_S = 0xC3,
        I64_EXTEND32_S = 0xC4,

        I32_TRUNC_SAT_F32_S = 0xFC00,
        I32_TRUNC_SAT_F32_U = 0xFC01,
        I32_TRUNC_SAT_F64_S = 0xFC02,
        I32_TRUNC_SAT_F64_U = 0xFC03,
        I64_TRUNC_SAT_F32_S = 0xFC04,
        I64_TRUNC_SAT_F32_U = 0xFC05,
        I64_TRUNC_SAT_F64_S = 0xFC06,
        I64_TRUNC_SAT_F64_U = 0xFC07,

        // Vector instructions
        V128_LOAD = 0xFD00,
        V128_LOAD8X8_S = 0xFD01,
        V128_LOAD8X8_U = 0xFD02,
        V128_LOAD16X4_S = 0xFD03,
        V128_LOAD16X4_U = 0xFD04,
        V128_LOAD32X2_S = 0xFD05,
        V128_LOAD32X2_U = 0xFD06,
        V128_LOAD8_SPLAT = 0xFD07,
        V128_LOAD16_SPLAT = 0xFD08,
        V128_LOAD32_SPLAT = 0xFD09,
        V128_LOAD64_SPLAT = 0xFD0A,
        V128_LOAD32_ZERO = 0xFD5C,
        V128_LOAD64_ZERO = 0xFD5D,
        V128_STORE = 0xFD0B,
        V128_LOAD8_LANE = 0xFD54,
        V128_LOAD16_LANE = 0xFD55,
        V128_LOAD32_LANE = 0xFD56,
        V128_LOAD64_LANE = 0xFD57,
        V128_STORE8_LANE = 0xFD58,
        V128_STORE16_LANE = 0xFD59,
        V128_STORE32_LANE = 0xFD5A,
        V128_STORE64_LANE = 0xFD5B,

        V128_CONST = 0xFD0C,
        I8X16_SHUFFLE = 0xFD0D,

        I8X16_EXTRACT_LANE_S = 0xFD15,
        I8X16_EXTRACT_LANE_U = 0xFD16,
        I8X16_REPLACE_LANE = 0xFD17,
        I16X8_EXTRACT_LANE_S = 0xFD18,
        I16X8_EXTRACT_LANE_U = 0xFD19,
        I16X8_REPLACE_LANE = 0xFD1A,
        I32X4_EXTRACT_LANE = 0xFD1B,
        I32X4_REPLACE_LANE = 0xFD1C,
        I64X2_EXTRACT_LANE = 0xFD1D,
        I64X2_REPLACE_LANE = 0xFD1E,
        F32X4_EXTRACT_LANE = 0xFD1F,
        F32X4_REPLACE_LANE = 0xFD20,
        F64X2_EXTRACT_LANE = 0xFD21,
        F64X2_REPLACE_LANE = 0xFD22,

        I8X16_SWIZZLE = 0xFD0E,
        I8X16_SPLAT = 0xFD0F,
        I16X8_SPLAT = 0xFD10,
        I32X4_SPLAT = 0xFD11,
        I64X2_SPLAT = 0xFD12,
        F32X4_SPLAT = 0xFD13,
        F64X2_SPLAT = 0xFD14,

        I8X16_EQ = 0xFD23,
        I8X16_NE = 0xFD24,
        I8X16_LT_S = 0xFD25,
        I8X16_LT_U = 0xFD26,
        I8X16_GT_S = 0xFD27,
        I8X16_GT_U = 0xFD28,
        I8X16_LE_S = 0xFD29,
        I8X16_LE_U = 0xFD2A,
        I8X16_GE_S = 0xFD2B,
        I8X16_GE_U = 0xFD2C,

        I16X8_EQ = 0xFD2D,
        I16X8_NE = 0xFD2E,
        I16X8_LT_S = 0xFD2F,
        I16X8_LT_U = 0xFD30,
        I16X8_GT_S = 0xFD31,
        I16X8_GT_U = 0xFD32,
        I16X8_LE_S = 0xFD33,
        I16X8_LE_U = 0xFD34,
        I16X8_GE_S = 0xFD35,
        I16X8_GE_U = 0xFD36,

        I32X4_EQ = 0xFD37,
        I32X4_NE = 0xFD38,
        I32X4_LT_S = 0xFD39,
        I32X4_LT_U = 0xFD3A,
        I32X4_GT_S = 0xFD3B,
        I32X4_GT_U = 0xFD3C,
        I32X4_LE_S = 0xFD3D,
        I32X4_LE_U = 0xFD3E,
        I32X4_GE_S = 0xFD3F,
        I32X4_GE_U = 0xFD40,

        I64X2_EQ = 0xFDD6,
        I64X2_NE = 0xFDD7,
        I64X2_LT_S = 0xFDD8,
        I64X2_GT_S = 0xFDD9,
        I64X2_LE_S = 0xFDDA,
        I64X2_GE_S = 0xFDDB,

        F32X4_EQ = 0xFD41,
        F32X4_NE = 0xFD42,
        F32X4_LT = 0xFD43,
        F32X4_GT = 0xFD44,
        F32X4_LE = 0xFD45,
        F32X4_GE = 0xFD46,

        F64X2_EQ = 0xFD47,
        F64X2_NE = 0xFD48,
        F64X2_LT = 0xFD49,
        F64X2_GT = 0xFD4A,
        F64X2_LE = 0xFD4B,
        F64X2_GE = 0xFD4C,

        V128_NOT = 0xFD4D,
        V128_AND = 0xFD4E,
        V128_ANDNOT = 0xFD4F,
        V128_OR = 0xFD50,
        V128_XOR = 0xFD51,
        V128_BITSELECT = 0xFD52,
        V128_ANY_TRUE = 0xFD53,

        I8X16_ABS = 0xFD60,
        I8X16_NEG = 0xFD61,
        I8X16_POPCNT = 0xFD62,
        I8X16_ALL_TRUE = 0xFD63,
        I8X16_BITMASK = 0xFD64,
        I8X16_NARROW_I16X8_S = 0xFD65,
        I8X16_NARROW_I16X8_U = 0xFD66,
        I8X16_SHL = 0xFD6B,
        I8X16_SHR_S = 0xFD6C,
        I8X16_SHR_U = 0xFD6D,
        I8X16_ADD = 0xFD6E,
        I8X16_ADD_SAT_S = 0xFD6F,
        I8X16_ADD_SAT_U = 0xFD70,
        I8X16_SUB = 0xFD71,
        I8X16_SUB_SAT_S = 0xFD72,
        I8X16_SUB_SAT_U = 0xFD73,
        I8X16_MIN_S = 0xFD76,
        I8X16_MIN_U = 0xFD77,
        I8X16_MAX_S = 0xFD78,
        I8X16_MAX_U = 0xFD79,
        I8X16_AVGR_U = 0xFD7B,
    }

    // When decoding an instruction, this tells us how to decode its operands.
    // Note that a vector of items always starts with 1 LEB128-encoded unsigned
    // 32-bit int which tells us the number of the items that follow.
    internal enum InstructionOperandType
    {
        BYTE,  // 1 byte.
        BYTE8,  // A little-endian unsigned 64-bit int in the next 8 bytes.
        F32,  // 1 little-endian IEEE 754 32-bit float.
        F64,  // 1 little-endian IEEE 754 64-bit double.
        I32,  // 1 LEB128-encoded signed 32-bit int.
        I64,  // 1 LEB128-encoded signed 64-bit int.
        U32,  // 1 LEB128-encoded unsigned 32-bit int.
        BLOCK,  // 1 block of instructions.
        SWITCH,  // A vector of LEB128-encoded unsigned 32-bit int, followed by a LEB128-encoded unsigned 32-bit int.
        U32X2,   // 2 LEB128-encoded unsigned 32-bit ints.
        MEMARG,  // 2 LEB128-encoded unsigned 32-bit ints.
        MEMARG_LANE,  // 3 LEB128-encoded unsigned 32-bit ints.
        LANE,  // 1 LEB128-encoded unsigned 32-bit int.
        LANE8,  // 16 LEB128-encoded unsigned 32-bit ints.
        VALTYPE_VECTOR,  // A vector of bytes.
    }

    // A fully decoded and flattened instruction.
    public struct Instruction
    {
        public InstructionType Type;
        public Value[] Operands;

        public Instruction(InstructionType type, Value[] operands)
        {
            Type = type;
            Operands = operands;
        }
    }

    // An operand for an UnflattenedInstruction. During the flatten operation, UnflattenedOperands
    // in UnflattenedInstructions will be replaced by Value operands in Instructions.
    public class UnflattenedOperand
    {
        public Value value;

        public UnflattenedOperand(Value value) { this.value = value; }
    }

    // An operand for an UnflattenedInstruction that also holds lists of UnflattenedInstructions.
    // During the flatten operation, these nested blocks get flattened so that we don't need the
    // lists anymore. Afterward, the block instruction's operands only contain jump targets.
    public class UnflattenedBlockOperand : UnflattenedOperand
    {
        // Used for BLOCK, LOOP, IF-END, and the positive branch of IF instructions.
        public List<UnflattenedInstruction> instructions;
        // Used only for the negative branch of IF instructions.
        public List<UnflattenedInstruction> else_instructions;

        public UnflattenedBlockOperand(Value value, List<UnflattenedInstruction> instructions, List<UnflattenedInstruction> else_instructions) : base(value)
        {
            this.instructions = instructions;
            this.else_instructions = else_instructions;
        }

        public static UnflattenedBlockOperand Decode(BinaryReader stream)
        {
            // The secret here is that if the first byte is >= 0x40, then the signed
            // LEB128 decode is a negative number -- specifically, a 7-bit signed negative
            // number. This means that any number between 0x40 and 0x7F can indicate
            // something other than an type index, namely, either a void block or a block with
            // a ValueType return type.
            ulong value_hi;

            long encoded_block_type = stream.ReadLEB128Signed();
            if (encoded_block_type < 0)
            {
                encoded_block_type += 0x80;
                if (encoded_block_type == 0x40)
                {
                    value_hi = (ulong)BlockType.VOID_BLOCK;
                }
                else
                {
                    value_hi = (ulong)BlockType.RETURNING_BLOCK;
                    value_hi |= (ulong)(encoded_block_type & 0xFF) << 2;
                }
            }
            else
            {
                value_hi = (ulong)BlockType.TYPED_BLOCK;
                value_hi |= (ulong)encoded_block_type << 2;
            }

            List<UnflattenedInstruction> instructions = new List<UnflattenedInstruction>();
            List<UnflattenedInstruction> else_instructions = new List<UnflattenedInstruction>();

            while (true)
            {
                UnflattenedInstruction insn = UnflattenedInstruction.Decode(stream);
                instructions.Add(insn);
                if (insn.Type == InstructionType.END) break;
                if (insn.Type == InstructionType.ELSE)
                {
                    while (true)
                    {
                        insn = UnflattenedInstruction.Decode(stream);
                        else_instructions.Add(insn);
                        if (insn.Type == InstructionType.END) break;
                    }
                    break;
                }
            }

            // value_lo will be filled in during the flatten operation, when we will figure
            // out program counters.
            return new UnflattenedBlockOperand(new Value(value_hi, 0UL), instructions, else_instructions);
        }
    }

    // A fully decoded but unflattened instruction. That means its operands can contain not only Values
    // but also lists of (unflattened) instructions.
    public struct UnflattenedInstruction
    {
        public InstructionType Type;
        public UnflattenedOperand[] Operands;

        public UnflattenedInstruction(InstructionType type, UnflattenedOperand[] operands)
        {
            Type = type;
            Operands = operands;
        }

        public static UnflattenedInstruction Decode(BinaryReader stream)
        {
            uint opcode = stream.ReadByte();

            // I couldn't find it explicitly stated, but at the very least, opcodes
            // 0xFC and 0xFD are "extension" opcodes, where you have to read the next
            // LEB128-encoded unsigned int to get the instruction.

            if (opcode == 0xFC || opcode == 0xFD)
            {
                uint ext_opcode = (uint)stream.ReadLEB128Unsigned();
                // We're just going to assume that there aren't any extended opcodes past
                // 0xFF.
                if (ext_opcode > 0xFF)
                    throw new InvalidOperationException(
                        $"Unknown extended opcode {opcode:02X} + {ext_opcode:02X}"
                    );
                opcode = (opcode << 8) | ext_opcode;
            }

            InstructionType type = (InstructionType)opcode;
            UnflattenedOperand[] operands = { };

            if (!Map.TryGetValue(type, out InstructionOperandType operandType))
                return new UnflattenedInstruction(type, operands);

            switch (operandType)
            {
                case InstructionOperandType.BYTE:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value((uint)stream.ReadByte())) };
                    break;

                case InstructionOperandType.BYTE8:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value(stream.ReadUInt64())) };
                    break;

                case InstructionOperandType.U32:  // fallthrough
                case InstructionOperandType.LANE:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())) };
                    break;

                case InstructionOperandType.U32X2:  // fallthrough
                case InstructionOperandType.MEMARG:
                    operands = new UnflattenedOperand[] {
                        new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())),
                        new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())) };
                    break;

                case InstructionOperandType.MEMARG_LANE:
                    operands = new UnflattenedOperand[] {
                        new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())),
                        new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())),
                        new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())) };
                    break;

                case InstructionOperandType.LANE8:
                    operands = new UnflattenedOperand[16];
                    for (int i = 0; i < 16; i++)
                        operands[i] = new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned()));
                    break;

                case InstructionOperandType.VALTYPE_VECTOR:
                    operands = new UnflattenedOperand[(uint)stream.ReadLEB128Unsigned()];
                    for (uint i = 0; i < operands.Length; i++)
                        operands[i] = new UnflattenedOperand(new Value((uint)stream.ReadByte()));
                    break;

                case InstructionOperandType.I32:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned())) };
                    break;

                case InstructionOperandType.I64:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value(stream.ReadLEB128Unsigned())) };
                    break;

                case InstructionOperandType.F32:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value(stream.ReadSingle())) };
                    break;

                case InstructionOperandType.F64:
                    operands = new UnflattenedOperand[] { new UnflattenedOperand(new Value(stream.ReadDouble())) };
                    break;

                case InstructionOperandType.SWITCH:
                    uint tableSize = (uint)stream.ReadLEB128Unsigned();
                    operands = new UnflattenedOperand[tableSize + 1];
                    for (uint i = 0; i < operands.Length; i++)
                        operands[i] = new UnflattenedOperand(new Value((uint)stream.ReadLEB128Unsigned()));
                    break;

                case InstructionOperandType.BLOCK:
                    operands = new UnflattenedOperand[] { UnflattenedBlockOperand.Decode(stream) };
                    break;
            }
            return new UnflattenedInstruction(type, operands);
        }

        // Instructions not in the map have no operands.
        internal static IReadOnlyDictionary<InstructionType, InstructionOperandType> Map = new Dictionary<InstructionType, InstructionOperandType>()
        {
            { InstructionType.REF_NULL, InstructionOperandType.BYTE },
            { InstructionType.V128_CONST, InstructionOperandType.BYTE8 },
            { InstructionType.REF_FUNC, InstructionOperandType.U32 },
            { InstructionType.LOCAL_GET, InstructionOperandType.U32 },
            { InstructionType.LOCAL_SET, InstructionOperandType.U32 },
            { InstructionType.LOCAL_TEE, InstructionOperandType.U32 },
            { InstructionType.GLOBAL_GET, InstructionOperandType.U32 },
            { InstructionType.GLOBAL_SET, InstructionOperandType.U32 },
            { InstructionType.TABLE_GET, InstructionOperandType.U32 },
            { InstructionType.ELEM_DROP, InstructionOperandType.U32 },
            { InstructionType.TABLE_GROW, InstructionOperandType.U32 },
            { InstructionType.TABLE_SIZE, InstructionOperandType.U32 },
            { InstructionType.TABLE_FILL, InstructionOperandType.U32 },
            { InstructionType.DATA_DROP, InstructionOperandType.U32 },
            { InstructionType.MEMORY_SIZE, InstructionOperandType.U32 },
            { InstructionType.MEMORY_GROW, InstructionOperandType.U32 },
            { InstructionType.MEMORY_FILL, InstructionOperandType.U32 },
            { InstructionType.BR, InstructionOperandType.U32 },
            { InstructionType.BR_IF, InstructionOperandType.U32 },
            { InstructionType.CALL, InstructionOperandType.U32 },
            { InstructionType.TABLE_INIT, InstructionOperandType.U32X2 },
            { InstructionType.TABLE_COPY, InstructionOperandType.U32X2 },
            { InstructionType.MEMORY_INIT, InstructionOperandType.U32X2 },
            { InstructionType.MEMORY_COPY, InstructionOperandType.U32X2 },
            { InstructionType.CALL_INDIRECT, InstructionOperandType.U32X2 },
            { InstructionType.I32_LOAD, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD, InstructionOperandType.MEMARG },
            { InstructionType.F32_LOAD, InstructionOperandType.MEMARG },
            { InstructionType.F64_LOAD, InstructionOperandType.MEMARG },
            { InstructionType.I32_LOAD8_S, InstructionOperandType.MEMARG },
            { InstructionType.I32_LOAD8_U, InstructionOperandType.MEMARG },
            { InstructionType.I32_LOAD16_S, InstructionOperandType.MEMARG },
            { InstructionType.I32_LOAD16_U, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD8_S, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD8_U, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD16_S, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD16_U, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD32_S, InstructionOperandType.MEMARG },
            { InstructionType.I64_LOAD32_U, InstructionOperandType.MEMARG },
            { InstructionType.I32_STORE, InstructionOperandType.MEMARG },
            { InstructionType.I64_STORE, InstructionOperandType.MEMARG },
            { InstructionType.F32_STORE, InstructionOperandType.MEMARG },
            { InstructionType.F64_STORE, InstructionOperandType.MEMARG },
            { InstructionType.I32_STORE8, InstructionOperandType.MEMARG },
            { InstructionType.I32_STORE16, InstructionOperandType.MEMARG },
            { InstructionType.I64_STORE8, InstructionOperandType.MEMARG },
            { InstructionType.I64_STORE16, InstructionOperandType.MEMARG },
            { InstructionType.I64_STORE32, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD8X8_S, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD8X8_U, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD16X4_S, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD16X4_U, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD32X2_S, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD32X2_U, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD8_SPLAT, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD16_SPLAT, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD32_SPLAT, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD64_SPLAT, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD32_ZERO, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD64_ZERO, InstructionOperandType.MEMARG },
            { InstructionType.V128_LOAD8_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_LOAD16_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_LOAD32_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_LOAD64_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_STORE8_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_STORE16_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_STORE32_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.V128_STORE64_LANE, InstructionOperandType.MEMARG_LANE },
            { InstructionType.I8X16_EXTRACT_LANE_S, InstructionOperandType.LANE },
            { InstructionType.I8X16_EXTRACT_LANE_U, InstructionOperandType.LANE },
            { InstructionType.I8X16_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.I16X8_EXTRACT_LANE_S, InstructionOperandType.LANE },
            { InstructionType.I16X8_EXTRACT_LANE_U, InstructionOperandType.LANE },
            { InstructionType.I16X8_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.I32X4_EXTRACT_LANE, InstructionOperandType.LANE },
            { InstructionType.I32X4_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.I64X2_EXTRACT_LANE, InstructionOperandType.LANE },
            { InstructionType.I64X2_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.F32X4_EXTRACT_LANE, InstructionOperandType.LANE },
            { InstructionType.F32X4_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.F64X2_EXTRACT_LANE, InstructionOperandType.LANE },
            { InstructionType.F64X2_REPLACE_LANE, InstructionOperandType.LANE },
            { InstructionType.I8X16_SHUFFLE, InstructionOperandType.LANE8 },
            { InstructionType.SELECT_VEC, InstructionOperandType.VALTYPE_VECTOR },
            { InstructionType.I32_CONST, InstructionOperandType.I32 },
            { InstructionType.I64_CONST, InstructionOperandType.I64 },
            { InstructionType.F32_CONST, InstructionOperandType.F32 },
            { InstructionType.F64_CONST, InstructionOperandType.F64 },
            { InstructionType.BLOCK, InstructionOperandType.BLOCK },
            { InstructionType.LOOP, InstructionOperandType.BLOCK },
            { InstructionType.IF, InstructionOperandType.BLOCK },
            { InstructionType.BR_TABLE, InstructionOperandType.SWITCH },
        };
    }

    // Flattens a list of UnflattenedInstructions. This also resolves instruction locations in terms
    // of program counters, which allows us to populate block targets.
    public static class Flattener
    {
        public static List<Instruction> flatten(this List<UnflattenedInstruction> instructions, uint pc)
        {
            List<Instruction> flattened_instructions = new List<Instruction>();
            List<Instruction> block_insns;
            UnflattenedBlockOperand block_operand;

            foreach (UnflattenedInstruction instruction in instructions)
            {
                Instruction initial_instruction = new Instruction(instruction.Type,
                    (from operand in instruction.Operands select operand.value).ToArray());
                switch (instruction.Type)
                {
                    case InstructionType.BLOCK:
                        block_insns = new List<Instruction> { initial_instruction };
                        block_operand = (UnflattenedBlockOperand)instruction.Operands[0];
                        block_insns.AddRange(block_operand.instructions.flatten(pc + 1));

                        pc += (uint)block_insns.Count;
                        initial_instruction.Operands[0].value_lo = pc;

                        flattened_instructions.AddRange(block_insns);
                        break;

                    case InstructionType.LOOP:
                        block_insns = new List<Instruction> { initial_instruction };
                        block_operand = (UnflattenedBlockOperand)instruction.Operands[0];
                        block_insns.AddRange(block_operand.instructions.flatten(pc + 1));

                        initial_instruction.Operands[0].value_lo = pc;
                        pc += (uint)block_insns.Count;

                        flattened_instructions.AddRange(block_insns);
                        break;

                    case InstructionType.IF:
                        block_insns = new List<Instruction> { initial_instruction };
                        block_operand = (UnflattenedBlockOperand)instruction.Operands[0];
                        block_insns.AddRange(block_operand.instructions.flatten(pc + 1));

                        // This first block ends in either an END or an ELSE.

                        pc += (uint)block_insns.Count + 1;
                        initial_instruction.Operands[0].value_lo = (ulong)pc << 32;  // Will be either END+1 or ELSE+1.

                        List<Instruction> false_insns = block_operand.else_instructions.flatten(pc);
                        pc += (uint)false_insns.Count;

                        initial_instruction.Operands[0].value_lo |= pc;  // The end of the IF-[ELSE-]END block.

                        // Note that if there was no ELSE, then both targets will be equal.

                        flattened_instructions.AddRange(block_insns);
                        flattened_instructions.AddRange(false_insns);
                        break;

                    default:
                        flattened_instructions.Add(initial_instruction);
                        pc += 1;
                        break;
                }
            }
            return flattened_instructions;
        }
    }
}  // namespace Derg
