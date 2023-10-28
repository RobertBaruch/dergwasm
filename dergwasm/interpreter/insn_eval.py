"""Evaluates instructions."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=too-many-branches,too-many-statements,too-many-lines
# pylint: disable=unused-argument
# pylint: disable=invalid-name

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import struct
from typing import Callable, Union, cast

from dergwasm.interpreter.insn import Instruction, InstructionType, Block
from dergwasm.interpreter import values
from dergwasm.interpreter.machine import Machine
from dergwasm.interpreter.binary import FuncType

EvalOperands = list[Union[values.ValueType, int, float, Block]]
EvalFunc = Callable[[Machine, EvalOperands], None]
MASK64 = 0xFFFFFFFFFFFFFFFF
MASK32 = 0xFFFFFFFF


def _unsigned_i32(v: values.Value) -> int:
    """Converts a value to an unsigned 32-bit integer.

    Only necessary because Python ints are bignums.
    """
    assert isinstance(v.value, int)
    assert v.value_type == values.ValueType.I32
    return int(cast(values.Value, v).value) & MASK32


def _unsigned_i64(v: values.Value) -> int:
    """Converts a value to an unsigned 64-bit integer.

    Only necessary because Python ints are bignums.
    """
    assert isinstance(v.value, int)
    assert v.value_type == values.ValueType.I64
    return int(cast(values.Value, v).value) & MASK64


def _signed_i32(v: values.Value) -> int:
    """Converts a value to a signed 32-bit integer.

    Only necessary because Python ints are bignums.
    """
    n = _unsigned_i32(v)
    return n - 0x100000000 if n & 0x80000000 else n


def _signed_i64(v: values.Value) -> int:
    """Converts a value to a signed 64-bit integer.

    Only necessary because Python ints are bignums.
    """
    n = _unsigned_i64(v)
    return n - 0x10000000000000000 if n & 0x8000000000000000 else n


# Control instructions
def unreachable(machine: Machine, instruction: Instruction) -> None:
    raise RuntimeError("unreachable instruction reached!")


def nop(machine: Machine, instruction: Instruction) -> None:
    machine.get_current_frame().pc += 1


def _block(machine: Machine, block_operand: Block, continuation_pc: int) -> None:
    f = machine.get_current_frame()
    block_func_type = block_operand.block_type
    if isinstance(block_func_type, int):
        block_func_type = f.module.func_types[block_func_type]
    elif block_func_type is not None:
        block_func_type = FuncType([], [block_func_type])
    else:
        block_func_type = FuncType([], [])

    # Create a label for the block. Its continuation is the end of the
    # block.
    label = values.Label(len(block_func_type.results), continuation_pc)
    # Slide the label under the params.
    block_vals = [machine.pop() for _ in block_func_type.parameters]
    machine.push(label)
    for v in reversed(block_vals):
        machine.push(v)


def block(machine: Machine, instruction: Instruction) -> None:
    operands = instruction.operands
    block_operand: Block = operands[0]
    _block(machine, block_operand, instruction.continuation_pc)
    machine.get_current_frame().pc += 1


def loop(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    block_operand: Block = instruction.operands[0]
    block_func_type = block_operand.block_type
    if isinstance(block_func_type, int):
        block_func_type = f.module.func_types[block_func_type]
    elif block_func_type is not None:
        block_func_type = FuncType([], [block_func_type])
    else:
        block_func_type = FuncType([], [])

    label = values.Label(len(block_func_type.parameters), instruction.continuation_pc)
    # Slide the label under the params.
    block_vals = [machine.pop() for _ in block_func_type.parameters]
    machine.push(label)
    for v in reversed(block_vals):
        machine.push(v)
    machine.get_current_frame().pc += 1


def if_(machine: Machine, instruction: Instruction) -> None:
    assert isinstance(instruction.operands[0], Block)
    block_operand: Block = instruction.operands[0]
    cond: values.Value = machine.pop()
    if cond.value:
        _block(machine, block_operand, instruction.continuation_pc)
        machine.get_current_frame().pc += 1
    else:
        # If there's no else clause, don't start a block.
        if instruction.else_continuation_pc != instruction.continuation_pc:
            _block(machine, block_operand, instruction.continuation_pc)
        machine.get_current_frame().pc = instruction.else_continuation_pc


def else_(machine: Machine, instruction: Instruction) -> None:
    # End of block reached without jump. Slide the first label out of the
    # stack, and then jump to after its end.
    stack_values = []
    value = machine.pop()
    while not isinstance(value, values.Label):
        stack_values.append(value)
        value = machine.pop()

    label = value
    # Push the vals back on the stack
    while stack_values:
        machine.push(stack_values.pop())
    machine.get_current_frame().pc = label.continuation


def end(machine: Machine, instruction: Instruction) -> None:
    # End of block reached without jump. Slide the first label out of the
    # stack, and then jump to after its end.
    stack_values = []
    value = machine.pop()
    while not isinstance(value, values.Label):
        stack_values.append(value)
        value = machine.pop()
    # Push the vals back on the stack
    while stack_values:
        machine.push(stack_values.pop())
    machine.get_current_frame().pc += 1


def _br(machine: Machine, level: int) -> None:
    label: values.Label = machine.get_nth_value_of_type(level, values.Label)
    n = label.arity

    # save the top n values on the stack
    vals = [machine.pop() for _ in range(n)]
    # pop everything up to the label
    while level >= 0:
        value = machine.pop()
        if isinstance(value, values.Label):
            level -= 1
    # push the saved values back on the stack
    for v in reversed(vals):
        machine.push(v)
    machine.get_current_frame().pc = label.continuation


def br(machine: Machine, instruction: Instruction) -> None:
    # Branch out of a nested block. The sole exception is BR 0 when not
    # in a block. That is the equivalent of a return.
    # Find the level-th label (0-based) on the stack.
    assert isinstance(instruction.operands[0], int)
    level = instruction.operands[0]
    _br(machine, level)


def br_if(machine: Machine, instruction: Instruction) -> None:
    assert isinstance(instruction.operands[0], int)
    level = instruction.operands[0]
    cond: values.Value = machine.pop()
    if cond.value:
        _br(machine, level)
        return
    machine.get_current_frame().pc += 1


def br_table(machine: Machine, instruction: Instruction) -> None:
    operands = instruction.operands
    idx_value: values.Value = machine.pop()
    assert isinstance(idx_value.value, int)
    idx = idx_value.value
    if idx < len(operands):
        _br(machine, operands[idx])
        return
    _br(machine, operands[-1])


def return_(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    n = f.arity
    results = [machine.pop() for _ in range(n)]
    # Pop everything up to and including the frame. This will also include
    # the function's label. Basically skip all nesting levels.
    machine.pop_to_frame()
    # Push the results back on the stack
    for v in reversed(results):
        machine.push(v)


def call(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    f.pc += 1
    operands = instruction.operands
    a = f.module.funcaddrs[operands[0]]
    machine.invoke_func(a)


def call_indirect(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


# Reference instructions,
def ref_null(machine: Machine, instruction: Instruction) -> None:
    """A null reference to the type given by the operand."""
    assert isinstance(instruction.operands[0], int)  # encoded ref type
    assert int(instruction.operands[0]) in (0x70, 0x6F)
    machine.push(values.Value(values.ValueType(int(instruction.operands[0])), None))
    machine.get_current_frame().pc += 1


def ref_is_null(machine: Machine, instruction: Instruction) -> None:
    val = machine.pop()
    assert isinstance(val, values.Value)
    assert val.value_type in (values.ValueType.FUNCREF, values.ValueType.EXTERNREF)
    machine.push(values.Value(values.ValueType.I32, 1 if val.value is None else 0))
    machine.get_current_frame().pc += 1


def ref_func(machine: Machine, instruction: Instruction) -> None:
    """A function reference to the store's funcaddr for the module's funcidx given by
    the operand."""
    assert isinstance(instruction.operands[0], int)  # funcidx
    f = machine.get_current_frame()
    val = f.module.funcaddrs[int(instruction.operands[0])]
    machine.push(values.Value(values.ValueType.FUNCREF, val))
    machine.get_current_frame().pc += 1


# Parametric instructions,
def drop(machine: Machine, instruction: Instruction) -> None:
    machine.pop()
    machine.get_current_frame().pc += 1


def select(machine: Machine, instruction: Instruction) -> None:
    c = _unsigned_i32(machine.pop())
    val2 = cast(values.Value, machine.pop())
    val1 = cast(values.Value, machine.pop())
    machine.push(val1 if c else val2)
    machine.get_current_frame().pc += 1


def select_vec(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


# Variable instructions,
def local_get(machine: Machine, instruction: Instruction) -> None:
    """Gets a local variable from the current frame.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    machine.push(f.local_vars[operands[0]])
    f.pc += 1


