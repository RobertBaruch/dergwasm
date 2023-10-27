"""Evaluates instructions."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import struct
from typing import Callable, Union, cast

from dergwasm.interpreter.insn import InstructionType, Block
from dergwasm.interpreter import values
from dergwasm.interpreter.machine import Machine
from dergwasm.interpreter.binary import FuncType

EvalOperands = list[Union[values.ValueType, int, float, Block]]
EvalFunc = Callable[[Machine, EvalOperands], None]


# Control instructions
def unreachable(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError("unreachable instruction reached!")


def nop(machine: Machine, operands: EvalOperands) -> None:
    return


def block(machine: Machine, operands: EvalOperands) -> None:
    f = machine.get_current_frame()
    block_func_type = operands[0].block_type
    if isinstance(block_func_type, int):
        block_func_type = f.module.func_types[block_func_type]
    elif block_func_type is not None:
        block_func_type = FuncType([], [block_func_type])
    else:
        block_func_type = FuncType([], [])
    label = values.Label(len(block_func_type.results), f.pc + 1)
    # Slide the label under the parameters.
    block_vals = [machine.pop() for _ in block_func_type.parameters]
    machine.push(label)
    for v in reversed(block_vals):
        machine.push(v)


def loop(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def if_(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Not really an instruction, but it's used to end an if-block and start an else-block.,
def else_(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError(
        "else psuedo-instruction reached! There aren't supposed to be any!"
    )


# Not really an instruction, but it's used to end blocks.,
def end(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError(
        "end psuedo-instruction reached! There aren't supposed to be any!"
    )


def br(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError("br is supposed to be handled in the machine")


def br_if(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError("br_if is supposed to be handled in the machine")


def br_table(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError("br_table is supposed to be handled in the machine")


def return_(machine: Machine, operands: EvalOperands) -> None:
    raise RuntimeError("return is supposed to be handled in the machine")


def call(machine: Machine, operands: EvalOperands) -> None:
    f = machine.get_current_frame()
    a = f.module.funcaddrs[operands[0]]
    machine.invoke_func(a)


def call_indirect(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Reference instructions,
def ref_null(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def ref_is_null(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def ref_func(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Parametric instructions,
def drop(machine: Machine, operands: EvalOperands) -> None:
    machine.pop()


def select(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def select_vec(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Variable instructions,
def local_get(machine: Machine, operands: EvalOperands) -> None:
    """Gets a local variable from the current frame.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    machine.push(f.local_vars[operands[0]])


def local_set(machine: Machine, operands: EvalOperands) -> None:
    """Sets a local variable in the current frame.

    Expects the value to set to be on the top of the stack.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    f.local_vars[operands[0]] = machine.pop()


def local_tee(machine: Machine, operands: EvalOperands) -> None:
    """Sets a local variable in the current frame, but doesn't consume stack.

    Expects the value to set to be on the top of the stack.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    f.local_vars[operands[0]] = machine.peek()


