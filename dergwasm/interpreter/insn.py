"""Instruction definitions."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import dataclasses
import enum
from io import BufferedIOBase
import struct
from typing import Union

import leb128  # type: ignore

from dergwasm.interpreter import values


@enum.unique
class InstructionType(enum.Enum):
    """Instruction types."""

    # Control instructions
    UNREACHABLE = 0x00
    NOP = 0x01
    BLOCK = 0x02
    LOOP = 0x03
    IF = 0x04
    # Not really an instruction, but it's used to end an if-block and start an else-block.
    ELSE = 0x05
    # Not really an instruction, but it's used to end blocks.
    END = 0x0B
    BR = 0x0C
    BR_IF = 0x0D
    BR_TABLE = 0x0E
    RETURN = 0x0F
    CALL = 0x10
    CALL_INDIRECT = 0x11

    # Reference instructions
    REF_NULL = 0xD0
    REF_IS_NULL = 0xD1
    REF_FUNC = 0xD2

    # Parametric instructions
    DROP = 0x1A
    SELECT = 0x1B
    SELECT_VEC = 0x1C

    # Variable instructions
    LOCAL_GET = 0x20
    LOCAL_SET = 0x21
    LOCAL_TEE = 0x22
    GLOBAL_GET = 0x23
    GLOBAL_SET = 0x24

    # Table instructions
    TABLE_GET = 0x25
    TABLE_SET = 0x26
    TABLE_INIT = 0xFC0C
    ELEM_DROP = 0xFC0D
    TABLE_COPY = 0xFC0E
    TABLE_GROW = 0xFC0F
    TABLE_SIZE = 0xFC10
    TABLE_FILL = 0xFC11

    # Memory instructions
    I32_LOAD = 0x28
    I64_LOAD = 0x29
    F32_LOAD = 0x2A
    F64_LOAD = 0x2B
    I32_LOAD8_S = 0x2C
    I32_LOAD8_U = 0x2D
    I32_LOAD16_S = 0x2E
    I32_LOAD16_U = 0x2F
    I64_LOAD8_S = 0x30
    I64_LOAD8_U = 0x31
    I64_LOAD16_S = 0x32
    I64_LOAD16_U = 0x33
    I64_LOAD32_S = 0x34
    I64_LOAD32_U = 0x35
    I32_STORE = 0x36
    I64_STORE = 0x37
    F32_STORE = 0x38
    F64_STORE = 0x39
    I32_STORE8 = 0x3A
    I32_STORE16 = 0x3B
    I64_STORE8 = 0x3C
    I64_STORE16 = 0x3D
    I64_STORE32 = 0x3E
    MEMORY_SIZE = 0x3F
    MEMORY_GROW = 0x40
    MEMORY_INIT = 0xFC08
    DATA_DROP = 0xFC09
    MEMORY_COPY = 0xFC0A
    MEMORY_FILL = 0xFC0B

    # Numeric instructions
    I32_CONST = 0x41
    I64_CONST = 0x42
    F32_CONST = 0x43
    F64_CONST = 0x44

    I32_EQZ = 0x45
    I32_EQ = 0x46
    I32_NE = 0x47
    I32_LT_S = 0x48
    I32_LT_U = 0x49
    I32_GT_S = 0x4A
    I32_GT_U = 0x4B
    I32_LE_S = 0x4C
    I32_LE_U = 0x4D
    I32_GE_S = 0x4E
    I32_GE_U = 0x4F

    I64_EQZ = 0x50
    I64_EQ = 0x51
    I64_NE = 0x52
    I64_LT_S = 0x53
    I64_LT_U = 0x54
    I64_GT_S = 0x55
    I64_GT_U = 0x56
    I64_LE_S = 0x57
    I64_LE_U = 0x58
    I64_GE_S = 0x59
    I64_GE_U = 0x5A
    F32_EQ = 0x5B
    F32_NE = 0x5C
    F32_LT = 0x5D
    F32_GT = 0x5E
    F32_LE = 0x5F
    F32_GE = 0x60

    F64_EQ = 0x61
    F64_NE = 0x62
    F64_LT = 0x63
    F64_GT = 0x64
    F64_LE = 0x65
    F64_GE = 0x66

    I32_CLZ = 0x67
    I32_CTZ = 0x68
    I32_POPCNT = 0x69
    I32_ADD = 0x6A
    I32_SUB = 0x6B
    I32_MUL = 0x6C
    I32_DIV_S = 0x6D
    I32_DIV_U = 0x6E
    I32_REM_S = 0x6F
    I32_REM_U = 0x70
    I32_AND = 0x71
    I32_OR = 0x72
    I32_XOR = 0x73
    I32_SHL = 0x74
    I32_SHR_S = 0x75
    I32_SHR_U = 0x76
    I32_ROTL = 0x77
    I32_ROTR = 0x78

    I64_CLZ = 0x79
    I64_CTZ = 0x7A
    I64_POPCNT = 0x7B
    I64_ADD = 0x7C
    I64_SUB = 0x7D
    I64_MUL = 0x7E
    I64_DIV_S = 0x7F
    I64_DIV_U = 0x80
    I64_REM_S = 0x81
    I64_REM_U = 0x82
    I64_AND = 0x83
    I64_OR = 0x84
    I64_XOR = 0x85
    I64_SHL = 0x86
    I64_SHR_S = 0x87
    I64_SHR_U = 0x88
    I64_ROTL = 0x89
    I64_ROTR = 0x8A

    F32_ABS = 0x8B
    F32_NEG = 0x8C
    F32_CEIL = 0x8D
    F32_FLOOR = 0x8E
    F32_TRUNC = 0x8F
    F32_NEAREST = 0x90
    F32_SQRT = 0x91
    F32_ADD = 0x92
    F32_SUB = 0x93
    F32_MUL = 0x94
    F32_DIV = 0x95
    F32_MIN = 0x96
    F32_MAX = 0x97
    F32_COPYSIGN = 0x98

    F64_ABS = 0x99
    F64_NEG = 0x9A
    F64_CEIL = 0x9B
    F64_FLOOR = 0x9C
    F64_TRUNC = 0x9D
    F64_NEAREST = 0x9E
    F64_SQRT = 0x9F
    F64_ADD = 0xA0
    F64_SUB = 0xA1
    F64_MUL = 0xA2
    F64_DIV = 0xA3
    F64_MIN = 0xA4
    F64_MAX = 0xA5
    F64_COPYSIGN = 0xA6

    I32_WRAP_I64 = 0xA7
    I32_TRUNC_F32_S = 0xA8
    I32_TRUNC_F32_U = 0xA9
    I32_TRUNC_F64_S = 0xAA
    I32_TRUNC_F64_U = 0xAB
    I64_EXTEND_I32_S = 0xAC
    I64_EXTEND_I32_U = 0xAD
    I64_TRUNC_F32_S = 0xAE
    I64_TRUNC_F32_U = 0xAF
    I64_TRUNC_F64_S = 0xB0
    I64_TRUNC_F64_U = 0xB1
    F32_CONVERT_I32_S = 0xB2
    F32_CONVERT_I32_U = 0xB3
    F32_CONVERT_I64_S = 0xB4
    F32_CONVERT_I64_U = 0xB5
    F32_DEMOTE_F64 = 0xB6
    F64_CONVERT_I32_S = 0xB7
    F64_CONVERT_I32_U = 0xB8
    F64_CONVERT_I64_S = 0xB9
    F64_CONVERT_I64_U = 0xBA
    F32_F64_PROMOTE = 0xBB
    F32_I32_REINTERPRET = 0xBC
    F64_I64_REINTERPRET = 0xBD
    F32_REINTERPRET_I32 = 0xBE
    F64_REINTERPRET_I64 = 0xBF

    I32_EXTEND8_S = 0xC0
    I32_EXTEND16_S = 0xC1
    I64_EXTEND8_S = 0xC2
    I64_EXTEND16_S = 0xC3
    I64_EXTEND32_S = 0xC4

    I32_TRUNC_SAT_F32_S = 0xFC00
    I32_TRUNC_SAT_F32_U = 0xFC01
    I32_TRUNC_SAT_F64_S = 0xFC02
    I32_TRUNC_SAT_F64_U = 0xFC03
    I64_TRUNC_SAT_F32_S = 0xFC04
    I64_TRUNC_SAT_F32_U = 0xFC05
    I64_TRUNC_SAT_F64_S = 0xFC06
    I64_TRUNC_SAT_F64_U = 0xFC07

    # Vector instructions
    V128_LOAD = 0xFD00
    V128_LOAD8X8_S = 0xFD01
    V128_LOAD8X8_U = 0xFD02
    V128_LOAD16X4_S = 0xFD03
    V128_LOAD16X4_U = 0xFD04
    V128_LOAD32X2_S = 0xFD05
    V128_LOAD32X2_U = 0xFD06
    V128_LOAD8_SPLAT = 0xFD07
    V128_LOAD16_SPLAT = 0xFD08
    V128_LOAD32_SPLAT = 0xFD09
    V128_LOAD64_SPLAT = 0xFD0A
    V128_LOAD32_ZERO = 0xFD5C
    V128_LOAD64_ZERO = 0xFD5D
    V128_STORE = 0xFD0B
    V128_LOAD8_LANE = 0xFD54
    V128_LOAD16_LANE = 0xFD55
    V128_LOAD32_LANE = 0xFD56
    V128_LOAD64_LANE = 0xFD57
    V128_STORE8_LANE = 0xFD58
    V128_STORE16_LANE = 0xFD59
    V128_STORE32_LANE = 0xFD5A
    V128_STORE64_LANE = 0xFD5B

    V128_CONST = 0xFD0C
    I8X16_SHUFFLE = 0xFD0D

    I8X16_EXTRACT_LANE_S = 0xFD15
    I8X16_EXTRACT_LANE_U = 0xFD16
    I8X16_REPLACE_LANE = 0xFD17
    I16X8_EXTRACT_LANE_S = 0xFD18
    I16X8_EXTRACT_LANE_U = 0xFD19
    I16X8_REPLACE_LANE = 0xFD1A
    I32X4_EXTRACT_LANE = 0xFD1B
    I32X4_REPLACE_LANE = 0xFD1C
    I64X2_EXTRACT_LANE = 0xFD1D
    I64X2_REPLACE_LANE = 0xFD1E
    F32X4_EXTRACT_LANE = 0xFD1F
    F32X4_REPLACE_LANE = 0xFD20
    F64X2_EXTRACT_LANE = 0xFD21
    F64X2_REPLACE_LANE = 0xFD22

    I8X16_SWIZZLE = 0xFD0E
    I8X16_SPLAT = 0xFD0F
    I16X8_SPLAT = 0xFD10
    I32X4_SPLAT = 0xFD11
    I64X2_SPLAT = 0xFD12
    F32X4_SPLAT = 0xFD13
    F64X2_SPLAT = 0xFD14

    I8X16_EQ = 0xFD23
    I8X16_NE = 0xFD24
    I8X16_LT_S = 0xFD25
    I8X16_LT_U = 0xFD26
    I8X16_GT_S = 0xFD27
    I8X16_GT_U = 0xFD28
    I8X16_LE_S = 0xFD29
    I8X16_LE_U = 0xFD2A
    I8X16_GE_S = 0xFD2B
    I8X16_GE_U = 0xFD2C

    I16X8_EQ = 0xFD2D
    I16X8_NE = 0xFD2E
    I16X8_LT_S = 0xFD2F
    I16X8_LT_U = 0xFD30
    I16X8_GT_S = 0xFD31
    I16X8_GT_U = 0xFD32
    I16X8_LE_S = 0xFD33
    I16X8_LE_U = 0xFD34
    I16X8_GE_S = 0xFD35
    I16X8_GE_U = 0xFD36

    I32X4_EQ = 0xFD37
    I32X4_NE = 0xFD38
    I32X4_LT_S = 0xFD39
    I32X4_LT_U = 0xFD3A
    I32X4_GT_S = 0xFD3B
    I32X4_GT_U = 0xFD3C
    I32X4_LE_S = 0xFD3D
    I32X4_LE_U = 0xFD3E
    I32X4_GE_S = 0xFD3F
    I32X4_GE_U = 0xFD40

    I64X2_EQ = 0xFDD6
    I64X2_NE = 0xFDD7
    I64X2_LT_S = 0xFDD8
    I64X2_GT_S = 0xFDD9
    I64X2_LE_S = 0xFDDA
    I64X2_GE_S = 0xFDDB

    F32X4_EQ = 0xFD41
    F32X4_NE = 0xFD42
    F32X4_LT = 0xFD43
    F32X4_GT = 0xFD44
    F32X4_LE = 0xFD45
    F32X4_GE = 0xFD46

    F64X2_EQ = 0xFD47
    F64X2_NE = 0xFD48
    F64X2_LT = 0xFD49
    F64X2_GT = 0xFD4A
    F64X2_LE = 0xFD4B
    F64X2_GE = 0xFD4C

    V128_NOT = 0xFD4D
    V128_AND = 0xFD4E
    V128_ANDNOT = 0xFD4F
    V128_OR = 0xFD50
    V128_XOR = 0xFD51
    V128_BITSELECT = 0xFD52
    V128_ANY_TRUE = 0xFD53

    I8X16_ABS = 0xFD60
    I8X16_NEG = 0xFD61
    I8X16_POPCNT = 0xFD62
    I8X16_ALL_TRUE = 0xFD63
    I8X16_BITMASK = 0xFD64
    I8X16_NARROW_I16X8_S = 0xFD65
    I8X16_NARROW_I16X8_U = 0xFD66
    I8X16_SHL = 0xFD6B
    I8X16_SHR_S = 0xFD6C
    I8X16_SHR_U = 0xFD6D
    I8X16_ADD = 0xFD6E
    I8X16_ADD_SAT_S = 0xFD6F
    I8X16_ADD_SAT_U = 0xFD70
    I8X16_SUB = 0xFD71
    I8X16_SUB_SAT_S = 0xFD72
    I8X16_SUB_SAT_U = 0xFD73
    I8X16_MIN_S = 0xFD76
    I8X16_MIN_U = 0xFD77
    I8X16_MAX_S = 0xFD78
    I8X16_MAX_U = 0xFD79
    I8X16_AVGR_U = 0xFD7B


BYTE_INSNS = [InstructionType.REF_NULL]
BYTE8_INSNS = [InstructionType.V128_CONST]
U32_INSNS = [
    InstructionType.REF_FUNC,
    InstructionType.LOCAL_GET,
    InstructionType.LOCAL_SET,
    InstructionType.LOCAL_TEE,
    InstructionType.GLOBAL_GET,
    InstructionType.GLOBAL_SET,
    InstructionType.TABLE_GET,
    InstructionType.ELEM_DROP,
    InstructionType.TABLE_GROW,
    InstructionType.TABLE_SIZE,
    InstructionType.TABLE_FILL,
    InstructionType.DATA_DROP,
    InstructionType.MEMORY_SIZE,
    InstructionType.MEMORY_GROW,
    InstructionType.MEMORY_FILL,
    InstructionType.BR,
    InstructionType.BR_IF,
    InstructionType.CALL,
]
U32X2_INSNS = [
    InstructionType.TABLE_INIT,
    InstructionType.TABLE_COPY,
    InstructionType.MEMORY_INIT,
    InstructionType.MEMORY_COPY,
    InstructionType.CALL_INDIRECT,
]
MEMARG_INSNS = [
    InstructionType.I32_LOAD,
    InstructionType.I64_LOAD,
    InstructionType.F32_LOAD,
    InstructionType.F64_LOAD,
    InstructionType.I32_LOAD8_S,
    InstructionType.I32_LOAD8_U,
    InstructionType.I32_LOAD16_S,
    InstructionType.I32_LOAD16_U,
    InstructionType.I64_LOAD8_S,
    InstructionType.I64_LOAD8_U,
    InstructionType.I64_LOAD16_S,
    InstructionType.I64_LOAD16_U,
    InstructionType.I64_LOAD32_S,
    InstructionType.I64_LOAD32_U,
    InstructionType.I32_STORE,
    InstructionType.I64_STORE,
    InstructionType.F32_STORE,
    InstructionType.F64_STORE,
    InstructionType.I32_STORE8,
    InstructionType.I32_STORE16,
    InstructionType.I64_STORE8,
    InstructionType.I64_STORE16,
    InstructionType.I64_STORE32,
    InstructionType.V128_LOAD,
    InstructionType.V128_LOAD8X8_S,
    InstructionType.V128_LOAD8X8_U,
    InstructionType.V128_LOAD16X4_S,
    InstructionType.V128_LOAD16X4_U,
    InstructionType.V128_LOAD32X2_S,
    InstructionType.V128_LOAD32X2_U,
    InstructionType.V128_LOAD8_SPLAT,
    InstructionType.V128_LOAD16_SPLAT,
    InstructionType.V128_LOAD32_SPLAT,
    InstructionType.V128_LOAD64_SPLAT,
    InstructionType.V128_LOAD32_ZERO,
    InstructionType.V128_LOAD64_ZERO,
]
MEMARG_LANE_INSNS = [
    InstructionType.V128_LOAD8_LANE,
    InstructionType.V128_LOAD16_LANE,
    InstructionType.V128_LOAD32_LANE,
    InstructionType.V128_LOAD64_LANE,
    InstructionType.V128_STORE8_LANE,
    InstructionType.V128_STORE16_LANE,
    InstructionType.V128_STORE32_LANE,
    InstructionType.V128_STORE64_LANE,
]
LANE_INSNS = [
    InstructionType.I8X16_EXTRACT_LANE_S,
    InstructionType.I8X16_EXTRACT_LANE_U,
    InstructionType.I8X16_REPLACE_LANE,
    InstructionType.I16X8_EXTRACT_LANE_S,
    InstructionType.I16X8_EXTRACT_LANE_U,
    InstructionType.I16X8_REPLACE_LANE,
    InstructionType.I32X4_EXTRACT_LANE,
    InstructionType.I32X4_REPLACE_LANE,
    InstructionType.I64X2_EXTRACT_LANE,
    InstructionType.I64X2_REPLACE_LANE,
    InstructionType.F32X4_EXTRACT_LANE,
    InstructionType.F32X4_REPLACE_LANE,
    InstructionType.F64X2_EXTRACT_LANE,
    InstructionType.F64X2_REPLACE_LANE,
]
LANE8_INSNS = [InstructionType.I8X16_SHUFFLE]
VALTYPE_VECTOR_INSNS = [InstructionType.SELECT_VEC]
I32_INSNS = [
    InstructionType.I32_CONST,
]
I64_INSNS = [
    InstructionType.I64_CONST,
]
F32_INSNS = [
    InstructionType.F32_CONST,
]
F64_INSNS = [
    InstructionType.F64_CONST,
]
BLOCK_INSNS = [
    InstructionType.BLOCK,
    InstructionType.LOOP,
    InstructionType.IF,
]
SWITCH_INSNS = [
    InstructionType.BR_TABLE,
]

INSTRUCTION_TYPE_STRINGS: dict[InstructionType, str] = {
    # Control instructions
    InstructionType.UNREACHABLE: "unreachable",
    InstructionType.NOP: "nop",
    InstructionType.BLOCK: "block",
    InstructionType.LOOP: "loop",
    InstructionType.IF: "if",
    # Not really an instruction, but it's used to end an if-block and start an else-block.,
    InstructionType.ELSE: "else",
    # Not really an instruction, but it's used to end blocks.,
    InstructionType.END: "end",
    InstructionType.BR: "br",
    InstructionType.BR_IF: "br_if",
    InstructionType.BR_TABLE: "br_table",
    InstructionType.RETURN: "return",
    InstructionType.CALL: "call",
    InstructionType.CALL_INDIRECT: "call_indirect",
    # Reference instructions,
    InstructionType.REF_NULL: "ref.null",
    InstructionType.REF_IS_NULL: "ref.is_null",
    InstructionType.REF_FUNC: "ref.func",
    # Parametric instructions,
    InstructionType.DROP: "drop",
    InstructionType.SELECT: "select",
    InstructionType.SELECT_VEC: "select.vec",
    # Variable instructions,
    InstructionType.LOCAL_GET: "local.get",
    InstructionType.LOCAL_SET: "local.set",
    InstructionType.LOCAL_TEE: "local.tee",
    InstructionType.GLOBAL_GET: "global.get",
    InstructionType.GLOBAL_SET: "global.set",
    # Table instructions,
    InstructionType.TABLE_GET: "table.get",
    InstructionType.TABLE_SET: "table.set",
    InstructionType.TABLE_INIT: "table.init",
    InstructionType.ELEM_DROP: "elem.drop",
    InstructionType.TABLE_COPY: "table.copy",
    InstructionType.TABLE_GROW: "table.grow",
    InstructionType.TABLE_SIZE: "table.size",
    InstructionType.TABLE_FILL: "table.fill",
    # Memory instructions,
    InstructionType.I32_LOAD: "i32.load",
    InstructionType.I64_LOAD: "i64.load",
    InstructionType.F32_LOAD: "f32.load",
    InstructionType.F64_LOAD: "f64.load",
    InstructionType.I32_LOAD8_S: "i32.load8_s",
    InstructionType.I32_LOAD8_U: "i32.load8_u",
    InstructionType.I32_LOAD16_S: "i32.load16_s",
    InstructionType.I32_LOAD16_U: "i32.load16_u",
    InstructionType.I64_LOAD8_S: "i64.load8_s",
    InstructionType.I64_LOAD8_U: "i64.load8_u",
    InstructionType.I64_LOAD16_S: "i64.load16_s",
    InstructionType.I64_LOAD16_U: "i64.load16_u",
    InstructionType.I64_LOAD32_S: "i64.load32_s",
    InstructionType.I64_LOAD32_U: "i64.load32_u",
    InstructionType.I32_STORE: "i32.store",
    InstructionType.I64_STORE: "i64.store",
    InstructionType.F32_STORE: "f32.store",
    InstructionType.F64_STORE: "f64.store",
    InstructionType.I32_STORE8: "i32.store8",
    InstructionType.I32_STORE16: "i32.store16",
    InstructionType.I64_STORE8: "i64.store8",
    InstructionType.I64_STORE16: "i64.store16",
    InstructionType.I64_STORE32: "i64.store32",
    InstructionType.MEMORY_SIZE: "memory.size",
    InstructionType.MEMORY_GROW: "memory.grow",
    InstructionType.MEMORY_INIT: "memory.init",
    InstructionType.DATA_DROP: "data.drop",
    InstructionType.MEMORY_COPY: "memory.copy",
    InstructionType.MEMORY_FILL: "memory.fill",
    # Numeric instructions,
    InstructionType.I32_CONST: "i32.const",
    InstructionType.I64_CONST: "i64.const",
    InstructionType.F32_CONST: "f32.const",
    InstructionType.F64_CONST: "f64.const",
    InstructionType.I32_EQZ: "i32.eqz",
    InstructionType.I32_EQ: "i32.eq",
    InstructionType.I32_NE: "i32.ne",
    InstructionType.I32_LT_S: "i32.lt_s",
    InstructionType.I32_LT_U: "i32.lt_u",
    InstructionType.I32_GT_S: "i32.gt_s",
    InstructionType.I32_GT_U: "i32.gt_u",
    InstructionType.I32_LE_S: "i32.le_s",
    InstructionType.I32_LE_U: "i32.le_u",
    InstructionType.I32_GE_S: "i32.ge_s",
    InstructionType.I32_GE_U: "i32.ge_u",
    InstructionType.I64_EQZ: "i64.eqz",
    InstructionType.I64_EQ: "i64.eq",
    InstructionType.I64_NE: "i64.ne",
    InstructionType.I64_LT_S: "i64.lt_s",
    InstructionType.I64_LT_U: "i64.lt_u",
    InstructionType.I64_GT_S: "i64.gt_s",
    InstructionType.I64_GT_U: "i64.gt_u",
    InstructionType.I64_LE_S: "i64.le_s",
    InstructionType.I64_LE_U: "i64.le_u",
    InstructionType.I64_GE_S: "i64.ge_s",
    InstructionType.I64_GE_U: "i64.ge_u",
    InstructionType.F32_EQ: "f32.eq",
    InstructionType.F32_NE: "f32.ne",
    InstructionType.F32_LT: "f32.lt",
    InstructionType.F32_GT: "f32.gt",
    InstructionType.F32_LE: "f32.le",
    InstructionType.F32_GE: "f32.ge",
    InstructionType.F64_EQ: "f64.eq",
    InstructionType.F64_NE: "f64.ne",
    InstructionType.F64_LT: "f64.lt",
    InstructionType.F64_GT: "f64.gt",
    InstructionType.F64_LE: "f64.le",
    InstructionType.F64_GE: "f64.ge",
    InstructionType.I32_CLZ: "i32.clz",
    InstructionType.I32_CTZ: "i32.ctz",
    InstructionType.I32_POPCNT: "i32.popcnt",
    InstructionType.I32_ADD: "i32.add",
    InstructionType.I32_SUB: "i32.sub",
    InstructionType.I32_MUL: "i32.mul",
    InstructionType.I32_DIV_S: "i32.div_s",
    InstructionType.I32_DIV_U: "i32.div_u",
    InstructionType.I32_REM_S: "i32.rem_s",
    InstructionType.I32_REM_U: "i32.rem_u",
    InstructionType.I32_AND: "i32.and",
    InstructionType.I32_OR: "i32.or",
    InstructionType.I32_XOR: "i32.xor",
    InstructionType.I32_SHL: "i32.shl",
    InstructionType.I32_SHR_S: "i32.shr_s",
    InstructionType.I32_SHR_U: "i32.shr_u",
    InstructionType.I32_ROTL: "i32.rotl",
    InstructionType.I32_ROTR: "i32.rotr",
    InstructionType.I64_CLZ: "i64.clz",
    InstructionType.I64_CTZ: "i64.ctz",
    InstructionType.I64_POPCNT: "i64.popcnt",
    InstructionType.I64_ADD: "i64.add",
    InstructionType.I64_SUB: "i64.sub",
    InstructionType.I64_MUL: "i64.mul",
    InstructionType.I64_DIV_S: "i64.div_s",
    InstructionType.I64_DIV_U: "i64.div_u",
    InstructionType.I64_REM_S: "i64.rem_s",
    InstructionType.I64_REM_U: "i64.rem_u",
    InstructionType.I64_AND: "i64.and",
    InstructionType.I64_OR: "i64.or",
    InstructionType.I64_XOR: "i64.xor",
    InstructionType.I64_SHL: "i64.shl",
    InstructionType.I64_SHR_S: "i64.shr_s",
    InstructionType.I64_SHR_U: "i64.shr_u",
    InstructionType.I64_ROTL: "i64.rotl",
    InstructionType.I64_ROTR: "i64.rotr",
    InstructionType.F32_ABS: "f32.abs",
    InstructionType.F32_NEG: "f32.neg",
    InstructionType.F32_CEIL: "f32.ceil",
    InstructionType.F32_FLOOR: "f32.floor",
    InstructionType.F32_TRUNC: "f32.trunc",
    InstructionType.F32_NEAREST: "f32.nearest",
    InstructionType.F32_SQRT: "f32.sqrt",
    InstructionType.F32_ADD: "f32.add",
    InstructionType.F32_SUB: "f32.sub",
    InstructionType.F32_MUL: "f32.mul",
    InstructionType.F32_DIV: "f32.div",
    InstructionType.F32_MIN: "f32.min",
    InstructionType.F32_MAX: "f32.max",
    InstructionType.F32_COPYSIGN: "f32.copysign",
    InstructionType.F64_ABS: "f64.abs",
    InstructionType.F64_NEG: "f64.neg",
    InstructionType.F64_CEIL: "f64.ceil",
    InstructionType.F64_FLOOR: "f64.floor",
    InstructionType.F64_TRUNC: "f64.trunc",
    InstructionType.F64_NEAREST: "f64.nearest",
    InstructionType.F64_SQRT: "f64.sqrt",
    InstructionType.F64_ADD: "f64.add",
    InstructionType.F64_SUB: "f64.sub",
    InstructionType.F64_MUL: "f64.mul",
    InstructionType.F64_DIV: "f64.div",
    InstructionType.F64_MIN: "f64.min",
    InstructionType.F64_MAX: "f64.max",
    InstructionType.F64_COPYSIGN: "f64.copysign",
    InstructionType.I32_WRAP_I64: "i32.wrap_i64",
    InstructionType.I32_TRUNC_F32_S: "i32.trunc_f32_s",
    InstructionType.I32_TRUNC_F32_U: "i32.trunc_f32_u",
    InstructionType.I32_TRUNC_F64_S: "i32.trunc_f64_s",
    InstructionType.I32_TRUNC_F64_U: "i32.trunc_f64_u",
    InstructionType.I64_EXTEND_I32_S: "i64.extend_i32_s",
    InstructionType.I64_EXTEND_I32_U: "i64.extend_i32_u",
    InstructionType.I64_TRUNC_F32_S: "i64.trunc_f32_s",
    InstructionType.I64_TRUNC_F32_U: "i64.trunc_f32_u",
    InstructionType.I64_TRUNC_F64_S: "i64.trunc_f64_s",
    InstructionType.I64_TRUNC_F64_U: "i64.trunc_f64_u",
    InstructionType.F32_CONVERT_I32_S: "f32.convert_i32_s",
    InstructionType.F32_CONVERT_I32_U: "f32.convert_i32_u",
    InstructionType.F32_CONVERT_I64_S: "f32.convert_i64_s",
    InstructionType.F32_CONVERT_I64_U: "f32.convert_i64_u",
    InstructionType.F32_DEMOTE_F64: "f32.demote_f64",
    InstructionType.F64_CONVERT_I32_S: "f64.convert_i32_s",
    InstructionType.F64_CONVERT_I32_U: "f64.convert_i32_u",
    InstructionType.F64_CONVERT_I64_S: "f64.convert_i64_s",
    InstructionType.F64_CONVERT_I64_U: "f64.convert_i64_u",
    InstructionType.F32_F64_PROMOTE: "f32.f64_promote",
    InstructionType.F32_I32_REINTERPRET: "f32.i32_reinterpret",
    InstructionType.F64_I64_REINTERPRET: "f64.i64_reinterpret",
    InstructionType.F32_REINTERPRET_I32: "f32.reinterpret_i32",
    InstructionType.F64_REINTERPRET_I64: "f64.reinterpret_i64",
    InstructionType.I32_EXTEND8_S: "i32.extend8_s",
    InstructionType.I32_EXTEND16_S: "i32.extend16_s",
    InstructionType.I64_EXTEND8_S: "i64.extend8_s",
    InstructionType.I64_EXTEND16_S: "i64.extend16_s",
    InstructionType.I64_EXTEND32_S: "i64.extend32_s",
    InstructionType.I32_TRUNC_SAT_F32_S: "i32.trunc_sat_f32_s",
    InstructionType.I32_TRUNC_SAT_F32_U: "i32.trunc_sat_f32_u",
    InstructionType.I32_TRUNC_SAT_F64_S: "i32.trunc_sat_f64_s",
    InstructionType.I32_TRUNC_SAT_F64_U: "i32.trunc_sat_f64_u",
    InstructionType.I64_TRUNC_SAT_F32_S: "i64.trunc_sat_f32_s",
    InstructionType.I64_TRUNC_SAT_F32_U: "i64.trunc_sat_f32_u",
    InstructionType.I64_TRUNC_SAT_F64_S: "i64.trunc_sat_f64_s",
    InstructionType.I64_TRUNC_SAT_F64_U: "i64.trunc_sat_f64_u",
    # Vector instructions,
    InstructionType.V128_LOAD: "v128.load",
    InstructionType.V128_LOAD8X8_S: "v128.load8x8_s",
    InstructionType.V128_LOAD8X8_U: "v128.load8x8_u",
    InstructionType.V128_LOAD16X4_S: "v128.load16x4_s",
    InstructionType.V128_LOAD16X4_U: "v128.load16x4_u",
    InstructionType.V128_LOAD32X2_S: "v128.load32x2_s",
    InstructionType.V128_LOAD32X2_U: "v128.load32x2_u",
    InstructionType.V128_LOAD8_SPLAT: "v128.load8_splat",
    InstructionType.V128_LOAD16_SPLAT: "v128.load16_splat",
    InstructionType.V128_LOAD32_SPLAT: "v128.load32_splat",
    InstructionType.V128_LOAD64_SPLAT: "v128.load64_splat",
    InstructionType.V128_LOAD32_ZERO: "v128.load32_zero",
    InstructionType.V128_LOAD64_ZERO: "v128.load64_zero",
    InstructionType.V128_STORE: "v128.store",
    InstructionType.V128_LOAD8_LANE: "v128.load8_lane",
    InstructionType.V128_LOAD16_LANE: "v128.load16_lane",
    InstructionType.V128_LOAD32_LANE: "v128.load32_lane",
    InstructionType.V128_LOAD64_LANE: "v128.load64_lane",
    InstructionType.V128_STORE8_LANE: "v128.store8_lane",
    InstructionType.V128_STORE16_LANE: "v128.store16_lane",
    InstructionType.V128_STORE32_LANE: "v128.store32_lane",
    InstructionType.V128_STORE64_LANE: "v128.store64_lane",
    InstructionType.V128_CONST: "v128.const",
    InstructionType.I8X16_SHUFFLE: "i8x16.shuffle",
    InstructionType.I8X16_EXTRACT_LANE_S: "i8x16.extract_lane_s",
    InstructionType.I8X16_EXTRACT_LANE_U: "i8x16.extract_lane_u",
    InstructionType.I8X16_REPLACE_LANE: "i8x16.replace_lane",
    InstructionType.I16X8_EXTRACT_LANE_S: "i16x8.extract_lane_s",
    InstructionType.I16X8_EXTRACT_LANE_U: "i16x8.extract_lane_u",
    InstructionType.I16X8_REPLACE_LANE: "i16x8.replace_lane",
    InstructionType.I32X4_EXTRACT_LANE: "i32x4.extract_lane",
    InstructionType.I32X4_REPLACE_LANE: "i32x4.replace_lane",
    InstructionType.I64X2_EXTRACT_LANE: "i64x2.extract_lane",
    InstructionType.I64X2_REPLACE_LANE: "i64x2.replace_lane",
    InstructionType.F32X4_EXTRACT_LANE: "f32x4.extract_lane",
    InstructionType.F32X4_REPLACE_LANE: "f32x4.replace_lane",
    InstructionType.F64X2_EXTRACT_LANE: "f64x2.extract_lane",
    InstructionType.F64X2_REPLACE_LANE: "f64x2.replace_lane",
    InstructionType.I8X16_SWIZZLE: "i8x16.swizzle",
    InstructionType.I8X16_SPLAT: "i8x16.splat",
    InstructionType.I16X8_SPLAT: "i16x8.splat",
    InstructionType.I32X4_SPLAT: "i32x4.splat",
    InstructionType.I64X2_SPLAT: "i64x2.splat",
    InstructionType.F32X4_SPLAT: "f32x4.splat",
    InstructionType.F64X2_SPLAT: "f64x2.splat",
    InstructionType.I8X16_EQ: "i8x16.eq",
    InstructionType.I8X16_NE: "i8x16.ne",
    InstructionType.I8X16_LT_S: "i8x16.lt_s",
    InstructionType.I8X16_LT_U: "i8x16.lt_u",
    InstructionType.I8X16_GT_S: "i8x16.gt_s",
    InstructionType.I8X16_GT_U: "i8x16.gt_u",
    InstructionType.I8X16_LE_S: "i8x16.le_s",
    InstructionType.I8X16_LE_U: "i8x16.le_u",
    InstructionType.I8X16_GE_S: "i8x16.ge_s",
    InstructionType.I8X16_GE_U: "i8x16.ge_u",
    InstructionType.I16X8_EQ: "i16x8.eq",
    InstructionType.I16X8_NE: "i16x8.ne",
    InstructionType.I16X8_LT_S: "i16x8.lt_s",
    InstructionType.I16X8_LT_U: "i16x8.lt_u",
    InstructionType.I16X8_GT_S: "i16x8.gt_s",
    InstructionType.I16X8_GT_U: "i16x8.gt_u",
    InstructionType.I16X8_LE_S: "i16x8.le_s",
    InstructionType.I16X8_LE_U: "i16x8.le_u",
    InstructionType.I16X8_GE_S: "i16x8.ge_s",
    InstructionType.I16X8_GE_U: "i16x8.ge_u",
    InstructionType.I32X4_EQ: "i32x4.eq",
    InstructionType.I32X4_NE: "i32x4.ne",
    InstructionType.I32X4_LT_S: "i32x4.lt_s",
    InstructionType.I32X4_LT_U: "i32x4.lt_u",
    InstructionType.I32X4_GT_S: "i32x4.gt_s",
    InstructionType.I32X4_GT_U: "i32x4.gt_u",
    InstructionType.I32X4_LE_S: "i32x4.le_s",
    InstructionType.I32X4_LE_U: "i32x4.le_u",
    InstructionType.I32X4_GE_S: "i32x4.ge_s",
    InstructionType.I32X4_GE_U: "i32x4.ge_u",
    InstructionType.I64X2_EQ: "i64x2.eq",
    InstructionType.I64X2_NE: "i64x2.ne",
    InstructionType.I64X2_LT_S: "i64x2.lt_s",
    InstructionType.I64X2_GT_S: "i64x2.gt_s",
    InstructionType.I64X2_LE_S: "i64x2.le_s",
    InstructionType.I64X2_GE_S: "i64x2.ge_s",
    InstructionType.F32X4_EQ: "f32x4.eq",
    InstructionType.F32X4_NE: "f32x4.ne",
    InstructionType.F32X4_LT: "f32x4.lt",
    InstructionType.F32X4_GT: "f32x4.gt",
    InstructionType.F32X4_LE: "f32x4.le",
    InstructionType.F32X4_GE: "f32x4.ge",
    InstructionType.F64X2_EQ: "f64x2.eq",
    InstructionType.F64X2_NE: "f64x2.ne",
    InstructionType.F64X2_LT: "f64x2.lt",
    InstructionType.F64X2_GT: "f64x2.gt",
    InstructionType.F64X2_LE: "f64x2.le",
    InstructionType.F64X2_GE: "f64x2.ge",
    InstructionType.V128_NOT: "v128.not",
    InstructionType.V128_AND: "v128.and",
    InstructionType.V128_ANDNOT: "v128.andnot",
    InstructionType.V128_OR: "v128.or",
    InstructionType.V128_XOR: "v128.xor",
    InstructionType.V128_BITSELECT: "v128.bitselect",
    InstructionType.V128_ANY_TRUE: "v128.any_true",
    InstructionType.I8X16_ABS: "i8x16.abs",
    InstructionType.I8X16_NEG: "i8x16.neg",
    InstructionType.I8X16_POPCNT: "i8x16.popcnt",
    InstructionType.I8X16_ALL_TRUE: "i8x16.all_true",
    InstructionType.I8X16_BITMASK: "i8x16.bitmask",
    InstructionType.I8X16_NARROW_I16X8_S: "i8x16.narrow_i16x8_s",
    InstructionType.I8X16_NARROW_I16X8_U: "i8x16.narrow_i16x8_u",
    InstructionType.I8X16_SHL: "i8x16.shl",
    InstructionType.I8X16_SHR_S: "i8x16.shr_s",
    InstructionType.I8X16_SHR_U: "i8x16.shr_u",
    InstructionType.I8X16_ADD: "i8x16.add",
    InstructionType.I8X16_ADD_SAT_S: "i8x16.add_sat_s",
    InstructionType.I8X16_ADD_SAT_U: "i8x16.add_sat_u",
    InstructionType.I8X16_SUB: "i8x16.sub",
    InstructionType.I8X16_SUB_SAT_S: "i8x16.sub_sat_s",
    InstructionType.I8X16_SUB_SAT_U: "i8x16.sub_sat_u",
    InstructionType.I8X16_MIN_S: "i8x16.min_s",
    InstructionType.I8X16_MIN_U: "i8x16.min_u",
    InstructionType.I8X16_MAX_S: "i8x16.max_s",
    InstructionType.I8X16_MAX_U: "i8x16.max_u",
    InstructionType.I8X16_AVGR_U: "i8x16.avgr_u",
}


@dataclasses.dataclass
class Instruction:
    """An instruction."""

    instruction_type: InstructionType
    # In standard Pythons, ints are bignums, and floats are represented as doubles.
    operands: list[Union[values.ValueType, int, float, "Block"]]
    # For structured control instructions, where does ending it go? For all other
    # instructions, where is the next instruction to execute?
    continuation_pc: int = 0
    # Only for the IF instruction, where is the next instruction to execute if the
    # condition is false?
    else_continuation_pc: int = 0

    @staticmethod
    def read(f: BufferedIOBase) -> Instruction:
        """Reads and returns an Instruction.

        Returns:
            The Instruction read.

        Raises:
            ValueError: upon encountering a bad instruction type.
        """
        opcode = f.read(1)[0]

        # I couldn't find it explicitly stated, but at the very least, opcodes
        # 0xFC and 0xFD are "extension" opcodes, where you have to read the next
        # encoded u32 to get the instruction.

        if opcode == 0xFC or opcode == 0xFD:
            ext_opcode = leb128.u.decode_reader(f)[0]
            # We're just going to assume that there aren't any extended opcodes past
            # 0xFF.
            if ext_opcode > 0xFF:
                raise ValueError(
                    f"Unknown extended opcode {opcode:02X} + {ext_opcode:02X}"
                )
            opcode = (opcode << 8) | ext_opcode

        instruction_type = InstructionType(opcode)

        if instruction_type in BYTE_INSNS:
            return Instruction(instruction_type, [values.ValueType(f.read(1)[0])])
        if instruction_type in BYTE8_INSNS:
            return Instruction(instruction_type, [struct.unpack("<Q", f.read(8))[0]])
        if instruction_type in U32_INSNS or instruction_type in LANE_INSNS:
            return Instruction(instruction_type, [leb128.u.decode_reader(f)[0]])
        if instruction_type in U32X2_INSNS or instruction_type in MEMARG_INSNS:
            return Instruction(
                instruction_type, [leb128.u.decode_reader(f)[0] for _ in range(2)]
            )
        if instruction_type in MEMARG_LANE_INSNS:
            return Instruction(
                instruction_type, [leb128.u.decode_reader(f)[0] for _ in range(3)]
            )
        if instruction_type in LANE8_INSNS:
            return Instruction(
                instruction_type, [leb128.u.decode_reader(f)[0] for _ in range(16)]
            )
        if instruction_type in VALTYPE_VECTOR_INSNS:
            return Instruction(
                instruction_type,
                [
                    values.ValueType(f.read(1)[0])
                    for _ in range(leb128.u.decode_reader(f)[0])
                ],
            )
        if instruction_type in I32_INSNS or instruction_type in I64_INSNS:
            return Instruction(instruction_type, [leb128.i.decode_reader(f)[0]])
        if instruction_type in F32_INSNS:
            # Little-endian IEEE 754 float
            return Instruction(instruction_type, [struct.unpack("<f", f.read(4))[0]])
        if instruction_type in F64_INSNS:
            # Little-endian IEEE 754 double
            return Instruction(instruction_type, [struct.unpack("<d", f.read(8))[0]])
        if instruction_type in BLOCK_INSNS:
            return Instruction(instruction_type, [Block.read(f)])
        if instruction_type in SWITCH_INSNS:
            table_size = leb128.u.decode_reader(f)[0]
            # The last label index is the default.
            return Instruction(
                instruction_type,
                [leb128.u.decode_reader(f)[0] for _ in range(table_size + 1)],
            )
        else:
            return Instruction(instruction_type, [])

    def to_str(self, indents: int) -> str:
        """Returns a string representation of the instruction."""
        operands = []
        for operand in self.operands:
            if isinstance(operand, values.ValueType):
                operands.append(operand.name)
            elif isinstance(operand, Block):
                operands.append(operand.to_str(indents + 1))
            else:
                operands.append(str(operand))
        return (
            f"{'  '*indents}{INSTRUCTION_TYPE_STRINGS[self.instruction_type]} "
            f"({', '.join(operands)})"
        )

    def __repr__(self) -> str:
        return self.to_str(0)


@dataclasses.dataclass
class Block:
    """A Block is a list of instructions terminated by END.

    Instructions can themselves contain blocks, so this is a recursive definition.
    """

    block_type: None | values.ValueType | int
    instructions: list[Instruction]
    # Only valid for the if-else instruction.
    else_instructions: list[Instruction]

    @staticmethod
    def read(f: BufferedIOBase) -> Block:
        """Reads and returns a Block from a binary stream."""
        # The secret here is that if the first byte is >= 0x40, then the signed
        # LEB128 decode is a negative number -- specifically, a 7-bit signed negative
        # number. This means that any number between 0x40 and 0x7F can indicate
        # something other than an index.
        block_type = leb128.i.decode_reader(f)[0]
        if block_type < 0:
            block_type += 0x80
            if block_type == 0x40:
                block_type = None
            else:
                block_type = values.ValueType(block_type)

        instructions = []
        else_instructions = []
        while True:
            insn = Instruction.read(f)
            instructions.append(insn)
            if insn.instruction_type == InstructionType.END:
                break
            if insn.instruction_type == InstructionType.ELSE:
                while True:
                    insn = Instruction.read(f)
                    else_instructions.append(insn)
                    if insn.instruction_type == InstructionType.END:
                        break
                break

        return Block(block_type, instructions, else_instructions)

    def to_str(self, indents: int) -> str:
        """Returns a string representation of the block."""
        if self.block_type is None:
            block_type = ""
        elif isinstance(self.block_type, values.ValueType):
            block_type = f"[{self.block_type.name}]:"
        else:
            block_type = f"[{self.block_type}]:"
        insns = "".join([i.to_str(indents + 1) for i in self.instructions])
        else_insns = ""
        if self.else_instructions:
            else_insns = "".join(
                [i.to_str(indents + 1) for i in self.else_instructions]
            )
            else_insns = f"{'  '*indents}else:\n{else_insns}"
        return f"{block_type}\n{insns}{else_insns}"