def local_set(machine: Machine, instruction: Instruction) -> None:
    """Sets a local variable in the current frame.

    Expects the value to set to be on the top of the stack.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    f.local_vars[operands[0]] = machine.pop()
    f.pc += 1


def local_tee(machine: Machine, instruction: Instruction) -> None:
    """Sets a local variable in the current frame, but doesn't consume stack.

    Expects the value to set to be on the top of the stack.

    Operands:
      0: local index (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    f.local_vars[operands[0]] = machine.peek()
    f.pc += 1


def global_get(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    operands = instruction.operands
    machine.push(machine.get_global(f.module.globaladdrs[operands[0]]))
    f.pc += 1


def global_set(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    operands = instruction.operands
    val = machine.pop()
    machine.set_global(f.module.globaladdrs[operands[0]], val)
    f.pc += 1


# Table instructions,
def table_get(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_set(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_init(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def elem_drop(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_copy(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_grow(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_size(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def table_fill(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


# Memory instructions,
def i32_load(machine: Machine, instruction: Instruction) -> None:
    """Loads a 32-bit integer from memory.

    Expects the base address (i32) to be on the top of the stack.

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    i = _unsigned_i32(machine.pop())  # base
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"i32.load: access out of bounds: base {i} offset {operands[1]}"
        )
    val = struct.unpack("<I", mem[ea : ea + 4])[0]
    machine.push(values.Value(values.ValueType.I32, val))
    f.pc += 1


def i64_load(machine: Machine, instruction: Instruction) -> None:
    """Loads a 64-bit integer from memory.

    Expects the base address (i32) to be on the top of the stack.

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    i = _unsigned_i32(machine.pop())  # base
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)
    if ea + 8 > len(mem):
        raise RuntimeError(
            f"i64.load: access out of bounds: base {i} offset {operands[1]}"
        )
    val = struct.unpack("<Q", mem[ea : ea + 8])[0]
    machine.push(values.Value(values.ValueType.I64, val))
    f.pc += 1


def f32_load(machine: Machine, instruction: Instruction) -> None:
    """Loads a 32-bit float from memory.

    Expects the base address (i32) to be on the top of the stack.

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    i = _unsigned_i32(machine.pop())  # base
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"f32.load: access out of bounds: base {i} offset {operands[1]}"
        )
    val = struct.unpack("<f", mem[ea : ea + 4])[0]
    machine.push(values.Value(values.ValueType.F32, val))
    f.pc += 1


def f64_load(machine: Machine, instruction: Instruction) -> None:
    """Loads a 64-bit float from memory.

    Expects the base address (i32) to be on the top of the stack.

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    i = _unsigned_i32(machine.pop())  # base
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)
    if ea + 8 > len(mem):
        raise RuntimeError(
            f"f32.load: access out of bounds: base {i} offset {operands[1]}"
        )
    val = struct.unpack("<d", mem[ea : ea + 8])[0]
    machine.push(values.Value(values.ValueType.F64, val))
    f.pc += 1


def _isz_loadN_sx(
    machine: Machine, instruction: Instruction, sz: int, n: int, sx: bool
) -> None:
    f = machine.get_current_frame()
    operands = instruction.operands
    i = _unsigned_i32(machine.pop())  # base
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # memaddr
    ea = i + int(operands[1])  # effective address
    mem = machine.get_mem(a)

    numbytes = n // 8
    if ea + numbytes > len(mem):
        raise RuntimeError(
            f"i32.load{n}_{'s' if sx else 'u'}: access out of bounds: "
            f"base {i} offset {operands[1]}"
        )

    # Python things
    val = bytearray(sz // 8)
    # Put the memory bytes into val, left-justified (because little-endian).
    #
    # Examples:
    #
    #  sz = 32, n = 8: 12 -> 12 00 00 00
    #  sz = 32, n = 16: 12 34 -> 12 34 00 00
    #  sz = 64, n = 16: 12 34 -> 12 34 00 00 00 00 00 00
    val[:numbytes] = mem[ea : ea + numbytes]
    if sx and (val[numbytes - 1] & 0x80):
        # Sign extend.
        #
        # Examples:
        #
        #  sz = 32, n = 8: 84 -> 84 FF FF FF
        #  sz = 32, n = 16: 12 82 -> 12 82 FF FF
        #  sz = 64, n = 16: 12 82 -> 12 82 FF FF FF FF FF FF
        val[numbytes:] = b"\xFF" * ((sz // 8) - numbytes)

    if sz == 32:
        machine.push(values.Value(values.ValueType.I32, struct.unpack("<I", val)[0]))
    else:
        machine.push(values.Value(values.ValueType.I64, struct.unpack("<Q", val)[0]))
    f.pc += 1


def i32_load8_s(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 32, 8, True)


def i32_load8_u(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 32, 8, False)


def i32_load16_s(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 32, 16, True)


def i32_load16_u(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 32, 16, False)


def i64_load8_s(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 8, True)


def i64_load8_u(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 8, False)


def i64_load16_s(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 16, True)


def i64_load16_u(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 16, False)


def i64_load32_s(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 32, True)


def i64_load32_u(machine: Machine, instruction: Instruction) -> None:
    _isz_loadN_sx(machine, instruction, 64, 32, False)


def i32_store(machine: Machine, instruction: Instruction) -> None:
    """Stores a 32-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i32), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i32(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"i32.store: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 4] = struct.pack("<I", c)
    f.pc += 1


def i64_store(machine: Machine, instruction: Instruction) -> None:
    """Stores a 64-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i64), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i64(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 8 > len(mem):
        raise RuntimeError(
            f"i64.store: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 8] = struct.pack("<Q", c)
    f.pc += 1


def f32_store(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = float(cast(values.Value, machine.pop()).value)  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"f32.store: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 4] = struct.pack("<f", c)
    f.pc += 1


def f64_store(machine: Machine, instruction: Instruction) -> None:
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = float(cast(values.Value, machine.pop()).value)  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 8 > len(mem):
        raise RuntimeError(
            f"f64.store: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 8] = struct.pack("<d", c)
    f.pc += 1


def i32_store8(machine: Machine, instruction: Instruction) -> None:
    """Stores the low 8 bits of a 32-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i32), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i32(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 1 > len(mem):
        raise RuntimeError(
            f"i32.store8: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea] = c & 0xFF
    f.pc += 1


def i32_store16(machine: Machine, instruction: Instruction) -> None:
    """Stores the low 16 bits of a 32-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i32), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i32(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 2 > len(mem):
        raise RuntimeError(
            f"i32.store16: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 2] = struct.pack("<H", c & 0xFFFF)
    f.pc += 1


def i64_store8(machine: Machine, instruction: Instruction) -> None:
    """Stores the low 8 bits of a 64-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i64), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i64(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 1 > len(mem):
        raise RuntimeError(
            f"i64.store8: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea] = c & 0xFF
    f.pc += 1


def i64_store16(machine: Machine, instruction: Instruction) -> None:
    """Stores the low 16 bits of a 64-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i64), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i64(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 2 > len(mem):
        raise RuntimeError(
            f"i64.store16: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 2] = struct.pack("<H", c & 0xFFFF)
    f.pc += 1


def i64_store32(machine: Machine, instruction: Instruction) -> None:
    """Stores the low 32 bits of a 64-bit integer to memory.

    Expects the stack to contain, starting from top: [value (i64), base addr (i32)].

    Operands:
      0: alignment (int)
      1: offset (int)
    """
    f = machine.get_current_frame()
    operands = instruction.operands
    # Ignore operand[0], the alignment.
    a = f.module.memaddrs[0]  # offset
    mem = machine.get_mem(a)
    c = _unsigned_i64(machine.pop())  # value
    i = _unsigned_i32(machine.pop())  # base
    ea = i + int(operands[1])  # effective address
    if ea + 4 > len(mem):
        raise RuntimeError(
            f"i64.store32: access out of bounds: base {i} offset {operands[1]}"
        )
    mem[ea : ea + 4] = struct.pack("<I", c & MASK32)
    f.pc += 1


def memory_size(machine: Machine, instruction: Instruction) -> None:
    assert isinstance(instruction.operands[0], int)  # memindex = 0
    f = machine.get_current_frame()
    ma = f.module.memaddrs[instruction.operands[0]]
    mem = machine.get_mem(ma)
    machine.push(values.Value(values.ValueType.I32, len(mem) // 65536))
    f.pc += 1


def memory_grow(machine: Machine, instruction: Instruction) -> None:
    assert isinstance(instruction.operands[0], int)  # memindex = 0
    f = machine.get_current_frame()
    ma = f.module.memaddrs[instruction.operands[0]]
    mem = machine.get_mem(ma)
    sz = len(mem) // 65536
    n = _unsigned_i32(machine.pop())  # page growth amount
    if sz + n > machine.get_max_allowed_memory_pages():
        machine.push(values.Value(values.ValueType.I32, 0xFFFFFFFF))
        f.pc += 1
        return
    mem.extend(bytearray(n * 65536))
    machine.push(values.Value(values.ValueType.I32, sz))
    f.pc += 1


def memory_init(machine: Machine, instruction: Instruction) -> None:
    """Initializes a memory segment."""
    operands = instruction.operands
    assert len(operands) == 2
    assert isinstance(operands[0], int)  # dataindex
    assert isinstance(operands[1], int)  # memindex = 0

    curr_frame = machine.get_current_frame()
    ma = curr_frame.module.memaddrs[operands[1]]
    mem = machine.get_mem(ma)
    da = curr_frame.module.dataaddrs[operands[0]]
    data = machine.get_data(da)
    n = _unsigned_i32(machine.pop())  # data size, i32
    s = _unsigned_i32(machine.pop())  # source, i32
    d = _unsigned_i32(machine.pop())  # destination, i32

    if s + n > len(data):
        raise RuntimeError("memory.init: source is out of bounds")
    if d + n > len(mem):
        raise RuntimeError("memory.init: destination is out of bounds")
    if n > 0:
        mem[d : d + n] = data[s : s + n]
    machine.get_current_frame().pc += 1


def data_drop(machine: Machine, instruction: Instruction) -> None:
    """Drops a data segment."""
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], int)  # dataindex

    da = machine.get_current_frame().module.dataaddrs[operands[0]]
    machine.datas[da] = bytearray()
    machine.get_current_frame().pc += 1


def memory_copy(machine: Machine, instruction: Instruction) -> None:
    operands = instruction.operands
    assert len(operands) == 2
    assert isinstance(operands[0], int)  # memindex
    assert isinstance(operands[1], int)  # memindex
    n = _unsigned_i32(machine.pop())  # data size, i32
    s = _unsigned_i32(machine.pop())  # source, i32
    d = _unsigned_i32(machine.pop())  # destination, i32
    curr_frame = machine.get_current_frame()
    # Note: wasm doesn't yet support more than one memory, so it expects all
    # memindexes to be 0. I'm not quite sure which operand represents the source and
    # which the destination. But for now, they are both equal to eqch other and equal
    # to 0.
    ma = curr_frame.module.memaddrs[operands[1]]
    mem = machine.get_mem(ma)
    if s + n > len(mem):
        raise RuntimeError("memory.copy: source is out of bounds")
    if d + n > len(mem):
        raise RuntimeError("memory.copy: destination is out of bounds")
    if n > 0:
        mem[d : d + n] = mem[s : s + n]
    machine.get_current_frame().pc += 1


def memory_fill(machine: Machine, instruction: Instruction) -> None:
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], int)  # memindex
    n = _unsigned_i32(machine.pop())  # data size, i32
    v = _unsigned_i32(machine.pop())  # value, i32
    d = _unsigned_i32(machine.pop())  # destination, i32
    curr_frame = machine.get_current_frame()
    ma = curr_frame.module.memaddrs[operands[0]]
    mem = machine.get_mem(ma)
    if d + n > len(mem):
        raise RuntimeError("memory.fill: destination is out of bounds")
    if n > 0:
        mem[d : d + n] = bytearray([v & 0xFF] * n)
    machine.get_current_frame().pc += 1


# Numeric instructions,
def i32_const(machine: Machine, instruction: Instruction) -> None:
    """Pushes a 32-bit integer constant onto the stack."""
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], int)
    machine.push(values.Value(values.ValueType.I32, operands[0] & MASK32))
    machine.get_current_frame().pc += 1


def i64_const(machine: Machine, instruction: Instruction) -> None:
    """Pushes a 64-bit integer constant onto the stack."""
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], int)
    machine.push(values.Value(values.ValueType.I64, operands[0] & MASK64))
    machine.get_current_frame().pc += 1


def f32_const(machine: Machine, instruction: Instruction) -> None:
    """Pushes a 32-bit float constant onto the stack."""
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], float)
    # Necessary because python floats are 64-bit, but wasm F32s are 32-bit.
    val32 = struct.unpack("f", struct.pack("f", operands[0]))[0]
    machine.push(values.Value(values.ValueType.F32, val32))
    machine.get_current_frame().pc += 1


def f64_const(machine: Machine, instruction: Instruction) -> None:
    """Pushes a 64-bit float constant onto the stack."""
    operands = instruction.operands
    assert len(operands) == 1
    assert isinstance(operands[0], float)
    machine.push(values.Value(values.ValueType.F64, operands[0]))
    machine.get_current_frame().pc += 1


def i32_eqz(machine: Machine, instruction: Instruction) -> None:
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, 0 if c1 else 1))
    machine.get_current_frame().pc += 1


def i32_eq(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 == c2)))
    machine.get_current_frame().pc += 1


def i32_ne(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 != c2)))
    machine.get_current_frame().pc += 1


def i32_lt_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 < c2)))
    machine.get_current_frame().pc += 1