def global_get(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def global_set(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Table instructions,
def table_get(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_set(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_init(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def elem_drop(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_copy(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_grow(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_size(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def table_fill(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Memory instructions,
def i32_load(machine: Machine, operands: EvalOperands) -> None:
    """Loads a 32-bit integer from memory.

    Expects the base address (i32) to be on the top of the stack.

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    i = int(cast(values.Value, machine.pop()).value)  # base (i32)
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)
    if ea + 4 > len(mem):
        raise RuntimeError(
            "i32.load: access out of bounds: base {i} offset {operands[1]}"
        )
    val = struct.unpack("<i", mem[ea : ea + 4])[0]
    machine.push(values.Value(values.ValueType.I32, val))


def i64_load(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_load(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_load(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_load8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_load8_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_load16_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_load16_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load8_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load16_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load16_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_load32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_store(machine: Machine, operands: EvalOperands) -> None:
    """Stores a 32-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i32), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = int(cast(values.Value, machine.pop()).value)  # value to store
    i = int(cast(values.Value, machine.pop()).value)  # i32 base
    ea = i + int(operands[1])  # effective address
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"i32.store: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 4] = struct.pack("<i", c)


def i64_store(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_store(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_store(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_store8(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_store16(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_store8(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_store16(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_store32(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def memory_size(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def memory_grow(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def memory_init(machine: Machine, operands: EvalOperands) -> None:
    """Initializes a memory segment."""
    assert len(operands) == 2
    assert isinstance(operands[0], int)  # dataindex
    assert isinstance(operands[1], int)  # memindex = 0

    curr_frame = cast(values.Frame, machine.get_current_frame())
    ma = curr_frame.module.memaddrs[operands[1]]
    mem = machine.get_mem(ma)
    da = curr_frame.module.dataaddrs[operands[0]]
    data = machine.get_data(da)
    n = int(cast(values.Value, machine.pop()).value)  # data size, i32
    s = int(cast(values.Value, machine.pop()).value)  # source, i32
    d = int(cast(values.Value, machine.pop()).value)  # destination, i32

    if s + n > len(data):
        raise RuntimeError("memory.init: source is out of bounds")
    if d + n > len(mem):
        raise RuntimeError("memory.init: destination is out of bounds")
    if n == 0:
        return
    mem[d : d + n] = data[s : s + n]


def data_drop(machine: Machine, operands: EvalOperands) -> None:
    """Drops a data segment."""
    assert len(operands) == 1
    assert isinstance(operands[0], int)  # dataindex

    da = machine.get_current_frame().module.dataaddrs[operands[0]]
    machine.datas[da] = bytearray()


def memory_copy(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def memory_fill(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Numeric instructions,
def i32_const(machine: Machine, operands: EvalOperands) -> None:
    """Pushes a 32-bit integer constant onto the stack."""
    assert len(operands) == 1
    assert isinstance(operands[0], int)
    machine.push(values.Value(values.ValueType.I32, operands[0]))


def i64_const(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_const(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_const(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_eqz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_lt_u(machine: Machine, operands: EvalOperands) -> None:
    c2 = int(cast(values.Value, machine.pop()).value) & 0xFFFFFFFF
    c1 = int(cast(values.Value, machine.pop()).value) & 0xFFFFFFFF
    machine.push(values.Value(values.ValueType.I32, int(c1 < c2)))


def i32_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_gt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_le_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_ge_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_eqz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_lt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_gt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_le_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_ge_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_lt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_gt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_le(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_ge(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_lt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_gt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_le(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_ge(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_clz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_ctz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_popcnt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_add(machine: Machine, operands: EvalOperands) -> None:
    c2 = int(cast(values.Value, machine.pop()).value)
    c1 = int(cast(values.Value, machine.pop()).value)
    machine.push(values.Value(values.ValueType.I32, (c1 + c2) % 0x100000000))


def i32_sub(machine: Machine, operands: EvalOperands) -> None:
    c2 = int(cast(values.Value, machine.pop()).value)
    c1 = int(cast(values.Value, machine.pop()).value)
    machine.push(values.Value(values.ValueType.I32, (c1 - c2) % 0x100000000))


def i32_mul(machine: Machine, operands: EvalOperands) -> None:
    c2 = int(cast(values.Value, machine.pop()).value)
    c1 = int(cast(values.Value, machine.pop()).value)
    machine.push(values.Value(values.ValueType.I32, (c1 * c2) % 0x100000000))


def i32_div_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_div_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_rem_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_rem_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_and(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_or(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_xor(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_shl(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_shr_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_shr_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_rotl(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_rotr(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_clz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_ctz(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_popcnt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_add(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_sub(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_mul(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_div_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_div_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_rem_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_rem_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_and(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_or(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_xor(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_shl(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_shr_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_shr_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_rotl(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_rotr(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_abs(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_neg(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_ceil(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_floor(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_trunc(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_nearest(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_sqrt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_add(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_sub(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_mul(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_div(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_min(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_max(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_copysign(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_abs(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_neg(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_ceil(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_floor(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_trunc(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_nearest(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_sqrt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_add(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_sub(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_mul(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_div(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_min(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_max(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_copysign(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_i32_wrap(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_f32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_f32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_f64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_f64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_extend_i32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_extend_i32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_f32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_f32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_f64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_f64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_convert_i32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_convert_i32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_convert_i64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_convert_i64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_demote_f64(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_convert_i32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_convert_i32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_convert_i64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_convert_i64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_f64_promote(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_i32_reinterpret(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_i64_reinterpret(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32_reinterpret_i32(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64_reinterpret_i64(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_extend8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_extend16_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_extend8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_extend16_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_extend32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_sat_f32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_sat_f32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_sat_f64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32_trunc_sat_f64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_sat_f32_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_sat_f32_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_sat_f64_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64_trunc_sat_f64_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


# Vector instructions,
def v128_load(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load8x8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load8x8_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load16x4_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load16x4_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load32x2_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load32x2_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load8_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load16_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load32_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load64_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load32_zero(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load64_zero(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_store(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load8_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load16_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load32_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_load64_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_store8_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_store16_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_store32_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_store64_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_const(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_shuffle(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_extract_lane_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_extract_lane_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_extract_lane_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_extract_lane_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_extract_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_extract_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_extract_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_extract_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_replace_lane(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_swizzle(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_splat(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_lt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_gt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_le_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_ge_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_lt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_gt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_le_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i16x8_ge_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_lt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_gt_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_le_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i32x4_ge_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_lt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_gt_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_le_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i64x2_ge_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_lt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_gt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_le(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f32x4_ge(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_eq(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_ne(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_lt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_gt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_le(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def f64x2_ge(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_not(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_and(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_andnot(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_or(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_xor(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_bitselect(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def v128_any_true(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_abs(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_neg(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_popcnt(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_all_true(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_bitmask(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_narrow_i16x8_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_narrow_i16x8_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_shl(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_shr_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_shr_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_add(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_add_sat_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_add_sat_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_sub(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_sub_sat_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_sub_sat_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_min_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_min_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_max_s(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_max_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def i8x16_avgr_u(machine: Machine, operands: EvalOperands) -> None:
    raise NotImplementedError


def eval_insn(
    instruction: InstructionType, operands: EvalOperands, machine: Machine
) -> None:
    """Evaluates an instruction."""
    try:
        print(f"{instruction} {operands}")
        INSTRUCTION_FUNCS[instruction](machine, operands)
    except NotImplementedError:
        print("Instruction not implemented:", instruction)
        print("Current stack:")
        try:
            while True:
                v = machine.pop()
                print(v)
        except IndexError:
            pass
        raise


INSTRUCTION_FUNCS: dict[InstructionType, EvalFunc] = {
    # Control instructions
    InstructionType.UNREACHABLE: unreachable,
    InstructionType.NOP: nop,
    InstructionType.BLOCK: block,
    InstructionType.LOOP: loop,
    InstructionType.IF: if_,
    # Not really an instruction, but it's used to end an if-block and start an else-block.,
    InstructionType.ELSE: else_,
    # Not really an instruction, but it's used to end blocks.,
    InstructionType.END: end,
    InstructionType.BR: br,
    InstructionType.BR_IF: br_if,
    InstructionType.BR_TABLE: br_table,
    InstructionType.RETURN: return_,
    InstructionType.CALL: call,
    InstructionType.CALL_INDIRECT: call_indirect,
    # Reference instructions,
    InstructionType.REF_NULL: ref_null,
    InstructionType.REF_IS_NULL: ref_is_null,
    InstructionType.REF_FUNC: ref_func,
    # Parametric instructions,
    InstructionType.DROP: drop,
    InstructionType.SELECT: select,
    InstructionType.SELECT_VEC: select_vec,
    # Variable instructions,
    InstructionType.LOCAL_GET: local_get,
    InstructionType.LOCAL_SET: local_set,
    InstructionType.LOCAL_TEE: local_tee,
    InstructionType.GLOBAL_GET: global_get,
    InstructionType.GLOBAL_SET: global_set,
    # Table instructions,
    InstructionType.TABLE_GET: table_get,
    InstructionType.TABLE_SET: table_set,
    InstructionType.TABLE_INIT: table_init,
    InstructionType.ELEM_DROP: elem_drop,
    InstructionType.TABLE_COPY: table_copy,
    InstructionType.TABLE_GROW: table_grow,
    InstructionType.TABLE_SIZE: table_size,
    InstructionType.TABLE_FILL: table_fill,
    # Memory instructions,
    InstructionType.I32_LOAD: i32_load,
    InstructionType.I64_LOAD: i64_load,
    InstructionType.F32_LOAD: f32_load,
    InstructionType.F64_LOAD: f64_load,
    InstructionType.I32_LOAD8_S: i32_load8_s,
    InstructionType.I32_LOAD8_U: i32_load8_u,
    InstructionType.I32_LOAD16_S: i32_load16_s,
    InstructionType.I32_LOAD16_U: i32_load16_u,
    InstructionType.I64_LOAD8_S: i64_load8_s,
    InstructionType.I64_LOAD8_U: i64_load8_u,
    InstructionType.I64_LOAD16_S: i64_load16_s,
    InstructionType.I64_LOAD16_U: i64_load16_u,
    InstructionType.I64_LOAD32_S: i64_load32_s,
    InstructionType.I64_LOAD32_U: i64_load32_u,
    InstructionType.I32_STORE: i32_store,
    InstructionType.I64_STORE: i64_store,
    InstructionType.F32_STORE: f32_store,
    InstructionType.F64_STORE: f64_store,
    InstructionType.I32_STORE8: i32_store8,
    InstructionType.I32_STORE16: i32_store16,
    InstructionType.I64_STORE8: i64_store8,
    InstructionType.I64_STORE16: i64_store16,
    InstructionType.I64_STORE32: i64_store32,
    InstructionType.MEMORY_SIZE: memory_size,
    InstructionType.MEMORY_GROW: memory_grow,
    InstructionType.MEMORY_INIT: memory_init,
    InstructionType.DATA_DROP: data_drop,
    InstructionType.MEMORY_COPY: memory_copy,
    InstructionType.MEMORY_FILL: memory_fill,
    # Numeric instructions,
    InstructionType.I32_CONST: i32_const,
    InstructionType.I64_CONST: i64_const,
    InstructionType.F32_CONST: f32_const,
    InstructionType.F64_CONST: f64_const,
    InstructionType.I32_EQZ: i32_eqz,
    InstructionType.I32_EQ: i32_eq,
    InstructionType.I32_NE: i32_ne,
    InstructionType.I32_LT_S: i32_lt_s,
    InstructionType.I32_LT_U: i32_lt_u,
    InstructionType.I32_GT_S: i32_gt_s,
    InstructionType.I32_GT_U: i32_gt_u,
    InstructionType.I32_LE_S: i32_le_s,
    InstructionType.I32_LE_U: i32_le_u,
    InstructionType.I32_GE_S: i32_ge_s,
    InstructionType.I32_GE_U: i32_ge_u,
    InstructionType.I64_EQZ: i64_eqz,
    InstructionType.I64_EQ: i64_eq,
    InstructionType.I64_NE: i64_ne,
    InstructionType.I64_LT_S: i64_lt_s,
    InstructionType.I64_LT_U: i64_lt_u,
    InstructionType.I64_GT_S: i64_gt_s,
    InstructionType.I64_GT_U: i64_gt_u,
    InstructionType.I64_LE_S: i64_le_s,
    InstructionType.I64_LE_U: i64_le_u,
    InstructionType.I64_GE_S: i64_ge_s,
    InstructionType.I64_GE_U: i64_ge_u,
    InstructionType.F32_EQ: f32_eq,
    InstructionType.F32_NE: f32_ne,
    InstructionType.F32_LT: f32_lt,
    InstructionType.F32_GT: f32_gt,
    InstructionType.F32_LE: f32_le,
    InstructionType.F32_GE: f32_ge,
    InstructionType.F64_EQ: f64_eq,
    InstructionType.F64_NE: f64_ne,
    InstructionType.F64_LT: f64_lt,
    InstructionType.F64_GT: f64_gt,
    InstructionType.F64_LE: f64_le,
    InstructionType.F64_GE: f64_ge,
    InstructionType.I32_CLZ: i32_clz,
    InstructionType.I32_CTZ: i32_ctz,
    InstructionType.I32_POPCNT: i32_popcnt,
    InstructionType.I32_ADD: i32_add,
    InstructionType.I32_SUB: i32_sub,
    InstructionType.I32_MUL: i32_mul,
    InstructionType.I32_DIV_S: i32_div_s,
    InstructionType.I32_DIV_U: i32_div_u,
    InstructionType.I32_REM_S: i32_rem_s,
    InstructionType.I32_REM_U: i32_rem_u,
    InstructionType.I32_AND: i32_and,
    InstructionType.I32_OR: i32_or,
    InstructionType.I32_XOR: i32_xor,
    InstructionType.I32_SHL: i32_shl,
    InstructionType.I32_SHR_S: i32_shr_s,
    InstructionType.I32_SHR_U: i32_shr_u,
    InstructionType.I32_ROTL: i32_rotl,
    InstructionType.I32_ROTR: i32_rotr,
    InstructionType.I64_CLZ: i64_clz,
    InstructionType.I64_CTZ: i64_ctz,
    InstructionType.I64_POPCNT: i64_popcnt,
    InstructionType.I64_ADD: i64_add,
    InstructionType.I64_SUB: i64_sub,
    InstructionType.I64_MUL: i64_mul,
    InstructionType.I64_DIV_S: i64_div_s,
    InstructionType.I64_DIV_U: i64_div_u,
    InstructionType.I64_REM_S: i64_rem_s,
    InstructionType.I64_REM_U: i64_rem_u,
    InstructionType.I64_AND: i64_and,
    InstructionType.I64_OR: i64_or,
    InstructionType.I64_XOR: i64_xor,
    InstructionType.I64_SHL: i64_shl,
    InstructionType.I64_SHR_S: i64_shr_s,
    InstructionType.I64_SHR_U: i64_shr_u,
    InstructionType.I64_ROTL: i64_rotl,
    InstructionType.I64_ROTR: i64_rotr,
    InstructionType.F32_ABS: f32_abs,
    InstructionType.F32_NEG: f32_neg,
    InstructionType.F32_CEIL: f32_ceil,
    InstructionType.F32_FLOOR: f32_floor,
    InstructionType.F32_TRUNC: f32_trunc,
    InstructionType.F32_NEAREST: f32_nearest,
    InstructionType.F32_SQRT: f32_sqrt,
    InstructionType.F32_ADD: f32_add,
    InstructionType.F32_SUB: f32_sub,
    InstructionType.F32_MUL: f32_mul,
    InstructionType.F32_DIV: f32_div,
    InstructionType.F32_MIN: f32_min,
    InstructionType.F32_MAX: f32_max,
    InstructionType.F32_COPYSIGN: f32_copysign,
    InstructionType.F64_ABS: f64_abs,
    InstructionType.F64_NEG: f64_neg,
    InstructionType.F64_CEIL: f64_ceil,
    InstructionType.F64_FLOOR: f64_floor,
    InstructionType.F64_TRUNC: f64_trunc,
    InstructionType.F64_NEAREST: f64_nearest,
    InstructionType.F64_SQRT: f64_sqrt,
    InstructionType.F64_ADD: f64_add,
    InstructionType.F64_SUB: f64_sub,
    InstructionType.F64_MUL: f64_mul,
    InstructionType.F64_DIV: f64_div,
    InstructionType.F64_MIN: f64_min,
    InstructionType.F64_MAX: f64_max,
    InstructionType.F64_COPYSIGN: f64_copysign,
    InstructionType.I64_I32_WRAP: i64_i32_wrap,
    InstructionType.I32_TRUNC_F32_S: i32_trunc_f32_s,
    InstructionType.I32_TRUNC_F32_U: i32_trunc_f32_u,
    InstructionType.I32_TRUNC_F64_S: i32_trunc_f64_s,
    InstructionType.I32_TRUNC_F64_U: i32_trunc_f64_u,
    InstructionType.I64_EXTEND_I32_S: i64_extend_i32_s,
    InstructionType.I64_EXTEND_I32_U: i64_extend_i32_u,
    InstructionType.I64_TRUNC_F32_S: i64_trunc_f32_s,
    InstructionType.I64_TRUNC_F32_U: i64_trunc_f32_u,
    InstructionType.I64_TRUNC_F64_S: i64_trunc_f64_s,
    InstructionType.I64_TRUNC_F64_U: i64_trunc_f64_u,
    InstructionType.F32_CONVERT_I32_S: f32_convert_i32_s,
    InstructionType.F32_CONVERT_I32_U: f32_convert_i32_u,
    InstructionType.F32_CONVERT_I64_S: f32_convert_i64_s,
    InstructionType.F32_CONVERT_I64_U: f32_convert_i64_u,
    InstructionType.F32_DEMOTE_F64: f32_demote_f64,
    InstructionType.F64_CONVERT_I32_S: f64_convert_i32_s,
    InstructionType.F64_CONVERT_I32_U: f64_convert_i32_u,
    InstructionType.F64_CONVERT_I64_S: f64_convert_i64_s,
    InstructionType.F64_CONVERT_I64_U: f64_convert_i64_u,
    InstructionType.F32_F64_PROMOTE: f32_f64_promote,
    InstructionType.F32_I32_REINTERPRET: f32_i32_reinterpret,
    InstructionType.F64_I64_REINTERPRET: f64_i64_reinterpret,
    InstructionType.F32_REINTERPRET_I32: f32_reinterpret_i32,
    InstructionType.F64_REINTERPRET_I64: f64_reinterpret_i64,
    InstructionType.I32_EXTEND8_S: i32_extend8_s,
    InstructionType.I32_EXTEND16_S: i32_extend16_s,
    InstructionType.I64_EXTEND8_S: i64_extend8_s,
    InstructionType.I64_EXTEND16_S: i64_extend16_s,
    InstructionType.I64_EXTEND32_S: i64_extend32_s,
    InstructionType.I32_TRUNC_SAT_F32_S: i32_trunc_sat_f32_s,
    InstructionType.I32_TRUNC_SAT_F32_U: i32_trunc_sat_f32_u,
    InstructionType.I32_TRUNC_SAT_F64_S: i32_trunc_sat_f64_s,
    InstructionType.I32_TRUNC_SAT_F64_U: i32_trunc_sat_f64_u,
    InstructionType.I64_TRUNC_SAT_F32_S: i64_trunc_sat_f32_s,
    InstructionType.I64_TRUNC_SAT_F32_U: i64_trunc_sat_f32_u,
    InstructionType.I64_TRUNC_SAT_F64_S: i64_trunc_sat_f64_s,
    InstructionType.I64_TRUNC_SAT_F64_U: i64_trunc_sat_f64_u,
    # Vector instructions,
    InstructionType.V128_LOAD: v128_load,
    InstructionType.V128_LOAD8X8_S: v128_load8x8_s,
    InstructionType.V128_LOAD8X8_U: v128_load8x8_u,
    InstructionType.V128_LOAD16X4_S: v128_load16x4_s,
    InstructionType.V128_LOAD16X4_U: v128_load16x4_u,
    InstructionType.V128_LOAD32X2_S: v128_load32x2_s,
    InstructionType.V128_LOAD32X2_U: v128_load32x2_u,
    InstructionType.V128_LOAD8_SPLAT: v128_load8_splat,
    InstructionType.V128_LOAD16_SPLAT: v128_load16_splat,
    InstructionType.V128_LOAD32_SPLAT: v128_load32_splat,
    InstructionType.V128_LOAD64_SPLAT: v128_load64_splat,
    InstructionType.V128_LOAD32_ZERO: v128_load32_zero,
    InstructionType.V128_LOAD64_ZERO: v128_load64_zero,
    InstructionType.V128_STORE: v128_store,
    InstructionType.V128_LOAD8_LANE: v128_load8_lane,
    InstructionType.V128_LOAD16_LANE: v128_load16_lane,
    InstructionType.V128_LOAD32_LANE: v128_load32_lane,
    InstructionType.V128_LOAD64_LANE: v128_load64_lane,
    InstructionType.V128_STORE8_LANE: v128_store8_lane,
    InstructionType.V128_STORE16_LANE: v128_store16_lane,
    InstructionType.V128_STORE32_LANE: v128_store32_lane,
    InstructionType.V128_STORE64_LANE: v128_store64_lane,
    InstructionType.V128_CONST: v128_const,
    InstructionType.I8X16_SHUFFLE: i8x16_shuffle,
    InstructionType.I8X16_EXTRACT_LANE_S: i8x16_extract_lane_s,
    InstructionType.I8X16_EXTRACT_LANE_U: i8x16_extract_lane_u,
    InstructionType.I8X16_REPLACE_LANE: i8x16_replace_lane,
    InstructionType.I16X8_EXTRACT_LANE_S: i16x8_extract_lane_s,
    InstructionType.I16X8_EXTRACT_LANE_U: i16x8_extract_lane_u,
    InstructionType.I16X8_REPLACE_LANE: i16x8_replace_lane,
    InstructionType.I32X4_EXTRACT_LANE: i32x4_extract_lane,
    InstructionType.I32X4_REPLACE_LANE: i32x4_replace_lane,
    InstructionType.I64X2_EXTRACT_LANE: i64x2_extract_lane,
    InstructionType.I64X2_REPLACE_LANE: i64x2_replace_lane,
    InstructionType.F32X4_EXTRACT_LANE: f32x4_extract_lane,
    InstructionType.F32X4_REPLACE_LANE: f32x4_replace_lane,
    InstructionType.F64X2_EXTRACT_LANE: f64x2_extract_lane,
    InstructionType.F64X2_REPLACE_LANE: f64x2_replace_lane,
    InstructionType.I8X16_SWIZZLE: i8x16_swizzle,
    InstructionType.I8X16_SPLAT: i8x16_splat,
    InstructionType.I16X8_SPLAT: i16x8_splat,
    InstructionType.I32X4_SPLAT: i32x4_splat,
    InstructionType.I64X2_SPLAT: i64x2_splat,
    InstructionType.F32X4_SPLAT: f32x4_splat,
    InstructionType.F64X2_SPLAT: f64x2_splat,
    InstructionType.I8X16_EQ: i8x16_eq,
    InstructionType.I8X16_NE: i8x16_ne,
    InstructionType.I8X16_LT_S: i8x16_lt_s,
    InstructionType.I8X16_LT_U: i8x16_lt_u,
    InstructionType.I8X16_GT_S: i8x16_gt_s,
    InstructionType.I8X16_GT_U: i8x16_gt_u,
    InstructionType.I8X16_LE_S: i8x16_le_s,
    InstructionType.I8X16_LE_U: i8x16_le_u,
    InstructionType.I8X16_GE_S: i8x16_ge_s,
    InstructionType.I8X16_GE_U: i8x16_ge_u,
    InstructionType.I16X8_EQ: i16x8_eq,
    InstructionType.I16X8_NE: i16x8_ne,
    InstructionType.I16X8_LT_S: i16x8_lt_s,
    InstructionType.I16X8_LT_U: i16x8_lt_u,
    InstructionType.I16X8_GT_S: i16x8_gt_s,
    InstructionType.I16X8_GT_U: i16x8_gt_u,
    InstructionType.I16X8_LE_S: i16x8_le_s,
    InstructionType.I16X8_LE_U: i16x8_le_u,
    InstructionType.I16X8_GE_S: i16x8_ge_s,
    InstructionType.I16X8_GE_U: i16x8_ge_u,
    InstructionType.I32X4_EQ: i32x4_eq,
    InstructionType.I32X4_NE: i32x4_ne,
    InstructionType.I32X4_LT_S: i32x4_lt_s,
    InstructionType.I32X4_LT_U: i32x4_lt_u,
    InstructionType.I32X4_GT_S: i32x4_gt_s,
    InstructionType.I32X4_GT_U: i32x4_gt_u,
    InstructionType.I32X4_LE_S: i32x4_le_s,
    InstructionType.I32X4_LE_U: i32x4_le_u,
    InstructionType.I32X4_GE_S: i32x4_ge_s,
    InstructionType.I32X4_GE_U: i32x4_ge_u,
    InstructionType.I64X2_EQ: i64x2_eq,
    InstructionType.I64X2_NE: i64x2_ne,
    InstructionType.I64X2_LT_S: i64x2_lt_s,
    InstructionType.I64X2_GT_S: i64x2_gt_s,
    InstructionType.I64X2_LE_S: i64x2_le_s,
    InstructionType.I64X2_GE_S: i64x2_ge_s,
    InstructionType.F32X4_EQ: f32x4_eq,
    InstructionType.F32X4_NE: f32x4_ne,
    InstructionType.F32X4_LT: f32x4_lt,
    InstructionType.F32X4_GT: f32x4_gt,
    InstructionType.F32X4_LE: f32x4_le,
    InstructionType.F32X4_GE: f32x4_ge,
    InstructionType.F64X2_EQ: f64x2_eq,
    InstructionType.F64X2_NE: f64x2_ne,
    InstructionType.F64X2_LT: f64x2_lt,
    InstructionType.F64X2_GT: f64x2_gt,
    InstructionType.F64X2_LE: f64x2_le,
    InstructionType.F64X2_GE: f64x2_ge,
    InstructionType.V128_NOT: v128_not,
    InstructionType.V128_AND: v128_and,
    InstructionType.V128_ANDNOT: v128_andnot,
    InstructionType.V128_OR: v128_or,
    InstructionType.V128_XOR: v128_xor,
    InstructionType.V128_BITSELECT: v128_bitselect,
    InstructionType.V128_ANY_TRUE: v128_any_true,
    InstructionType.I8X16_ABS: i8x16_abs,
    InstructionType.I8X16_NEG: i8x16_neg,
    InstructionType.I8X16_POPCNT: i8x16_popcnt,
    InstructionType.I8X16_ALL_TRUE: i8x16_all_true,
    InstructionType.I8X16_BITMASK: i8x16_bitmask,
    InstructionType.I8X16_NARROW_I16X8_S: i8x16_narrow_i16x8_s,
    InstructionType.I8X16_NARROW_I16X8_U: i8x16_narrow_i16x8_u,
    InstructionType.I8X16_SHL: i8x16_shl,
    InstructionType.I8X16_SHR_S: i8x16_shr_s,
    InstructionType.I8X16_SHR_U: i8x16_shr_u,
    InstructionType.I8X16_ADD: i8x16_add,
    InstructionType.I8X16_ADD_SAT_S: i8x16_add_sat_s,
    InstructionType.I8X16_ADD_SAT_U: i8x16_add_sat_u,
    InstructionType.I8X16_SUB: i8x16_sub,
    InstructionType.I8X16_SUB_SAT_S: i8x16_sub_sat_s,
    InstructionType.I8X16_SUB_SAT_U: i8x16_sub_sat_u,
    InstructionType.I8X16_MIN_S: i8x16_min_s,
    InstructionType.I8X16_MIN_U: i8x16_min_u,
    InstructionType.I8X16_MAX_S: i8x16_max_s,
    InstructionType.I8X16_MAX_U: i8x16_max_u,
    InstructionType.I8X16_AVGR_U: i8x16_avgr_u,
}
