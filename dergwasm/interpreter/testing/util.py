"""Utility functions for testing the interpreter."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=unused-argument

import struct
from dergwasm.interpreter.insn import Instruction, InstructionType, Block
from dergwasm.interpreter.values import ValueType


def i32_const(value: int) -> Instruction:
    return Instruction(InstructionType.I32_CONST, [value], 0, 0)


def i64_const(value: int) -> Instruction:
    return Instruction(InstructionType.I64_CONST, [value], 0, 0)


def f32_const(value: float) -> Instruction:
    # Necessary because python floats are 64-bit, but wasm F32s are 32-bit.
    val32 = struct.unpack('f', struct.pack('f', value))[0]
    return Instruction(InstructionType.F32_CONST, [val32], 0, 0)


def f64_const(value: float) -> Instruction:
    return Instruction(InstructionType.F64_CONST, [value], 0, 0)


def br(labelidx: int) -> Instruction:
    return Instruction(InstructionType.BR, [labelidx], 0, 0)


def br_table(*labelidxs: int) -> Instruction:
    return Instruction(InstructionType.BR_TABLE, list(labelidxs), 0, 0)


def br_if(labelidx: int) -> Instruction:
    return Instruction(InstructionType.BR_IF, [labelidx], 0, 0)


def end() -> Instruction:
    return Instruction(InstructionType.END, [], 0, 0)


def void_block(*instructions: Instruction) -> Instruction:
    """A block that returns nothing."""
    ended_instructions = list(instructions) + [end()]
    return Instruction(
        InstructionType.BLOCK, [Block(None, ended_instructions, [])], 0, 0
    )


def i32_block(*instructions: Instruction) -> Instruction:
    """A block that returns an I32."""
    ended_instructions = list(instructions) + [end()]
    return Instruction(
        InstructionType.BLOCK, [Block(ValueType.I32, ended_instructions, [])], 0, 0
    )


def if_(*instructions: Instruction) -> Instruction:
    ended_instructions = list(instructions) + [end()]
    return Instruction(
        InstructionType.IF, [Block(ValueType.I32, ended_instructions, [])], 0, 0
    )


def else_() -> Instruction:
    return Instruction(InstructionType.ELSE, [], 0, 0)


def if_else(if_insns: list[Instruction], else_insns: list[Instruction]) -> Instruction:
    ended_if_insns = list(if_insns) + [else_()]
    ended_else_insns = list(else_insns) + [end()]
    return Instruction(
        InstructionType.IF,
        [Block(ValueType.I32, ended_if_insns, ended_else_insns)],
        0,
        0,
    )


def i32_loop(func_type_idx: int, *instructions: Instruction) -> Instruction:
    # An i32_loop will take in an I32 and return an I32.
    ended_instructions = list(instructions) + [end()]
    return Instruction(
        InstructionType.LOOP,
        [Block(func_type_idx, ended_instructions, [end()])],
        0,
        0,
    )


def local_tee(localidx: int) -> Instruction:
    return Instruction(InstructionType.LOCAL_TEE, [localidx], 0, 0)


def local_set(localidx: int) -> Instruction:
    return Instruction(InstructionType.LOCAL_SET, [localidx], 0, 0)


def local_get(localidx: int) -> Instruction:
    return Instruction(InstructionType.LOCAL_GET, [localidx], 0, 0)


def global_set(globalidx: int) -> Instruction:
    return Instruction(InstructionType.GLOBAL_SET, [globalidx], 0, 0)


def global_get(globalidx: int) -> Instruction:
    return Instruction(InstructionType.GLOBAL_GET, [globalidx], 0, 0)


def i32_load(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_LOAD, [alignment, offset], 0, 0)


def i32_store(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_STORE, [alignment, offset], 0, 0)


def i32_store8(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_STORE8, [alignment, offset], 0, 0)


def i32_store16(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_STORE16, [alignment, offset], 0, 0)


def i64_load(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I64_LOAD, [alignment, offset], 0, 0)


def i64_store(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I64_STORE, [alignment, offset], 0, 0)


def i64_store8(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I64_STORE8, [alignment, offset], 0, 0)


def i64_store16(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I64_STORE16, [alignment, offset], 0, 0)


def i64_store32(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I64_STORE32, [alignment, offset], 0, 0)


def memory_init(dataidx: int, memidx: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_INIT, [dataidx, memidx], 0, 0)


def memory_size(memidx: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_SIZE, [memidx], 0, 0)


def memory_grow(memidx: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_GROW, [memidx], 0, 0)


def memory_copy(memidx0: int, memidx1: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_COPY, [memidx0, memidx1], 0, 0)


def memory_fill(memidx: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_FILL, [memidx], 0, 0)


def data_drop(dataidx: int) -> Instruction:
    return Instruction(InstructionType.DATA_DROP, [dataidx], 0, 0)


def call(funcidx: int) -> Instruction:
    return Instruction(InstructionType.CALL, [funcidx], 0, 0)


def noarg(insn_type: InstructionType):
    return Instruction(insn_type, [], 0, 0)