def i32_lt_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 < c2)))
    machine.get_current_frame().pc += 1


def i32_gt_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 > c2)))
    machine.get_current_frame().pc += 1


def i32_gt_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 > c2)))
    machine.get_current_frame().pc += 1


def i32_le_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 <= c2)))
    machine.get_current_frame().pc += 1


def i32_le_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 <= c2)))
    machine.get_current_frame().pc += 1


def i32_ge_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 >= c2)))
    machine.get_current_frame().pc += 1


def i32_ge_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, int(c1 >= c2)))
    machine.get_current_frame().pc += 1


def i64_eqz(machine: Machine, instruction: Instruction) -> None:
    c1 = int(cast(values.Value, machine.pop()).value)
    machine.push(values.Value(values.ValueType.I64, 0 if c1 else 1))
    machine.get_current_frame().pc += 1


def i64_eq(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 == c2)))
    machine.get_current_frame().pc += 1


def i64_ne(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 != c2)))
    machine.get_current_frame().pc += 1


def i64_lt_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 < c2)))
    machine.get_current_frame().pc += 1


def i64_lt_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 < c2)))
    machine.get_current_frame().pc += 1


def i64_gt_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 > c2)))
    machine.get_current_frame().pc += 1


def i64_gt_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 > c2)))
    machine.get_current_frame().pc += 1


