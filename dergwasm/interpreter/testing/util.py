"""Utility functions for testing the interpreter."""
from dergwasm.interpreter.insn import Instruction, InstructionType, Block
from dergwasm.interpreter.values import ValueType


def i32_const(value: int) -> Instruction:
    return Instruction(InstructionType.I32_CONST, [value], 0, 0)


def i32_add() -> Instruction:
    return Instruction(InstructionType.I32_ADD, [], 0, 0)


def i32_sub() -> Instruction:
    return Instruction(InstructionType.I32_SUB, [], 0, 0)


def i32_mul() -> Instruction:
    return Instruction(InstructionType.I32_MUL, [], 0, 0)


def nop() -> Instruction:
    return Instruction(InstructionType.NOP, [], 0, 0)


def ret() -> Instruction:
    return Instruction(InstructionType.RETURN, [], 0, 0)


def drop() -> Instruction:
    return Instruction(InstructionType.DROP, [], 0, 0)


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


def i32_lt_u() -> Instruction:
    return Instruction(InstructionType.I32_LT_U, [], 0, 0)


def i32_load(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_LOAD, [alignment, offset], 0, 0)


def i32_store(alignment: int, offset: int) -> Instruction:
    return Instruction(InstructionType.I32_STORE, [alignment, offset], 0, 0)


def memory_init(dataidx: int, memidx: int) -> Instruction:
    return Instruction(InstructionType.MEMORY_INIT, [dataidx, memidx], 0, 0)


def data_drop(dataidx: int) -> Instruction:
    return Instruction(InstructionType.DATA_DROP, [dataidx], 0, 0)


def call(funcidx: int) -> Instruction:
    return Instruction(InstructionType.CALL, [funcidx], 0, 0)