def i64_le_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 <= c2)))
    machine.get_current_frame().pc += 1


def i64_le_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 <= c2)))
    machine.get_current_frame().pc += 1


def i64_ge_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 >= c2)))
    machine.get_current_frame().pc += 1


def i64_ge_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, int(c1 >= c2)))
    machine.get_current_frame().pc += 1


def f32_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_lt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_gt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_le(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_ge(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_lt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_gt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_le(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_ge(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_clz(machine: Machine, instruction: Instruction) -> None:
    """Count leading zero bits."""
    c1 = _unsigned_i32(machine.pop())
    val = 32
    for i in range(32):
        if c1 & 0x80000000:
            val = i
            break
        c1 <<= 1
    machine.push(values.Value(values.ValueType.I32, val))
    machine.get_current_frame().pc += 1


def i32_ctz(machine: Machine, instruction: Instruction) -> None:
    """Count trailing zero bits."""
    c1 = _unsigned_i32(machine.pop())
    val = 32
    for i in range(32):
        if c1 & 1:
            val = i
            break
        c1 >>= 1
    machine.push(values.Value(values.ValueType.I32, val))
    machine.get_current_frame().pc += 1


def i32_popcnt(machine: Machine, instruction: Instruction) -> None:
    """Count bits set."""
    c1 = _unsigned_i32(machine.pop())
    val = 0
    for i in range(32):
        if c1 & (1 << i):
            val += 1
    machine.push(values.Value(values.ValueType.I32, val))
    machine.get_current_frame().pc += 1


# All this bitmasking is necessary because Python ints are always signed
# and always bignums.
def i32_add(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 + c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_sub(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 - c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_mul(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 * c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_div_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    if c2 == 0:
        raise RuntimeError("i32.div_s: division by zero")
    if c1 // c2 == 0x80000000:  # Unrepresentable: -2^31 // -1
        raise RuntimeError("i32.div_s: overflow")
    machine.push(values.Value(values.ValueType.I32, (c1 // c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_div_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    if c2 == 0:
        raise RuntimeError("i32.div_u: division by zero")
    machine.push(values.Value(values.ValueType.I32, (c1 // c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_rem_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    if c2 == 0:
        raise RuntimeError("i32.rem_s: modulo zero")

    # Note: Python % is not consistent with most languages and not consistent with wasm.
    # See https://torstencurdt.com/tech/posts/modulo-of-negative-numbers.
    # Thus, we implement the "correct" version here. C# does it correctly.

    if c1 < 0 and c2 > 0:
        val = -((-c1) % c2)
    elif c1 > 0 and c2 < 0:
        val = c1 % (-c2)
    elif c1 < 0 and c2 < 0:
        val = -((-c1) % (-c2))
    else:
        val = c1 % c2
    machine.push(values.Value(values.ValueType.I32, val & MASK32))
    machine.get_current_frame().pc += 1


def i32_rem_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    if c2 == 0:
        raise RuntimeError("i32.rem_u: modulo zero")
    machine.push(values.Value(values.ValueType.I32, (c1 % c2) & MASK32))
    machine.get_current_frame().pc += 1


def i32_and(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 & c2)))
    machine.get_current_frame().pc += 1


def i32_or(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 | c2)))
    machine.get_current_frame().pc += 1


def i32_xor(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 ^ c2)))
    machine.get_current_frame().pc += 1


def i32_shl(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 << (c2 % 32)) & MASK32))
    machine.get_current_frame().pc += 1


def i32_shr_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _signed_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 >> (c2 % 32)) & MASK32))
    machine.get_current_frame().pc += 1


def i32_shr_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    machine.push(values.Value(values.ValueType.I32, (c1 >> (c2 % 32)) & MASK32))
    machine.get_current_frame().pc += 1


def i32_rotl(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    c2 %= 32
    val = ((c1 << c2) | (c1 >> (32 - c2))) & MASK32
    machine.push(values.Value(values.ValueType.I32, val))
    machine.get_current_frame().pc += 1


def i32_rotr(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i32(machine.pop())
    c1 = _unsigned_i32(machine.pop())
    c2 %= 32
    val = (c1 >> c2) | (c1 << (32 - c2)) & MASK32
    machine.push(values.Value(values.ValueType.I32, val))
    machine.get_current_frame().pc += 1


def i64_clz(machine: Machine, instruction: Instruction) -> None:
    """Count leading zero bits."""
    c1 = _unsigned_i64(machine.pop())
    val = 64
    for i in range(64):
        if c1 & 0x8000000000000000:
            val = i
            break
        c1 <<= 1
    machine.push(values.Value(values.ValueType.I64, val))
    machine.get_current_frame().pc += 1


def i64_ctz(machine: Machine, instruction: Instruction) -> None:
    """Count trailing zero bits."""
    c1 = _unsigned_i64(machine.pop())
    val = 64
    for i in range(64):
        if c1 & 1:
            val = i
            break
        c1 >>= 1
    machine.push(values.Value(values.ValueType.I64, val))
    machine.get_current_frame().pc += 1


def i64_popcnt(machine: Machine, instruction: Instruction) -> None:
    """Count bits set."""
    c1 = _unsigned_i64(machine.pop())
    val = 0
    for i in range(64):
        if c1 & (1 << i):
            val += 1
    machine.push(values.Value(values.ValueType.I64, val))
    machine.get_current_frame().pc += 1


# All this bitmasking is necessary because Python ints are always signed
# and always bignums.
def i64_add(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 + c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_sub(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 - c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_mul(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 * c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_div_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    if c2 == 0:
        raise RuntimeError("i64.div_s: division by zero")
    if c1 // c2 == 0x8000000000000000:  # Unrepresentable: -2^63 // -1
        raise RuntimeError("i64.div_s: overflow")
    machine.push(values.Value(values.ValueType.I64, (c1 // c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_div_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    if c2 == 0:
        raise RuntimeError("i64.div_u: division by zero")
    machine.push(values.Value(values.ValueType.I64, (c1 // c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_rem_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _signed_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    if c2 == 0:
        raise RuntimeError("i64.rem_s: modulo zero")

    # Note: Python % is not consistent with most languages and not consistent with wasm.
    # See https://torstencurdt.com/tech/posts/modulo-of-negative-numbers.
    # Thus, we implement the "correct" version here. C# does it correctly.

    if c1 < 0 and c2 > 0:
        val = -((-c1) % c2)
    elif c1 > 0 and c2 < 0:
        val = c1 % (-c2)
    elif c1 < 0 and c2 < 0:
        val = -((-c1) % (-c2))
    else:
        val = c1 % c2
    machine.push(values.Value(values.ValueType.I64, val & MASK64))
    machine.get_current_frame().pc += 1


def i64_rem_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    if c2 == 0:
        raise RuntimeError("i64.rem_u: modulo zero")
    machine.push(values.Value(values.ValueType.I64, (c1 % c2) & MASK64))
    machine.get_current_frame().pc += 1


def i64_and(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 & c2)))
    machine.get_current_frame().pc += 1


def i64_or(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 | c2)))
    machine.get_current_frame().pc += 1


def i64_xor(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 ^ c2)))
    machine.get_current_frame().pc += 1


def i64_shl(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 << (c2 % 64)) & MASK64))
    machine.get_current_frame().pc += 1


def i64_shr_s(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _signed_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 >> (c2 % 64)) & MASK64))
    machine.get_current_frame().pc += 1


def i64_shr_u(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    machine.push(values.Value(values.ValueType.I64, (c1 >> (c2 % 64)) & MASK64))
    machine.get_current_frame().pc += 1


def i64_rotl(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    c2 %= 64
    val = ((c1 << c2) | (c1 >> (64 - c2))) & MASK64
    machine.push(values.Value(values.ValueType.I64, val))
    machine.get_current_frame().pc += 1


def i64_rotr(machine: Machine, instruction: Instruction) -> None:
    c2 = _unsigned_i64(machine.pop())
    c1 = _unsigned_i64(machine.pop())
    c2 %= 64
    val = (c1 >> c2) | (c1 << (64 - c2)) & MASK64
    machine.push(values.Value(values.ValueType.I64, val))
    machine.get_current_frame().pc += 1


def f32_abs(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_neg(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_ceil(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_floor(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_trunc(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_nearest(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_sqrt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_add(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_sub(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_mul(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_div(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_min(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_max(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_copysign(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_abs(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_neg(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_ceil(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_floor(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_trunc(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_nearest(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_sqrt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_add(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_sub(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_mul(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_div(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_min(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_max(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_copysign(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_i32_wrap(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_f32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_f32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_f64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_f64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_extend_i32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_extend_i32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_f32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_f32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_f64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_f64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_convert_i32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_convert_i32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_convert_i64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_convert_i64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_demote_f64(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_convert_i32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_convert_i32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_convert_i64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_convert_i64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_f64_promote(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_i32_reinterpret(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_i64_reinterpret(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32_reinterpret_i32(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64_reinterpret_i64(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_extend8_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_extend16_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_extend8_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_extend16_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_extend32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_sat_f32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_sat_f32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_sat_f64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32_trunc_sat_f64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_sat_f32_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_sat_f32_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_sat_f64_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64_trunc_sat_f64_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


# Vector instructions,
def v128_load(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load8x8_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load8x8_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load16x4_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load16x4_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load32x2_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load32x2_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load8_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load16_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load32_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load64_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load32_zero(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load64_zero(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_store(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load8_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load16_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load32_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_load64_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_store8_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_store16_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_store32_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_store64_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_const(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_shuffle(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_extract_lane_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_extract_lane_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_extract_lane_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_extract_lane_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_extract_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_extract_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_extract_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_extract_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_replace_lane(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_swizzle(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_splat(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_lt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_lt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_gt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_gt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_le_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_le_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_ge_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_ge_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_lt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_lt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_gt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_gt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_le_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_le_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_ge_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i16x8_ge_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_lt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_lt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_gt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_gt_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_le_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_le_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_ge_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i32x4_ge_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_lt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_gt_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_le_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i64x2_ge_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_lt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_gt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_le(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f32x4_ge(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_eq(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_ne(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_lt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_gt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_le(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def f64x2_ge(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_not(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_and(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_andnot(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_or(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_xor(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_bitselect(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def v128_any_true(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_abs(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_neg(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_popcnt(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_all_true(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_bitmask(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_narrow_i16x8_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_narrow_i16x8_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_shl(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_shr_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_shr_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_add(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_add_sat_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_add_sat_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_sub(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_sub_sat_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_sub_sat_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_min_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_min_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_max_s(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_max_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def i8x16_avgr_u(machine: Machine, instruction: Instruction) -> None:
    raise NotImplementedError


def eval_insn(machine: Machine, instruction: Instruction) -> None:
    """Evaluates an instruction."""
    try:
        # print(f"{instruction} {instruction.operands}")
        INSTRUCTION_FUNCS[instruction.instruction_type](machine, instruction)
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
    InstructionType.I32_STORE8: i32_store8,
    InstructionType.I32_STORE16: i32_store16,
    InstructionType.I32_STORE: i32_store,
    InstructionType.I64_STORE8: i64_store8,
    InstructionType.I64_STORE16: i64_store16,
    InstructionType.I64_STORE32: i64_store32,
    InstructionType.I64_STORE: i64_store,
    InstructionType.F32_STORE: f32_store,
    InstructionType.F64_STORE: f64_store,
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
