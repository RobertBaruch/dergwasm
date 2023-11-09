"""Unit tests for insn_eval.py."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=too-many-lines,too-many-public-methods
# pylint: disable=invalid-name

import struct
from absl.testing import absltest, parameterized  # type: ignore

from dergwasm.interpreter.binary import (
    FuncType,
    GlobalType,
    MemType,
    Module,
    TableType,
    flatten_instructions,
)
from dergwasm.interpreter.machine import (
    ElementSegmentInstance,
    GlobalInstance,
    MemInstance,
    TableInstance,
)
from dergwasm.interpreter import machine_impl
from dergwasm.interpreter import module_instance
from dergwasm.interpreter import insn_eval
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter.values import Label, Limits, Value, Frame, ValueType
from dergwasm.interpreter.machine import ModuleFuncInstance
from dergwasm.interpreter.testing.util import (
    call,
    data_drop,
    global_get,
    global_set,
    i32_const,
    i64_const,
    f32_const,
    f64_const,
    if_else_void,
    if_else_i32,
    if_void,
    memory_grow,
    memory_init,
    memory_copy,
    memory_fill,
    memory_size,
    br,
    br_table,
    br_if,
    i32_block,
    i32_loop,
    if_,
    local_get,
    local_set,
    local_tee,
    nop,
    op1,
    void_block,
    noarg,
    op2,
)

MASK64 = 0xFFFFFFFFFFFFFFFF
MASK32 = 0xFFFFFFFF


class InsnEvalTest(parameterized.TestCase):
    machine: machine_impl.MachineImpl
    module: Module
    module_inst: module_instance.ModuleInstance
    starting_stack_depth: int

    def _add_i32_func(self, *instructions: Instruction) -> int:
        """Add an i32 function to the machine that takes no arguments.

        There is also one i32 local var.

        Returns the funcaddr.
        """
        func = ModuleFuncInstance(
            FuncType([], [ValueType.I32]),
            self.module_inst,
            [ValueType.I32],
            list(instructions),
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.module_inst.funcaddrs.append(funcaddr)
        return funcaddr

    def _add_i32_i32_func(self, *instructions: Instruction) -> int:
        """Add an i32 function to the machine that takes an i32 argument.

        There is also one i32 local var.

        Returns the funcaddr.
        """
        func = ModuleFuncInstance(
            FuncType([ValueType.I32], [ValueType.I32]),
            self.module_inst,
            [ValueType.I32],
            list(instructions),
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.module_inst.funcaddrs.append(funcaddr)
        return funcaddr

    def _add_i32_i32_i32_func(self, *instructions: Instruction) -> int:
        """Add an i32 function to the machine that takes twp i32 arguments.

        There is also one i32 local var.

        Returns the funcaddr.
        """
        func = ModuleFuncInstance(
            FuncType([ValueType.I32, ValueType.I32], [ValueType.I32]),
            self.module_inst,
            [ValueType.I32],
            list(instructions),
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.module_inst.funcaddrs.append(funcaddr)
        return funcaddr

    def add_func_type(self, args: list[ValueType], returns: list[ValueType]) -> int:
        self.module_inst.func_types.append(FuncType(args, returns))
        return len(self.module_inst.func_types) - 1

    def _stack_depth(self) -> int:
        return self.machine.stack_.depth()

    def assertStackDepth(self, expected: int) -> None:
        if self._stack_depth() == expected:
            return
        stack_debug = ["---stack:"]
        for i, v in enumerate(reversed(self.machine.stack_.data)):
            stack_debug.append(f"[{i}]: {v}")
        stack_lines = "\n".join(stack_debug)
        self.fail(
            f"Expected stack depth {expected}, got {self._stack_depth()}\n{stack_lines}"
        )

    def setUp(self):
        self.machine = machine_impl.MachineImpl()
        self.module = Module()
        self.module_inst = module_instance.ModuleInstance(self.module)
        self.machine.add_mem(MemInstance(MemType(Limits(1, 10)), bytearray(65536)))
        self.module_inst.memaddrs = [0]
        self.machine.new_frame(Frame(0, [], self.module_inst, -1))
        self.starting_stack_depth = self._stack_depth()

    def test_nop(self):
        insn_eval.eval_insn(self.machine, noarg(InstructionType.NOP))
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_i32_const(self):
        insn_eval.eval_insn(self.machine, i32_const(42))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 42))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_i64_const(self):
        insn_eval.eval_insn(self.machine, i64_const(42))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I64, 42))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_f32_const(self):
        insn_eval.eval_insn(self.machine, f32_const(4.2))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(
            self.machine.pop(),
            Value(ValueType.F32, struct.unpack("f", struct.pack("f", 4.2))[0]),
        )
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_f64_const(self):
        insn_eval.eval_insn(self.machine, f64_const(4.2))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.F64, 4.2))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("add", InstructionType.I32_ADD, 2, 1, 3),
        ("add mods", InstructionType.I32_ADD, 0xFEDC0000, 0x56780000, 0x55540000),
        ("mul", InstructionType.I32_MUL, 2, 3, 6),
        ("mul mods", InstructionType.I32_MUL, 0xFEDC1234, 0x56789ABC, 0x8CF0A630),
        ("sub", InstructionType.I32_SUB, 2, 1, 1),
        ("sub mods", InstructionType.I32_SUB, 0x00001234, 0x56789ABC, 0xA9877778),
        ("div_u", InstructionType.I32_DIV_U, 6, 2, 3),
        ("div_u 99/100", InstructionType.I32_DIV_U, 99, 100, 0),
        ("div_u 101/100", InstructionType.I32_DIV_U, 101, 100, 1),
        ("rem_u", InstructionType.I32_REM_U, 6, 4, 2),
        ("rem_u 99%100", InstructionType.I32_REM_U, 99, 100, 99),
        ("rem_u 101%100", InstructionType.I32_REM_U, 101, 100, 1),
        ("div_s 6/2", InstructionType.I32_DIV_S, 6, 2, 3),
        ("div_s -6/2", InstructionType.I32_DIV_S, -6, 2, -3),
        ("div_s 6/-2", InstructionType.I32_DIV_S, 6, -2, -3),
        ("div_s -6/-2", InstructionType.I32_DIV_S, -6, -2, 3),
        ("div_s -99/100", InstructionType.I32_DIV_S, -99, 100, -1),
        ("div_s -101/100", InstructionType.I32_DIV_S, -101, 100, -2),
        ("rem_s 13%3", InstructionType.I32_REM_S, 13, 3, 1),
        ("rem_s -13%3", InstructionType.I32_REM_S, -13, 3, -1),
        ("rem_s 13%-3", InstructionType.I32_REM_S, 13, -3, 1),
        ("rem_s -13%-3", InstructionType.I32_REM_S, -13, -3, -1),
        ("and", InstructionType.I32_AND, 0xFF00FF00, 0x12345678, 0x12005600),
        ("or", InstructionType.I32_OR, 0xFF00FF00, 0x12345678, 0xFF34FF78),
        ("xor", InstructionType.I32_XOR, 0xFF00FF00, 0xFFFF0000, 0x00FFFF00),
        ("shl", InstructionType.I32_SHL, 0xFF00FF00, 4, 0xF00FF000),
        ("shl mods", InstructionType.I32_SHL, 0xFF00FF00, 35, 0x7F807F800),
        ("shr_s", InstructionType.I32_SHR_S, 0x0F00FF00, 4, 0x00F00FF0),
        ("shr_s neg", InstructionType.I32_SHR_S, 0xFF00FF00, 4, 0xFFF00FF0),
        ("shr_s mods", InstructionType.I32_SHR_S, 0x0F00FF00, 35, 0x01E01FE0),
        ("shr_u", InstructionType.I32_SHR_U, 0x0F00FF00, 4, 0x00F00FF0),
        ("shr_u neg", InstructionType.I32_SHR_U, 0xFF00FF00, 4, 0x0FF00FF0),
        ("shr_u mods", InstructionType.I32_SHR_U, 0x0F00FF00, 35, 0x01E01FE0),
        ("rotl hi bit set", InstructionType.I32_ROTL, 0xF000000F, 1, 0xE000001F),
        ("rotl hi bit clr", InstructionType.I32_ROTL, 0x7000000F, 1, 0xE000001E),
        ("rotr lo bit set", InstructionType.I32_ROTR, 0xF000000F, 1, 0xF8000007),
        ("rotr lo bit clr", InstructionType.I32_ROTR, 0xF000000E, 1, 0x78000007),
    )
    def test_i32_binops(
        self, insn_type: InstructionType, a: int, b: int, expected: int
    ):
        self.machine.push(Value(ValueType.I32, a & MASK32))
        self.machine.push(Value(ValueType.I32, b & MASK32))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected & MASK32))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("add", InstructionType.I64_ADD, 2, 1, 3),
        (
            "add mods",
            InstructionType.I64_ADD,
            0xFEDC000000000000,
            0x5678000000000000,
            0x5554000000000000,
        ),
        ("mul", InstructionType.I64_MUL, 2, 3, 6),
        (
            "mul mods",
            InstructionType.I64_MUL,
            0xFEDC123400000000,
            0x56789ABC,
            0x8CF0A63000000000,
        ),
        ("sub", InstructionType.I64_SUB, 2, 1, 1),
        (
            "sub mods",
            InstructionType.I64_SUB,
            0x00001234,
            0x56789ABC,
            0xFFFFFFFFA9877778,
        ),
        ("div_u", InstructionType.I64_DIV_U, 6, 2, 3),
        ("div_u 99/100", InstructionType.I64_DIV_U, 99, 100, 0),
        ("div_u 101/100", InstructionType.I64_DIV_U, 101, 100, 1),
        ("rem_u", InstructionType.I64_REM_U, 6, 4, 2),
        ("rem_u 99%100", InstructionType.I64_REM_U, 99, 100, 99),
        ("rem_u 101%100", InstructionType.I64_REM_U, 101, 100, 1),
        ("div_s 6/2", InstructionType.I64_DIV_S, 6, 2, 3),
        ("div_s -6/2", InstructionType.I64_DIV_S, -6, 2, -3),
        ("div_s 6/-2", InstructionType.I64_DIV_S, 6, -2, -3),
        ("div_s -6/-2", InstructionType.I64_DIV_S, -6, -2, 3),
        ("div_s -99/100", InstructionType.I64_DIV_S, -99, 100, -1),
        ("div_s -101/100", InstructionType.I64_DIV_S, -101, 100, -2),
        ("rem_s 13%3", InstructionType.I64_REM_S, 13, 3, 1),
        ("rem_s -13%3", InstructionType.I64_REM_S, -13, 3, -1),
        ("rem_s 13%-3", InstructionType.I64_REM_S, 13, -3, 1),
        ("rem_s -13%-3", InstructionType.I64_REM_S, -13, -3, -1),
        (
            "and",
            InstructionType.I64_AND,
            0xFF00FF0000FF00FF,
            0x1234567812345678,
            0x1200560000340078,
        ),
        (
            "or",
            InstructionType.I64_OR,
            0xFF00FF0000FF00FF,
            0x1234567812345678,
            0xFF34FF7812FF56FF,
        ),
        (
            "xor",
            InstructionType.I64_XOR,
            0xFF00FF0000FF00FF,
            0xFFFF00000000FFFF,
            0x00FFFF0000FFFF00,
        ),
        ("shl", InstructionType.I64_SHL, 0xFF00FF00FF00FF00, 4, 0xF00FF00FF00FF000),
        (
            "shl mods",
            InstructionType.I64_SHL,
            0xFF00FF00FF00FF00,
            67,
            0x7F807F807F807F800,
        ),
        (
            "shr_s",
            InstructionType.I64_SHR_S,
            0x0F00FF00FF00FF00,
            4,
            0x00F00FF00FF00FF0,
        ),
        (
            "shr_s neg",
            InstructionType.I64_SHR_S,
            0xFF00FF00FF00FF00,
            4,
            0xFFF00FF00FF00FF0,
        ),
        (
            "shr_s mods",
            InstructionType.I64_SHR_S,
            0x0F00FF00FF00FF00,
            67,
            0x01E01FE01FE01FE0,
        ),
        (
            "shr_u",
            InstructionType.I64_SHR_U,
            0x0F00FF00FF00FF00,
            4,
            0x00F00FF00FF00FF0,
        ),
        (
            "shr_u neg",
            InstructionType.I64_SHR_U,
            0xFF00FF00FF00FF00,
            4,
            0x0FF00FF00FF00FF0,
        ),
        (
            "shr_u mods",
            InstructionType.I64_SHR_U,
            0x0F00FF00FF00FF00,
            67,
            0x01E01FE01FE01FE0,
        ),
        (
            "rotl hi bit set",
            InstructionType.I64_ROTL,
            0xF00000000000000F,
            1,
            0xE00000000000001F,
        ),
        (
            "rotl hi bit clr",
            InstructionType.I64_ROTL,
            0x700000000000000F,
            1,
            0xE00000000000001E,
        ),
        (
            "rotr lo bit set",
            InstructionType.I64_ROTR,
            0xF00000000000000F,
            1,
            0xF800000000000007,
        ),
        (
            "rotr lo bit clr",
            InstructionType.I64_ROTR,
            0xF00000000000000E,
            1,
            0x7800000000000007,
        ),
    )
    def test_i64_binops(
        self, insn_type: InstructionType, a: int, b: int, expected: int
    ):
        self.machine.push(Value(ValueType.I64, a & MASK64))
        self.machine.push(Value(ValueType.I64, b & MASK64))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I64, expected & MASK64))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("eq False", InstructionType.I32_EQ, 2, 1, 0),
        ("eq True", InstructionType.I32_EQ, 2, 2, 1),
        ("ne False", InstructionType.I32_NE, 2, 1, 1),
        ("ne True", InstructionType.I32_NE, 2, 2, 0),
        ("lt_u 1 < 2 is True", InstructionType.I32_LT_U, 1, 2, 1),
        ("lt_u 2 < 1 is False", InstructionType.I32_LT_U, 2, 1, 0),
        ("lt_u 1 < 1 is False", InstructionType.I32_LT_U, 1, 1, 0),
        ("lt_u -2 < -1 unsigned is True", InstructionType.I32_LT_U, -2, -1, 1),
        ("lt_u -1 < -2 unsigned is False", InstructionType.I32_LT_U, -1, -2, 0),
        ("lt_u -1 < 1 unsigned is False", InstructionType.I32_LT_U, -1, 1, 0),
        ("lt_u 1 < -1 unsigned is True", InstructionType.I32_LT_U, 1, -1, 1),
        ("lt_s 1 < 2 is True", InstructionType.I32_LT_S, 1, 2, 1),
        ("lt_s 2 < 1 is False", InstructionType.I32_LT_S, 2, 1, 0),
        ("lt_s 1 < 1 is False", InstructionType.I32_LT_S, 1, 1, 0),
        ("lt_s -2 < -1 signed is True", InstructionType.I32_LT_S, -2, -1, 1),
        ("lt_s -1 < -2 signed is False", InstructionType.I32_LT_S, -1, -2, 0),
        ("lt_s -1 < 1 signed is True", InstructionType.I32_LT_S, -1, 1, 1),
        ("lt_s 1 < -1 signed is False", InstructionType.I32_LT_S, 1, -1, 0),
        ("gt_u 1 > 2 is False", InstructionType.I32_GT_U, 1, 2, 0),
        ("gt_u 2 > 1 is True", InstructionType.I32_GT_U, 2, 1, 1),
        ("gt_u 1 > 1 is False", InstructionType.I32_GT_U, 1, 1, 0),
        ("gt_u -2 > -1 unsigned is False", InstructionType.I32_GT_U, -2, -1, 0),
        ("gt_u -1 > -2 unsigned is True", InstructionType.I32_GT_U, -1, -2, 1),
        ("gt_u -1 > 1 unsigned is True", InstructionType.I32_GT_U, -1, 1, 1),
        ("gt_u 1 > -1 unsigned is False", InstructionType.I32_GT_U, 1, -1, 0),
        ("gt_s 1 > 2 is False", InstructionType.I32_GT_S, 1, 2, 0),
        ("gt_s 2 > 1 is True", InstructionType.I32_GT_S, 2, 1, 1),
        ("gt_s 1 > 1 is False", InstructionType.I32_GT_S, 1, 1, 0),
        ("gt_s -2 > -1 signed is False", InstructionType.I32_GT_S, -2, -1, 0),
        ("gt_s -1 > -2 signed is True", InstructionType.I32_GT_S, -1, -2, 1),
        ("gt_s -1 > 1 signed is False", InstructionType.I32_GT_S, -1, 1, 0),
        ("gt_s 1 > -1 signed is True", InstructionType.I32_GT_S, 1, -1, 1),
        ("le_u 1 <= 2 is True", InstructionType.I32_LE_U, 1, 2, 1),
        ("le_u 2 <= 1 is False", InstructionType.I32_LE_U, 2, 1, 0),
        ("le_u 1 <= 1 is True", InstructionType.I32_LE_U, 1, 1, 1),
        ("le_u -2 <= -1 unsigned is True", InstructionType.I32_LE_U, -2, -1, 1),
        ("le_u -1 <= -2 unsigned is False", InstructionType.I32_LE_U, -1, -2, 0),
        ("le_u -1 <= 1 unsigned is False", InstructionType.I32_LE_U, -1, 1, 0),
        ("le_u 1 <= -1 unsigned is True", InstructionType.I32_LE_U, 1, -1, 1),
        ("le_s 1 <= 2 is True", InstructionType.I32_LE_S, 1, 2, 1),
        ("le_s 2 <= 1 is False", InstructionType.I32_LE_S, 2, 1, 0),
        ("le_s 1 <= 1 is True", InstructionType.I32_LE_S, 1, 1, 1),
        ("le_s -2 <= -1 signed is True", InstructionType.I32_LE_S, -2, -1, 1),
        ("le_s -1 <= -2 signed is False", InstructionType.I32_LE_S, -1, -2, 0),
        ("le_s -1 <= 1 signed is True", InstructionType.I32_LE_S, -1, 1, 1),
        ("le_s 1 <= -1 signed is False", InstructionType.I32_LE_S, 1, -1, 0),
        ("ge_u 1 >= 2 is False", InstructionType.I32_GE_U, 1, 2, 0),
        ("ge_u 2 >= 1 is True", InstructionType.I32_GE_U, 2, 1, 1),
        ("ge_u 1 >= 1 is True", InstructionType.I32_GE_U, 1, 1, 1),
        ("ge_u -2 >= -1 unsigned is False", InstructionType.I32_GE_U, -2, -1, 0),
        ("ge_u -1 >= -2 unsigned is True", InstructionType.I32_GE_U, -1, -2, 1),
        ("ge_u -1 >= 1 unsigned is True", InstructionType.I32_GE_U, -1, 1, 1),
        ("ge_u 1 >= -1 unsigned is False", InstructionType.I32_GE_U, 1, -1, 0),
        ("ge_s 1 >= 2 is False", InstructionType.I32_GE_S, 1, 2, 0),
        ("ge_s 2 >= 1 is True", InstructionType.I32_GE_S, 2, 1, 1),
        ("ge_s 1 >= 1 is True", InstructionType.I32_GE_S, 1, 1, 1),
        ("ge_s -2 >= -1 signed is False", InstructionType.I32_GE_S, -2, -1, 0),
        ("ge_s -1 >= -2 signed is True", InstructionType.I32_GE_S, -1, -2, 1),
        ("ge_s -1 >= 1 signed is False", InstructionType.I32_GE_S, -1, 1, 0),
        ("ge_s 1 >= -1 signed is True", InstructionType.I32_GE_S, 1, -1, 1),
    )
    def test_i32_relops(
        self, insn_type: InstructionType, a: int, b: int, expected: int
    ):
        self.machine.push(Value(ValueType.I32, a & MASK32))
        self.machine.push(Value(ValueType.I32, b & MASK32))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected & MASK32))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("eq False", InstructionType.I64_EQ, 2, 1, 0),
        ("eq True", InstructionType.I64_EQ, 2, 2, 1),
        ("ne False", InstructionType.I64_NE, 2, 1, 1),
        ("ne True", InstructionType.I64_NE, 2, 2, 0),
        ("lt_u 1 < 2 is True", InstructionType.I64_LT_U, 1, 2, 1),
        ("lt_u 2 < 1 is False", InstructionType.I64_LT_U, 2, 1, 0),
        ("lt_u 1 < 1 is False", InstructionType.I64_LT_U, 1, 1, 0),
        ("lt_u -2 < -1 unsigned is True", InstructionType.I64_LT_U, -2, -1, 1),
        ("lt_u -1 < -2 unsigned is False", InstructionType.I64_LT_U, -1, -2, 0),
        ("lt_u -1 < 1 unsigned is False", InstructionType.I64_LT_U, -1, 1, 0),
        ("lt_u 1 < -1 unsigned is True", InstructionType.I64_LT_U, 1, -1, 1),
        ("lt_s 1 < 2 is True", InstructionType.I64_LT_S, 1, 2, 1),
        ("lt_s 2 < 1 is False", InstructionType.I64_LT_S, 2, 1, 0),
        ("lt_s 1 < 1 is False", InstructionType.I64_LT_S, 1, 1, 0),
        ("lt_s -2 < -1 signed is True", InstructionType.I64_LT_S, -2, -1, 1),
        ("lt_s -1 < -2 signed is False", InstructionType.I64_LT_S, -1, -2, 0),
        ("lt_s -1 < 1 signed is True", InstructionType.I64_LT_S, -1, 1, 1),
        ("lt_s 1 < -1 signed is False", InstructionType.I64_LT_S, 1, -1, 0),
        ("gt_u 1 > 2 is False", InstructionType.I64_GT_U, 1, 2, 0),
        ("gt_u 2 > 1 is True", InstructionType.I64_GT_U, 2, 1, 1),
        ("gt_u 1 > 1 is False", InstructionType.I64_GT_U, 1, 1, 0),
        ("gt_u -2 > -1 unsigned is False", InstructionType.I64_GT_U, -2, -1, 0),
        ("gt_u -1 > -2 unsigned is True", InstructionType.I64_GT_U, -1, -2, 1),
        ("gt_u -1 > 1 unsigned is True", InstructionType.I64_GT_U, -1, 1, 1),
        ("gt_u 1 > -1 unsigned is False", InstructionType.I64_GT_U, 1, -1, 0),
        ("gt_s 1 > 2 is False", InstructionType.I64_GT_S, 1, 2, 0),
        ("gt_s 2 > 1 is True", InstructionType.I64_GT_S, 2, 1, 1),
        ("gt_s 1 > 1 is False", InstructionType.I64_GT_S, 1, 1, 0),
        ("gt_s -2 > -1 signed is False", InstructionType.I64_GT_S, -2, -1, 0),
        ("gt_s -1 > -2 signed is True", InstructionType.I64_GT_S, -1, -2, 1),
        ("gt_s -1 > 1 signed is False", InstructionType.I64_GT_S, -1, 1, 0),
        ("gt_s 1 > -1 signed is True", InstructionType.I64_GT_S, 1, -1, 1),
        ("le_u 1 <= 2 is True", InstructionType.I64_LE_U, 1, 2, 1),
        ("le_u 2 <= 1 is False", InstructionType.I64_LE_U, 2, 1, 0),
        ("le_u 1 <= 1 is True", InstructionType.I64_LE_U, 1, 1, 1),
        ("le_u -2 <= -1 unsigned is True", InstructionType.I64_LE_U, -2, -1, 1),
        ("le_u -1 <= -2 unsigned is False", InstructionType.I64_LE_U, -1, -2, 0),
        ("le_u -1 <= 1 unsigned is False", InstructionType.I64_LE_U, -1, 1, 0),
        ("le_u 1 <= -1 unsigned is True", InstructionType.I64_LE_U, 1, -1, 1),
        ("le_s 1 <= 2 is True", InstructionType.I64_LE_S, 1, 2, 1),
        ("le_s 2 <= 1 is False", InstructionType.I64_LE_S, 2, 1, 0),
        ("le_s 1 <= 1 is True", InstructionType.I64_LE_S, 1, 1, 1),
        ("le_s -2 <= -1 signed is True", InstructionType.I64_LE_S, -2, -1, 1),
        ("le_s -1 <= -2 signed is False", InstructionType.I64_LE_S, -1, -2, 0),
        ("le_s -1 <= 1 signed is True", InstructionType.I64_LE_S, -1, 1, 1),
        ("le_s 1 <= -1 signed is False", InstructionType.I64_LE_S, 1, -1, 0),
        ("ge_u 1 >= 2 is False", InstructionType.I64_GE_U, 1, 2, 0),
        ("ge_u 2 >= 1 is True", InstructionType.I64_GE_U, 2, 1, 1),
        ("ge_u 1 >= 1 is True", InstructionType.I64_GE_U, 1, 1, 1),
        ("ge_u -2 >= -1 unsigned is False", InstructionType.I64_GE_U, -2, -1, 0),
        ("ge_u -1 >= -2 unsigned is True", InstructionType.I64_GE_U, -1, -2, 1),
        ("ge_u -1 >= 1 unsigned is True", InstructionType.I64_GE_U, -1, 1, 1),
        ("ge_u 1 >= -1 unsigned is False", InstructionType.I64_GE_U, 1, -1, 0),
        ("ge_s 1 >= 2 is False", InstructionType.I64_GE_S, 1, 2, 0),
        ("ge_s 2 >= 1 is True", InstructionType.I64_GE_S, 2, 1, 1),
        ("ge_s 1 >= 1 is True", InstructionType.I64_GE_S, 1, 1, 1),
        ("ge_s -2 >= -1 signed is False", InstructionType.I64_GE_S, -2, -1, 0),
        ("ge_s -1 >= -2 signed is True", InstructionType.I64_GE_S, -1, -2, 1),
        ("ge_s -1 >= 1 signed is False", InstructionType.I64_GE_S, -1, 1, 0),
        ("ge_s 1 >= -1 signed is True", InstructionType.I64_GE_S, 1, -1, 1),
    )
    def test_i64_relops(
        self, insn_type: InstructionType, a: int, b: int, expected: int
    ):
        self.machine.push(Value(ValueType.I64, a & MASK64))
        self.machine.push(Value(ValueType.I64, b & MASK64))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected & MASK32))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("clz all zeros", InstructionType.I32_CLZ, 0x00000000, 32),
        ("clz", InstructionType.I32_CLZ, 0x00800000, 8),
        ("ctz all zeros", InstructionType.I32_CTZ, 0x00000000, 32),
        ("ctz", InstructionType.I32_CTZ, 0x00000100, 8),
        ("popcnt all zeros", InstructionType.I32_POPCNT, 0x00000000, 0),
        ("popcnt all ones", InstructionType.I32_POPCNT, 0xFFFFFFFF, 32),
        ("popcnt", InstructionType.I32_POPCNT, 0x0011110F, 8),
    )
    def test_i32_unops(self, insn_type: InstructionType, a: int, expected: int):
        self.machine.push(Value(ValueType.I32, a & MASK32))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected & MASK32))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("clz all zeros", InstructionType.I64_CLZ, 0x0000000000000000, 64),
        ("clz", InstructionType.I64_CLZ, 0x0000000000800000, 40),
        ("ctz all zeros", InstructionType.I64_CTZ, 0x0000000000000000, 64),
        ("ctz", InstructionType.I64_CTZ, 0x0000010000000000, 40),
        ("popcnt all zeros", InstructionType.I64_POPCNT, 0x0000000000000000, 0),
        ("popcnt all ones", InstructionType.I64_POPCNT, 0xFFFFFFFFFFFFFFFF, 64),
        ("popcnt", InstructionType.I64_POPCNT, 0x0011110F0011110F, 16),
    )
    def test_i64_unops(self, insn_type: InstructionType, a: int, expected: int):
        self.machine.push(Value(ValueType.I64, a & MASK64))

        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I64, expected & MASK64))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("div_u", InstructionType.I32_DIV_U, 6, 0),
        ("div_s n/0", InstructionType.I32_DIV_S, 6, 0),
        ("div_s -2^31 / -1", InstructionType.I32_DIV_S, -0x80000000, -1),
    )
    def test_i32_binops_trap(self, insn_type: InstructionType, a: int, b: int):
        self.machine.push(Value(ValueType.I32, a & MASK32))
        self.machine.push(Value(ValueType.I32, b & MASK32))

        with self.assertRaises(RuntimeError):
            insn_eval.eval_insn(self.machine, noarg(insn_type))

    @parameterized.named_parameters(
        ("div_u", InstructionType.I64_DIV_U, 6, 0),
        ("div_s n/0", InstructionType.I64_DIV_S, 6, 0),
        ("div_s -2^63 / -1", InstructionType.I64_DIV_S, -0x8000000000000000, -1),
    )
    def test_i64_binops_trap(self, insn_type: InstructionType, a: int, b: int):
        self.machine.push(Value(ValueType.I64, a & MASK64))
        self.machine.push(Value(ValueType.I64, b & MASK64))

        with self.assertRaises(RuntimeError):
            insn_eval.eval_insn(self.machine, noarg(insn_type))

    @parameterized.named_parameters(
        (
            "0 i32.load 0",
            InstructionType.I32_LOAD,
            0,
            0,
            Value(ValueType.I32, 0x03020100),
        ),
        (
            "1 i32.load 0",
            InstructionType.I32_LOAD,
            1,
            0,
            Value(ValueType.I32, 0x04030201),
        ),
        (
            "0 i32.load 1",
            InstructionType.I32_LOAD,
            0,
            1,
            Value(ValueType.I32, 0x04030201),
        ),
        (
            "1 i32.load 1",
            InstructionType.I32_LOAD,
            1,
            1,
            Value(ValueType.I32, 0x05040302),
        ),
        (
            "0 i64.load 0",
            InstructionType.I64_LOAD,
            0,
            0,
            Value(ValueType.I64, 0x0706050403020100),
        ),
        (
            "1 i64.load 0",
            InstructionType.I64_LOAD,
            1,
            0,
            Value(ValueType.I64, 0x0807060504030201),
        ),
        (
            "0 i64.load 1",
            InstructionType.I64_LOAD,
            0,
            1,
            Value(ValueType.I64, 0x0807060504030201),
        ),
        (
            "1 i64.load 1",
            InstructionType.I64_LOAD,
            1,
            1,
            Value(ValueType.I64, 0x0908070605040302),
        ),
        (
            "0 f32.load 0",
            InstructionType.F32_LOAD,
            0,
            0,
            # Ideally here I'd use the actual float value, but because Python,
            # it would be a double truncated to a float32. Rather than do that,
            # I'll just use the C conversions provided by struct.
            Value(ValueType.F32, struct.unpack("<f", b"\x00\x01\x02\x03")[0]),
        ),
        (
            "1 f32.load 0",
            InstructionType.F32_LOAD,
            1,
            0,
            Value(ValueType.F32, struct.unpack("<f", b"\x01\x02\x03\x04")[0]),
        ),
        (
            "0 f32.load 1",
            InstructionType.F32_LOAD,
            0,
            1,
            Value(ValueType.F32, struct.unpack("<f", b"\x01\x02\x03\x04")[0]),
        ),
        (
            "1 f32.load 1",
            InstructionType.F32_LOAD,
            1,
            1,
            Value(ValueType.F32, struct.unpack("<f", b"\x02\x03\x04\x05")[0]),
        ),
        (
            "0 f64.load 0",
            InstructionType.F64_LOAD,
            0,
            0,
            Value(ValueType.F64, 7.94992889512736253615566268553e-275),
        ),
        (
            "1 f64.load 0",
            InstructionType.F64_LOAD,
            1,
            0,
            Value(ValueType.F64, 5.44760372201160503468005645009e-270),
        ),
        (
            "0 f64.load 1",
            InstructionType.F64_LOAD,
            0,
            1,
            Value(ValueType.F64, 5.44760372201160503468005645009e-270),
        ),
        (
            "1 f64.load 1",
            InstructionType.F64_LOAD,
            1,
            1,
            Value(ValueType.F64, 3.7258146895053073663034247103e-265),
        ),
    )
    def test_load(
        self, insn_type: InstructionType, base: int, offset: int, expected: Value
    ):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        insn_eval.eval_insn(self.machine, op2(insn_type, 4, offset))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("i32.load", 4, InstructionType.I32_LOAD),
        ("i64.load", 8, InstructionType.I64_LOAD),
        ("f32.load", 4, InstructionType.F32_LOAD),
        ("f64.load", 8, InstructionType.F64_LOAD),
    )
    def test_load_raises_on_access_out_of_bounds(
        self, sz: int, insn_type: InstructionType
    ):
        self.machine.push(Value(ValueType.I32, 65536 - sz + 1))

        with self.assertRaisesRegex(RuntimeError, "load: access out of bounds"):
            insn_eval.eval_insn(self.machine, op2(insn_type, 4, 0))

    @parameterized.named_parameters(
        (
            "0 i32.load8_u 0",
            0,
            0,
            InstructionType.I32_LOAD8_U,
            Value(ValueType.I32, 0x00000080),
        ),
        (
            "1 i32.load8_u 0",
            1,
            0,
            InstructionType.I32_LOAD8_U,
            Value(ValueType.I32, 0x00000081),
        ),
        (
            "0 i32.load8_u 1",
            0,
            1,
            InstructionType.I32_LOAD8_U,
            Value(ValueType.I32, 0x00000081),
        ),
        (
            "1 i32.load8_u",
            1,
            1,
            InstructionType.I32_LOAD8_U,
            Value(ValueType.I32, 0x00000082),
        ),
        (
            "0 i32.load8_s 0",
            0,
            0,
            InstructionType.I32_LOAD8_S,
            Value(ValueType.I32, 0xFFFFFF80),
        ),
        (
            "1 i32.load8_s 0",
            1,
            0,
            InstructionType.I32_LOAD8_S,
            Value(ValueType.I32, 0xFFFFFF81),
        ),
        (
            "0 i32.load8_s 1",
            0,
            1,
            InstructionType.I32_LOAD8_S,
            Value(ValueType.I32, 0xFFFFFF81),
        ),
        (
            "1 i32.load8_s",
            1,
            1,
            InstructionType.I32_LOAD8_S,
            Value(ValueType.I32, 0xFFFFFF82),
        ),
        (
            "0 i32.load16_u 0",
            0,
            0,
            InstructionType.I32_LOAD16_U,
            Value(ValueType.I32, 0x00008180),
        ),
        (
            "1 i32.load16_u 0",
            1,
            0,
            InstructionType.I32_LOAD16_U,
            Value(ValueType.I32, 0x00008281),
        ),
        (
            "0 i32.load16_u 1",
            0,
            1,
            InstructionType.I32_LOAD16_U,
            Value(ValueType.I32, 0x00008281),
        ),
        (
            "1 i32.load16_u",
            1,
            1,
            InstructionType.I32_LOAD16_U,
            Value(ValueType.I32, 0x00008382),
        ),
        (
            "0 i32.load16_s 0",
            0,
            0,
            InstructionType.I32_LOAD16_S,
            Value(ValueType.I32, 0xFFFF8180),
        ),
        (
            "1 i32.load16_s 0",
            1,
            0,
            InstructionType.I32_LOAD16_S,
            Value(ValueType.I32, 0xFFFF8281),
        ),
        (
            "0 i32.load16_s 1",
            0,
            1,
            InstructionType.I32_LOAD16_S,
            Value(ValueType.I32, 0xFFFF8281),
        ),
        (
            "1 i32.load16_s",
            1,
            1,
            InstructionType.I32_LOAD16_S,
            Value(ValueType.I32, 0xFFFF8382),
        ),
        (
            "0 i64.load8_u 0",
            0,
            0,
            InstructionType.I64_LOAD8_U,
            Value(ValueType.I64, 0x0000000000000080),
        ),
        (
            "1 i64.load8_u 0",
            1,
            0,
            InstructionType.I64_LOAD8_U,
            Value(ValueType.I64, 0x0000000000000081),
        ),
        (
            "0 i64.load8_u 1",
            0,
            1,
            InstructionType.I64_LOAD8_U,
            Value(ValueType.I64, 0x0000000000000081),
        ),
        (
            "1 i64.load8_u",
            1,
            1,
            InstructionType.I64_LOAD8_U,
            Value(ValueType.I64, 0x0000000000000082),
        ),
        (
            "0 i64.load8_s 0",
            0,
            0,
            InstructionType.I64_LOAD8_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFFFF80),
        ),
        (
            "1 i64.load8_s 0",
            1,
            0,
            InstructionType.I64_LOAD8_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFFFF81),
        ),
        (
            "0 i64.load8_s 1",
            0,
            1,
            InstructionType.I64_LOAD8_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFFFF81),
        ),
        (
            "1 i64.load8_s",
            1,
            1,
            InstructionType.I64_LOAD8_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFFFF82),
        ),
        (
            "0 i64.load16_u 0",
            0,
            0,
            InstructionType.I64_LOAD16_U,
            Value(ValueType.I64, 0x0000000000008180),
        ),
        (
            "1 i64.load16_u 0",
            1,
            0,
            InstructionType.I64_LOAD16_U,
            Value(ValueType.I64, 0x0000000000008281),
        ),
        (
            "0 i64.load16_u 1",
            0,
            1,
            InstructionType.I64_LOAD16_U,
            Value(ValueType.I64, 0x0000000000008281),
        ),
        (
            "1 i64.load16_u",
            1,
            1,
            InstructionType.I64_LOAD16_U,
            Value(ValueType.I64, 0x0000000000008382),
        ),
        (
            "0 i64.load16_s 0",
            0,
            0,
            InstructionType.I64_LOAD16_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFF8180),
        ),
        (
            "1 i64.load16_s 0",
            1,
            0,
            InstructionType.I64_LOAD16_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFF8281),
        ),
        (
            "0 i64.load16_s 1",
            0,
            1,
            InstructionType.I64_LOAD16_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFF8281),
        ),
        (
            "1 i64.load16_s",
            1,
            1,
            InstructionType.I64_LOAD16_S,
            Value(ValueType.I64, 0xFFFFFFFFFFFF8382),
        ),
        (
            "0 i64.load32_u 0",
            0,
            0,
            InstructionType.I64_LOAD32_U,
            Value(ValueType.I64, 0x0000000083828180),
        ),
        (
            "1 i64.load32_u 0",
            1,
            0,
            InstructionType.I64_LOAD32_U,
            Value(ValueType.I64, 0x0000000084838281),
        ),
        (
            "0 i64.load32_u 1",
            0,
            1,
            InstructionType.I64_LOAD32_U,
            Value(ValueType.I64, 0x0000000084838281),
        ),
        (
            "1 i64.load32_u",
            1,
            1,
            InstructionType.I64_LOAD32_U,
            Value(ValueType.I64, 0x0000000085848382),
        ),
        (
            "0 i64.load32_s 0",
            0,
            0,
            InstructionType.I64_LOAD32_S,
            Value(ValueType.I64, 0xFFFFFFFF83828180),
        ),
        (
            "1 i64.load32_s 0",
            1,
            0,
            InstructionType.I64_LOAD32_S,
            Value(ValueType.I64, 0xFFFFFFFF84838281),
        ),
        (
            "0 i64.load32_s 1",
            0,
            1,
            InstructionType.I64_LOAD32_S,
            Value(ValueType.I64, 0xFFFFFFFF84838281),
        ),
        (
            "1 i64.load32_s",
            1,
            1,
            InstructionType.I64_LOAD32_S,
            Value(ValueType.I64, 0xFFFFFFFF85848382),
        ),
    )
    def test_loadN(
        self, base: int, offset: int, insn_type: InstructionType, expected: Value
    ):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.get_mem_data(0)[0:10] = b"\x80\x81\x82\x83\x84\x85\x86\x87\x88\x89"

        insn_eval.eval_insn(self.machine, op2(insn_type, 4, offset))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        (
            "0 store 0",
            InstructionType.I32_STORE,
            0,
            0,
            b"\x78\x56\x34\x12\x04\x05\x06\x07\x08\x09",
        ),
        (
            "1 store 0",
            InstructionType.I32_STORE,
            1,
            0,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "0 store 1",
            InstructionType.I32_STORE,
            0,
            1,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "1 store 1",
            InstructionType.I32_STORE,
            1,
            1,
            b"\x00\x01\x78\x56\x34\x12\x06\x07\x08\x09",
        ),
        (
            "0 store16 0",
            InstructionType.I32_STORE16,
            0,
            0,
            b"\x78\x56\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "1 store16 0",
            InstructionType.I32_STORE16,
            1,
            0,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "0 store16 1",
            InstructionType.I32_STORE16,
            0,
            1,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "1 store16 1",
            InstructionType.I32_STORE16,
            1,
            1,
            b"\x00\x01\x78\x56\x04\x05\x06\x07\x08\x09",
        ),
        (
            "0 store8 0",
            InstructionType.I32_STORE8,
            0,
            0,
            b"\x78\x01\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "1 store8 0",
            InstructionType.I32_STORE8,
            1,
            0,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "0 store8 1",
            InstructionType.I32_STORE8,
            0,
            1,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "1 store8 1",
            InstructionType.I32_STORE8,
            1,
            1,
            b"\x00\x01\x78\x03\x04\x05\x06\x07\x08\x09",
        ),
    )
    def test_i32_store(
        self, insn_type: InstructionType, base: int, offset: int, expected: bytes
    ):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.push(Value(ValueType.I32, 0x12345678))
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"
        instruction = Instruction(insn_type, [4, offset], 0, 0)

        insn_eval.eval_insn(self.machine, instruction)

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)
        self.assertEqual(self.machine.get_mem_data(0)[0:10], expected)

    @parameterized.named_parameters(
        ("store", InstructionType.I32_STORE),
        ("store16", InstructionType.I32_STORE16),
        ("store8", InstructionType.I32_STORE8),
    )
    def test_i32_store_raises_on_access_out_of_bounds(self, insn_type: InstructionType):
        self.machine.push(Value(ValueType.I32, 65536))
        self.machine.push(Value(ValueType.I32, 0x12345678))

        instruction = Instruction(insn_type, [4, 0], 0, 0)

        with self.assertRaisesRegex(RuntimeError, "access out of bounds"):
            insn_eval.eval_insn(self.machine, instruction)

    @parameterized.named_parameters(
        (
            "i64_store 0xDDCCBBAA12345678 to base 0, offset 0",
            InstructionType.I64_STORE,
            0,
            0,
            b"\x78\x56\x34\x12\xAA\xBB\xCC\xDD\x08\x09",
        ),
        (
            "i64_store 0xDDCCBBAA12345678 to base 1, offset 0",
            InstructionType.I64_STORE,
            1,
            0,
            b"\x00\x78\x56\x34\x12\xAA\xBB\xCC\xDD\x09",
        ),
        (
            "i64_store 0xDDCCBBAA12345678 to base 0, offset 1",
            InstructionType.I64_STORE,
            0,
            1,
            b"\x00\x78\x56\x34\x12\xAA\xBB\xCC\xDD\x09",
        ),
        (
            "i64_store 0xDDCCBBAA12345678 to base 1, offset 1",
            InstructionType.I64_STORE,
            1,
            1,
            b"\x00\x01\x78\x56\x34\x12\xAA\xBB\xCC\xDD",
        ),
        (
            "i64_store32 0xDDCCBBAA12345678 to base 0, offset 0",
            InstructionType.I64_STORE32,
            0,
            0,
            b"\x78\x56\x34\x12\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store32 0xDDCCBBAA12345678 to base 1, offset 0",
            InstructionType.I64_STORE32,
            1,
            0,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store32 0xDDCCBBAA12345678 to base 0, offset 1",
            InstructionType.I64_STORE32,
            0,
            1,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store32 0xDDCCBBAA12345678 to base 1, offset 1",
            InstructionType.I64_STORE32,
            1,
            1,
            b"\x00\x01\x78\x56\x34\x12\x06\x07\x08\x09",
        ),
        (
            "i64_store16 0xDDCCBBAA12345678 to base 0, offset 0",
            InstructionType.I64_STORE16,
            0,
            0,
            b"\x78\x56\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store16 0xDDCCBBAA12345678 to base 1, offset 0",
            InstructionType.I64_STORE16,
            1,
            0,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store16 0xDDCCBBAA12345678 to base 0, offset 1",
            InstructionType.I64_STORE16,
            0,
            1,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store16 0xDDCCBBAA12345678 to base 1, offset 1",
            InstructionType.I64_STORE16,
            1,
            1,
            b"\x00\x01\x78\x56\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store8 0xDDCCBBAA12345678 to base 0, offset 0",
            InstructionType.I64_STORE8,
            0,
            0,
            b"\x78\x01\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store8 0xDDCCBBAA12345678 to base 1, offset 0",
            InstructionType.I64_STORE8,
            1,
            0,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store8 0xDDCCBBAA12345678 to base 0, offset 1",
            InstructionType.I64_STORE8,
            0,
            1,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i64_store8 0xDDCCBBAA12345678 to base 1, offset 1",
            InstructionType.I64_STORE8,
            1,
            1,
            b"\x00\x01\x78\x03\x04\x05\x06\x07\x08\x09",
        ),
    )
    def test_i64_store(
        self, insn_type: InstructionType, base: int, offset: int, expected: bytes
    ):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.push(Value(ValueType.I64, 0xDDCCBBAA12345678))
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        instruction = Instruction(insn_type, [4, offset], 0, 0)

        insn_eval.eval_insn(self.machine, instruction)

        self.assertEqual(self.machine.get_mem_data(0)[0:10], expected)
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("store", InstructionType.I64_STORE),
        ("store32", InstructionType.I64_STORE32),
        ("store16", InstructionType.I64_STORE16),
        ("store8", InstructionType.I64_STORE8),
    )
    def test_i64_store_raises_on_access_out_of_bounds(self, insn_type: InstructionType):
        self.machine.push(Value(ValueType.I32, 65536))
        self.machine.push(Value(ValueType.I64, 0xDDCCBBAA12345678))

        instruction = Instruction(insn_type, [4, 0], 0, 0)

        with self.assertRaisesRegex(RuntimeError, "access out of bounds"):
            insn_eval.eval_insn(self.machine, instruction)

    @parameterized.named_parameters(
        ("0 f32.store 0", 0, 0, b"\xEC\x51\x2E\x42\x04\x05\x06\x07\x08\x09"),
        ("0 f32.store 1", 0, 1, b"\x00\xEC\x51\x2E\x42\x05\x06\x07\x08\x09"),
        ("1 f32.store 0", 1, 0, b"\x00\xEC\x51\x2E\x42\x05\x06\x07\x08\x09"),
        ("1 f32.store 1", 1, 1, b"\x00\x01\xEC\x51\x2E\x42\x06\x07\x08\x09"),
    )
    def test_f32_store(self, base: int, offset: int, expected: bytes):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.push(Value(ValueType.F32, 43.58))
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        insn_eval.eval_insn(self.machine, op2(InstructionType.F32_STORE, 4, offset))

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)
        self.assertEqual(self.machine.get_mem_data(0)[0:10], expected)

    @parameterized.named_parameters(
        ("0 f64.store 0", 0, 0, b"\x0A\xD7\xA3\x70\x3D\xCA\x45\x40\x08\x09"),
        ("0 f64.store 1", 0, 1, b"\x00\x0A\xD7\xA3\x70\x3D\xCA\x45\x40\x09"),
        ("1 f64.store 0", 1, 0, b"\x00\x0A\xD7\xA3\x70\x3D\xCA\x45\x40\x09"),
        ("1 f64.store 1", 1, 1, b"\x00\x01\x0A\xD7\xA3\x70\x3D\xCA\x45\x40"),
    )
    def test_f64_store(self, base: int, offset: int, expected: bytes):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.push(Value(ValueType.F64, 43.58))
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        insn_eval.eval_insn(self.machine, op2(InstructionType.F64_STORE, 4, offset))

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)
        self.assertEqual(self.machine.get_mem_data(0)[0:10], expected)

    @parameterized.named_parameters(
        (
            "i64.extend_i32_u",
            InstructionType.I64_EXTEND_I32_U,
            Value(ValueType.I32, 0xFFFFFFFF),
            Value(ValueType.I64, 0xFFFFFFFF),
        ),
        (
            "i64.wrap_i32",
            InstructionType.I32_WRAP_I64,
            Value(ValueType.I64, 0x12345678FFFFFFFF),
            Value(ValueType.I32, 0xFFFFFFFF),
        ),
        (
            "1 i64.eqz is False",
            InstructionType.I64_EQZ,
            Value(ValueType.I64, 1),
            Value(ValueType.I32, 0),
        ),
        (
            "0 i64.eqz is True",
            InstructionType.I64_EQZ,
            Value(ValueType.I64, 0),
            Value(ValueType.I32, 1),
        ),
        (
            "1 i32.eqz is False",
            InstructionType.I32_EQZ,
            Value(ValueType.I32, 1),
            Value(ValueType.I32, 0),
        ),
        (
            "0 i32.eqz is True",
            InstructionType.I32_EQZ,
            Value(ValueType.I32, 0),
            Value(ValueType.I32, 1),
        ),
    )
    def test_cvtops(self, insn_type: InstructionType, v: Value, expected: Value):
        self.machine.push(v)
        insn_eval.eval_insn(self.machine, noarg(insn_type))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        (
            "ref.null func",
            InstructionType.REF_NULL,
            0x70,
            Value(ValueType.FUNCREF, None),
        ),
        (
            "ref.null externref",
            InstructionType.REF_NULL,
            0x6F,
            Value(ValueType.EXTERNREF, None),
        ),
        ("ref.func", InstructionType.REF_FUNC, 1, Value(ValueType.FUNCREF, 2)),
    )
    def test_ref(self, insn_type: InstructionType, val: int, expected: Value):
        self.module_inst.funcaddrs = [3, 2, 1]

        insn_eval.eval_insn(self.machine, op1(insn_type, val))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.get_current_frame().pc, 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        (
            "ref.is_null null func",
            Value(ValueType.FUNCREF, None),
            Value(ValueType.I32, 1),
        ),
        (
            "ref.is_null null externref",
            Value(ValueType.EXTERNREF, None),
            Value(ValueType.I32, 1),
        ),
        (
            "ref.is_null func",
            Value(ValueType.FUNCREF, 2),
            Value(ValueType.I32, 0),
        ),
    )
    def test_ref_is_null(self, val: Value, expected: Value):
        self.machine.push(val)

        insn_eval.eval_insn(self.machine, noarg(InstructionType.REF_IS_NULL))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.get_current_frame().pc, 1)
        self.assertEqual(self.machine.pop(), expected)

    def test_memory_init(self):
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]

        self.machine.push(Value(ValueType.I32, 2))  # dest offset
        self.machine.push(Value(ValueType.I32, 1))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        insn_eval.eval_insn(self.machine, memory_init(1, 0))

        self.assertEqual(self.machine.get_mem_data(0)[0:5], b"\x00\x00ar\x00")
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_memory_init_zero_size(self):
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]

        self.machine.push(Value(ValueType.I32, 2))  # dest offset
        self.machine.push(Value(ValueType.I32, 1))  # source offset
        self.machine.push(Value(ValueType.I32, 0))  # data size

        insn_eval.eval_insn(self.machine, memory_init(1, 0))

        self.assertEqual(self.machine.get_mem_data(0)[0:5], b"\x00\x00\x00\x00\x00")
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_memory_init_traps_source_out_of_bounds(self):
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]

        self.machine.push(Value(ValueType.I32, 1))  # dest offset
        self.machine.push(Value(ValueType.I32, 60))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        with self.assertRaisesRegex(RuntimeError, "source is out of bounds"):
            insn_eval.eval_insn(self.machine, memory_init(1, 0))

    def test_memory_init_traps_dest_out_of_bounds(self):
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]

        self.machine.push(Value(ValueType.I32, 65535))  # dest offset
        self.machine.push(Value(ValueType.I32, 1))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        with self.assertRaisesRegex(RuntimeError, "destination is out of bounds"):
            insn_eval.eval_insn(self.machine, memory_init(1, 0))

    def test_memory_size(self):
        self.machine.mems[0].data = bytearray(3 * 65536)

        insn_eval.eval_insn(self.machine, memory_size(0))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("grow by 1", 1, 1, 2),
        ("grow by 0", 0, 1, 1),
        ("grow by 0xFFFF", 0xFFFF, 0xFFFFFFFF, 1),
        ("grow by 1024", 1024, 0xFFFFFFFF, 1),  # tests max allowed size
        ("grow by 10", 10, 0xFFFFFFFF, 1),  # tests max limit
    )
    def test_memory_grow(self, n: int, expected_orig_size: int, expected_size: int):
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"
        self.machine.push(Value(ValueType.I32, n))

        insn_eval.eval_insn(self.machine, memory_grow(0))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_orig_size))
        self.assertEqual(len(self.machine.get_mem_data(0)), expected_size * 65536)
        self.assertEqual(
            self.machine.get_mem_data(0)[0:10],
            b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09",
        )
        self.assertEqual(self.machine.get_mem(0).mem_type.limits.min, expected_size)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("d <= s", 3, 1, 4, b"\x00\x03\x04\x05\x06\x05\x06\x07\x08\x09"),
        ("d > s", 1, 3, 4, b"\x00\x01\x02\x01\x02\x03\x04\x07\x08\x09"),
        ("size zero", 1, 3, 0, b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"),
    )
    def test_memory_copy_with_overlap(self, s: int, d: int, sz: int, expected: bytes):
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"
        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, sz))

        insn_eval.eval_insn(self.machine, memory_copy(0, 0))

        self.assertEqual(self.machine.get_mem_data(0)[0:10], expected)
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("dest out of bounds", 0, 65535, 4),
        ("source out of bounds", 65535, 0, 4),
        ("size out of bounds", 1, 1, 65536),
    )
    def test_memory_copy_raises_on_out_of_bounds(self, s: int, d: int, sz: int):
        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, sz))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(self.machine, memory_copy(0, 0))

    def test_memory_fill(self):
        self.machine.get_mem_data(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"
        self.machine.push(Value(ValueType.I32, 1))  # dest
        self.machine.push(Value(ValueType.I32, 0x12FF))  # value
        self.machine.push(Value(ValueType.I32, 4))  # size

        insn_eval.eval_insn(self.machine, memory_fill(0))

        self.assertEqual(
            self.machine.get_mem_data(0)[0:10],
            b"\x00\xFF\xFF\xFF\xFF\x05\x06\x07\x08\x09",
        )
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("dest out of bounds", 65535, 4),
        ("size out of bounds", 1, 65536),
    )
    def test_memory_fill_raises_on_out_of_bounds(self, d: int, sz: int):
        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, 0))  # value
        self.machine.push(Value(ValueType.I32, sz))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(self.machine, memory_fill(0))

    def test_data_drop(self):
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]

        insn_eval.eval_insn(self.machine, data_drop(2))

        self.assertIsNone(self.machine.datas[0])
        self.assertIsNotNone(self.machine.datas[1])
        self.assertIsNotNone(self.machine.datas[2])
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("table 0 elem 0", 0, 0, Value(ValueType.FUNCREF, 30)),
        ("table 0 elem 1", 0, 1, Value(ValueType.FUNCREF, 31)),
        ("table 1 elem 0", 1, 0, Value(ValueType.FUNCREF, 20)),
        ("table 1 elem 1", 1, 1, Value(ValueType.FUNCREF, 21)),
    )
    def test_table_get(self, tableidx: int, elemidx: int, expected: Value):
        self.module_inst.tableaddrs = [2, 1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 10 + i) for i in range(3)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 20 + i) for i in range(3)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 30 + i) for i in range(3)],
            )
        )

        self.machine.push(Value(ValueType.I32, elemidx))

        insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_GET, tableidx))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_table_get_traps_on_out_of_bounds(self):
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )

        self.machine.push(Value(ValueType.I32, 3))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_GET, 0))

    @parameterized.named_parameters(
        ("table 0 elem 0", 0, 0, 2),
        ("table 0 elem 1", 0, 1, 2),
        ("table 1 elem 0", 1, 0, 1),
        ("table 1 elem 1", 1, 1, 1),
    )
    def test_table_set(self, tableidx: int, elemidx: int, expected_tableaddr: int):
        self.module_inst.tableaddrs = [2, 1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 10 + i) for i in range(3)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 20 + i) for i in range(3)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 30 + i) for i in range(3)],
            )
        )

        self.machine.push(Value(ValueType.I32, elemidx))
        self.machine.push(Value(ValueType.FUNCREF, None))

        insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_SET, tableidx))

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(
            self.machine.tables[expected_tableaddr].refs[elemidx],
            Value(ValueType.FUNCREF, None),
        )
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_table_set_traps_on_out_of_bounds(self):
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )

        self.machine.push(Value(ValueType.I32, 3))
        self.machine.push(Value(ValueType.FUNCREF, None))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_SET, 0))

    @parameterized.named_parameters(
        ("elem 0[0] -> table 0[0] n 3", 0, 0, 0, 0, 3, 2, [20, 21, 22, None, None]),
        ("elem 0[0] -> table 0[1] n 3", 0, 0, 0, 1, 3, 2, [None, 20, 21, 22, None]),
        ("elem 0[1] -> table 0[0] n 3", 0, 1, 0, 0, 3, 2, [21, 22, 23, None, None]),
        ("elem 0[1] -> table 0[1] n 3", 0, 1, 0, 1, 3, 2, [None, 21, 22, 23, None]),
        ("elem 0[0] -> table 1[0] n 3", 0, 0, 1, 0, 3, 1, [20, 21, 22, None, None]),
        ("elem 0[0] -> table 1[1] n 3", 0, 0, 1, 1, 3, 1, [None, 20, 21, 22, None]),
        ("elem 0[1] -> table 1[0] n 3", 0, 1, 1, 0, 3, 1, [21, 22, 23, None, None]),
        ("elem 0[1] -> table 1[1] n 3", 0, 1, 1, 1, 3, 1, [None, 21, 22, 23, None]),
        ("elem 1[0] -> table 0[0] n 3", 1, 0, 0, 0, 3, 2, [30, 31, 32, None, None]),
        ("elem 1[0] -> table 0[1] n 3", 1, 0, 0, 1, 3, 2, [None, 30, 31, 32, None]),
        ("elem 1[1] -> table 0[0] n 3", 1, 1, 0, 0, 3, 2, [31, 32, 33, None, None]),
        ("elem 1[1] -> table 0[1] n 3", 1, 1, 0, 1, 3, 2, [None, 31, 32, 33, None]),
        ("elem 1[1] -> table 0[1] n 2", 1, 1, 0, 1, 2, 2, [None, 31, 32, None, None]),
        (
            "elem 1[1] -> table 0[1] n 0",
            1,
            1,
            0,
            1,
            0,
            2,
            [None, None, None, None, None],
        ),
    )
    def test_table_init(
        self,
        elemidx: int,
        s: int,
        tableidx: int,
        d: int,
        n: int,
        expected_tableaddr: int,
        expected_content: list[int | None],
    ):
        self.module_inst.tableaddrs = [2, 1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.module_inst.elementaddrs = [1, 2, 0]
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, 10 + i) for i in range(5)],
            )
        )
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, 20 + i) for i in range(5)],
            )
        )
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, 30 + i) for i in range(5)],
            )
        )

        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, n))

        insn_eval.eval_insn(
            self.machine, op2(InstructionType.TABLE_INIT, tableidx, elemidx)
        )

        self.assertStackDepth(self.starting_stack_depth)
        expected = [Value(ValueType.FUNCREF, i) for i in expected_content]
        self.assertEqual(self.machine.tables[expected_tableaddr].refs, expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("elem 0[0] -> table 0[5] n 3", 0, 0, 0, 1, 3),
        ("elem 0[1] -> table 0[0] n 3", 0, 1, 0, 0, 3),
    )
    def test_table_init_traps_on_out_of_bounds(
        self,
        elemidx: int,
        s: int,
        tableidx: int,
        d: int,
        n: int,
    ):
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.module_inst.elementaddrs = [0]
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, 10 + i) for i in range(2)],
            )
        )

        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, n))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(
                self.machine, op2(InstructionType.TABLE_INIT, tableidx, elemidx)
            )

    def test_elem_drop(self):
        self.module_inst.elementaddrs = [2, 1, 0]
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )
        self.machine.add_element(
            ElementSegmentInstance(
                ValueType.FUNCREF,
                refs=[Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )

        insn_eval.eval_insn(self.machine, op1(InstructionType.ELEM_DROP, 2))

        self.assertIsNone(self.machine.element_segments[0])
        self.assertIsNotNone(self.machine.element_segments[1])
        self.assertIsNotNone(self.machine.element_segments[2])

    @parameterized.named_parameters(
        ("0[0] -> 0[1], n 3", 0, 0, 0, 1, 3, 1, [20, 20, 21, 22, 24]),
        ("0[1] -> 0[0], n 3", 0, 1, 0, 0, 3, 1, [21, 22, 23, 23, 24]),
        ("0[1] -> 0[0], n 0", 0, 1, 0, 0, 0, 1, [20, 21, 22, 23, 24]),
        ("0[0] -> 1[1], n 3", 0, 0, 1, 1, 3, 0, [10, 20, 21, 22, 14]),
        ("1[1] -> 0[0], n 3", 1, 1, 0, 0, 3, 1, [11, 12, 13, 23, 24]),
    )
    def test_table_copy(
        self,
        stableidx: int,
        s: int,
        dtableidx: int,
        d: int,
        n: int,
        expected_tableidx: int,
        expected_content: list[int],
    ):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 10 + i) for i in range(5)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, 20 + i) for i in range(5)],
            )
        )

        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, n))

        insn_eval.eval_insn(
            self.machine, op2(InstructionType.TABLE_COPY, dtableidx, stableidx)
        )

        self.assertEqual(
            self.machine.tables[expected_tableidx].refs,
            [Value(ValueType.FUNCREF, v) for v in expected_content],
        )
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("0[0] -> 0[3], n 3", 0, 0, 0, 3, 3),
        ("0[3] -> 0[0], n 3", 0, 3, 0, 0, 3),
        ("0[0] -> 1[1], n 6", 0, 0, 1, 1, 6),
        ("1[5] -> 0[0], n 6", 1, 5, 0, 0, 6),
    )
    def test_table_copy_traps_on_out_of_bounds(
        self,
        stableidx: int,
        s: int,
        dtableidx: int,
        d: int,
        n: int,
    ):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(10)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )

        self.machine.push(Value(ValueType.I32, d))
        self.machine.push(Value(ValueType.I32, s))
        self.machine.push(Value(ValueType.I32, n))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(
                self.machine, op2(InstructionType.TABLE_COPY, dtableidx, stableidx)
            )

    @parameterized.named_parameters(
        ("table 0", 0, 10),
        ("table 1", 1, 5),
    )
    def test_table_size(self, tableidx: int, expected: int):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(10)],
            )
        )

        insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_SIZE, tableidx))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected))
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        (
            "0[0:0+3]",
            0,
            0,
            3,
            Value(ValueType.FUNCREF, 30),
            1,
            [30, 30, 30, None, None],
        ),
        (
            "1[2:2+2]",
            1,
            2,
            2,
            Value(ValueType.FUNCREF, 40),
            0,
            [None, None, 40, 40, None],
        ),
        (
            "zero size",
            1,
            2,
            0,
            Value(ValueType.FUNCREF, 40),
            0,
            [None, None, None, None, None],
        ),
    )
    def test_table_fill(
        self,
        tableidx: int,
        i: int,
        n: int,
        v: Value,
        expected_tableidx: int,
        expected_content: list[int | None],
    ):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )

        self.machine.push(Value(ValueType.I32, i))
        self.machine.push(v)
        self.machine.push(Value(ValueType.I32, n))

        insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_FILL, tableidx))

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(
            self.machine.tables[expected_tableidx].refs,
            [Value(ValueType.FUNCREF, v) for v in expected_content],
        )
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("0[3], n 3", 0, 3, 3),
        ("0[0], n 6", 0, 0, 6),
        ("1[5], n 6", 1, 5, 6),
    )
    def test_table_fill_traps_on_out_of_bounds(
        self,
        tableidx: int,
        i: int,
        n: int,
    ):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(10)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(0)),
                [Value(ValueType.FUNCREF, None) for _ in range(5)],
            )
        )

        self.machine.push(Value(ValueType.I32, i))
        self.machine.push(Value(ValueType.FUNCREF, None))
        self.machine.push(Value(ValueType.I32, n))

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_FILL, tableidx))

    @parameterized.named_parameters(
        (
            "grow 0 by 2",
            0,
            2,
            Value(ValueType.FUNCREF, 1),
            1,
            3,
            [None, None, None, 1, 1],
        ),
        ("grow 1 by 2", 1, 2, Value(ValueType.FUNCREF, 1), 0, 1, [None, 1, 1]),
        ("grow 1 by 0", 1, 0, Value(ValueType.FUNCREF, 1), 0, 1, [None]),
        ("grow 1 by 20", 1, 20, Value(ValueType.FUNCREF, 1), 0, 0xFFFFFFFF, [None]),
    )
    def test_table_grow(
        self,
        tableidx: int,
        n: int,
        v: Value,
        expected_tableidx: int,
        expected_old_size: int,
        expected_content: list[int | None],
    ):
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(1, 10)),
                [Value(ValueType.FUNCREF, None) for _ in range(1)],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(3, 20)),
                [Value(ValueType.FUNCREF, None) for _ in range(3)],
            )
        )

        self.machine.push(v)
        self.machine.push(Value(ValueType.I32, n))

        insn_eval.eval_insn(self.machine, op1(InstructionType.TABLE_GROW, tableidx))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_old_size))
        self.assertEqual(
            self.machine.tables[expected_tableidx].refs,
            [Value(ValueType.FUNCREF, v) for v in expected_content],
        )
        self.assertEqual(
            self.machine.tables[expected_tableidx].table_type.limits.min,
            len(expected_content),
        )
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("global.get 0", 0, Value(ValueType.I32, 1)),
        ("global.get 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_global_get(self, globalidx: int, expected: Value):
        self.module_inst.globaladdrs = [2, 1, 0]
        self.machine.global_vars = [
            GlobalInstance(
                GlobalType(ValueType.I32, mutable=True), [], Value(ValueType.I32, 0)
            ),
            GlobalInstance(
                GlobalType(ValueType.F32, mutable=True), [], Value(ValueType.F32, 2.2)
            ),
            GlobalInstance(
                GlobalType(ValueType.I32, mutable=True), [], Value(ValueType.I32, 1)
            ),
        ]
        insn_eval.eval_insn(self.machine, global_get(globalidx))

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("global.set 0", 0, Value(ValueType.I32, 1)),
        ("global.set 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_global_set(self, globalidx: int, expected: Value):
        self.module_inst.globaladdrs = [2, 1, 0]
        self.machine.global_vars = [
            GlobalInstance(
                GlobalType(ValueType.I32, mutable=True), [], Value(ValueType.I32, 0)
            ),
            GlobalInstance(
                GlobalType(ValueType.I32, mutable=True), [], Value(ValueType.I32, 0)
            ),
            GlobalInstance(
                GlobalType(ValueType.I32, mutable=True), [], Value(ValueType.I32, 0)
            ),
        ]
        self.machine.push(expected)

        insn_eval.eval_insn(self.machine, global_set(globalidx))

        self.assertEqual(
            self.machine.global_vars[self.module_inst.globaladdrs[globalidx]].value,
            expected,
        )
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("local.get 0", 0, Value(ValueType.I32, 1)),
        ("local.get 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_get(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 1), Value(ValueType.F32, 2.2)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, -1))

        insn_eval.eval_insn(self.machine, local_get(localidx))

        self.assertStackDepth(self.starting_stack_depth + 2)
        self.assertEqual(self.machine.pop(), expected)
        self.assertIsInstance(self.machine.pop(), Frame)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("const 1, local.set 0", 0, Value(ValueType.I32, 1)),
        ("f32.const 2.2, local.set 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_set(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 0), Value(ValueType.F32, 0)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, -1))
        self.machine.push(expected)

        insn_eval.eval_insn(self.machine, local_set(localidx))

        self.assertEqual(local_vars[localidx], expected)
        self.assertIsInstance(self.machine.pop(), Frame)
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    @parameterized.named_parameters(
        ("const 1, local.tee 0", 0, Value(ValueType.I32, 1)),
        ("f32.const 2.2, local.tee 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_tee(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 0), Value(ValueType.F32, 0)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, -1))
        self.machine.push(expected)

        insn_eval.eval_insn(self.machine, local_tee(localidx))

        self.assertEqual(local_vars[localidx], expected)
        self.assertEqual(self.machine.pop(), expected)
        self.assertIsInstance(self.machine.pop(), Frame)
        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 1)

    def test_invoke_void_func_falls_off_end(self):
        func = ModuleFuncInstance(
            FuncType([], []),
            self.module_inst,
            [],
            [noarg(InstructionType.NOP)],
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth)

    def test_invoke_func_falls_off_end(self):
        func = ModuleFuncInstance(
            FuncType([], [ValueType.I32, ValueType.I32]),
            self.module_inst,
            [],
            [i32_const(1), i32_const(2)],
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 2)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 2))
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_invoke_void_func_with_return(self):
        func = ModuleFuncInstance(
            FuncType([], []),
            self.module_inst,
            [],
            [
                noarg(InstructionType.NOP),
                noarg(InstructionType.RETURN),
                noarg(InstructionType.NOP),
            ],
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth)

    def test_invoke_func_with_return(self):
        funcaddr = self._add_i32_func(
            i32_const(1),
            noarg(InstructionType.RETURN),
            noarg(InstructionType.DROP),
            i32_const(2),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_invoke_void_func_with_return_using_br(self):
        func = ModuleFuncInstance(
            FuncType([], []),
            self.module_inst,
            [],
            [noarg(InstructionType.NOP), br(0), noarg(InstructionType.NOP)],
        )
        func.body = flatten_instructions(func.body, 0)
        funcaddr = self.machine.add_func(func)
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth)

    def test_invoke_func_with_return_using_br(self):
        funcaddr = self._add_i32_func(
            i32_const(1), br(0), noarg(InstructionType.DROP), i32_const(2)
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    @parameterized.named_parameters(
        ("1 < 2 is True", 1, 2, Value(ValueType.I32, 1)),
        ("2 < 1 is False", 2, 1, Value(ValueType.I32, 0)),
        ("1 < 1 is False", 1, 1, Value(ValueType.I32, 0)),
        ("-2 < -1 unsigned is True", -2, -1, Value(ValueType.I32, 1)),
        ("-1 < -2 unsigned is False", -1, -2, Value(ValueType.I32, 0)),
        ("-1 < 1 unsigned is False", -1, 1, Value(ValueType.I32, 0)),
        ("1 < -1 unsigned is True", 1, -1, Value(ValueType.I32, 1)),
    )
    def test_i32_lt_u(self, c1: int, c2: int, expected: Value):
        funcaddr = self._add_i32_func(
            i32_const(c1),
            i32_const(c2),
            noarg(InstructionType.I32_LT_U),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        ("[5,6] select 0 = 6", 0, 5, 6, Value(ValueType.I32, 6)),
        ("[5,6] select 1 = 5", 1, 5, 6, Value(ValueType.I32, 5)),
    )
    def test_select(self, c: int, v1: int, v2: int, expected: Value):
        funcaddr = self._add_i32_func(
            i32_const(v1),
            i32_const(v2),
            i32_const(c),
            noarg(InstructionType.SELECT),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    def test_block_end(self):
        funcaddr = self._add_i32_func(i32_block(i32_const(1)))
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_return(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_const(1),
                noarg(InstructionType.RETURN),
            ),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_nested_block_ends(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                ),
            ),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_nested_block_returns(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    noarg(InstructionType.RETURN),
                ),
                noarg(InstructionType.DROP),
                i32_const(2),
            ),
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_br_0_skips_own_block(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_const(1),
                br(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            ),
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_block_br_1_skips_parent_block(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    br(1),
                    noarg(InstructionType.DROP),
                    i32_const(2),
                ),
                i32_const(2),
                noarg(InstructionType.I32_ADD),
            ),
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_block_br_0_continues_parent_block(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    br(0),
                    noarg(InstructionType.DROP),
                    i32_const(2),
                ),
                i32_const(2),
                noarg(InstructionType.I32_ADD),
            ),
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 13))

    def test_br_if_0(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_const(1),
                i32_const(0),
                br_if(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 2))

    def test_br_if_1(self):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_const(1),
                i32_const(1),
                br_if(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    @parameterized.named_parameters(
        ("0", 0, 1),
        ("1", 1, 2),
        ("default", 2, 3),
    )
    def test_br_table(self, idx: int, expected_result: int):
        funcaddr = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_block(
                        i32_const(0),
                        i32_const(idx),
                        br_table(0, 1, 2),
                    ),
                    i32_const(1),
                    noarg(InstructionType.RETURN),
                ),
                i32_const(2),
                noarg(InstructionType.RETURN),
            ),
            i32_const(3),
            noarg(InstructionType.RETURN),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    def test_if_false_skips_to_end(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_void(nop())], 100)
        # Flattens out to:
        #  100: if
        #  101:   nop
        #  102: end
        self.machine.push(Value(ValueType.I32, 0))

        insn_eval.eval_insn(self.machine, insn[0])

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 103)

    def test_if_true_starts_block_and_continues(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_void(nop())], 100)
        # Flattens out to:
        #  100: if
        #  101:   nop
        #  102: end
        print(insn[0])
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.eval_insn(self.machine, insn[0])

        self.assertStackDepth(self.starting_stack_depth + 1)  # The label
        self.assertEqual(self.machine.peek(), Label(0, 103))
        self.assertEqual(self.machine.get_current_frame().pc, 101)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute nop

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute end

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 103)

    def test_if_true_starts_block_and_br0_ends(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_void(br(0), nop())], 100)
        # Flattens out to:
        #  100: if
        #  101:   br 0
        #  102:   nop
        #  103: end
        print(insn[0])
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.eval_insn(self.machine, insn[0])
        self.assertEqual(self.machine.get_current_frame().pc, 101)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute br 0

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 104)

    def test_if_else_false_starts_block_and_skips_to_else(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_else_void([nop()], [nop()])], 100)
        # Flattens out to:
        #  100: if
        #  101:   nop
        #  102: else
        #  103:   nop
        #  104: end
        self.machine.push(Value(ValueType.I32, 0))

        insn_eval.eval_insn(self.machine, insn[0])

        self.assertStackDepth(self.starting_stack_depth + 1)  # The label
        self.assertEqual(self.machine.peek(), Label(0, 105))
        self.assertEqual(self.machine.get_current_frame().pc, 103)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute nop

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute end

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 105)

    def test_if_else_false_starts_block_and_br0_ends(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_else_void([nop()], [br(0), nop()])], 100)
        # Flattens out to:
        #  100: if
        #  101:   nop
        #  102: else
        #  103:   br 0
        #  104:   nop
        #  105: end
        self.machine.push(Value(ValueType.I32, 0))

        insn_eval.eval_insn(self.machine, insn[0])
        self.assertEqual(self.machine.get_current_frame().pc, 103)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute br 0

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 106)

    def test_if_else_true_starts_block_and_continues(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_else_void([nop()], [nop()])], 100)
        # Flattens out to:
        #  100: if
        #  101:   nop
        #  102: else
        #  103:   nop
        #  104: end
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.eval_insn(self.machine, insn[0])

        self.assertStackDepth(self.starting_stack_depth + 1)  # The label
        self.assertEqual(self.machine.peek(), Label(0, 105))
        self.assertEqual(self.machine.get_current_frame().pc, 101)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute nop

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute else

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 105)

    def test_if_else_true_starts_block_and_br0_ends(self):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_else_void([br(0), nop()], [nop()])], 100)
        # Flattens out to:
        #  100: if
        #  101:   br 0
        #  102:   nop
        #  103: else
        #  104:   nop
        #  105: end
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.eval_insn(self.machine, insn[0])
        self.assertEqual(self.machine.get_current_frame().pc, 101)

        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )  # Execute br 0

        self.assertStackDepth(self.starting_stack_depth)
        self.assertEqual(self.machine.get_current_frame().pc, 106)

    @parameterized.named_parameters(
        ("False", 0, 1),
        ("True", 2, 2),
    )
    def test_if(self, val: int, expected_result: int):
        funcaddr = self._add_i32_func(
            i32_const(val),
            if_(
                i32_const(2),
                noarg(InstructionType.RETURN),
            ),
            i32_const(1),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    @parameterized.named_parameters(
        ("False", 0, 2),
        ("True", 2, 3),
    )
    def test_if_else_i32(self, cond: int, expected_result: int):
        self.machine.get_current_frame().pc = 100
        insn = flatten_instructions([if_else_i32([i32_const(3)], [i32_const(2)])], 100)
        # Flattens out to:
        #  100: if [i32]
        #  101:   i32.const 3
        #  102: else
        #  103:   i32.const 2
        #  104: end
        self.machine.push(Value(ValueType.I32, cond))

        insn_eval.eval_insn(self.machine, insn[0])
        # Execute two instructions
        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )
        insn_eval.eval_insn(
            self.machine, insn[self.machine.get_current_frame().pc - 100]
        )

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    def test_loop_one_iterations(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        funcaddr = self._add_i32_func(
            i32_const(1),
            i32_loop(
                func_type_idx,
                i32_const(2),
                noarg(InstructionType.I32_ADD),
            ),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_return(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        funcaddr = self._add_i32_func(
            i32_const(1),
            i32_loop(
                func_type_idx,
                i32_const(2),
                noarg(InstructionType.I32_ADD),
                noarg(InstructionType.RETURN),
            ),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_br_1(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        funcaddr = self._add_i32_func(
            i32_block(
                i32_const(1),
                i32_loop(
                    func_type_idx,
                    i32_const(2),
                    noarg(InstructionType.I32_ADD),
                    br(1),
                ),
            )
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_continue(self):
        func_type_idx = self.add_func_type([], [])
        funcaddr = self._add_i32_func(
            i32_const(1),
            local_set(0),
            i32_loop(
                func_type_idx,
                local_get(0),
                i32_const(2),
                noarg(InstructionType.I32_ADD),
                local_tee(0),
                i32_const(10),
                noarg(InstructionType.I32_LT_U),
                br_if(0),
            ),
            local_get(0),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_loop_continue_from_inner_block(self):
        func_type_idx = self.add_func_type([], [])
        funcaddr = self._add_i32_func(
            i32_const(1),
            local_set(0),
            i32_loop(
                func_type_idx,
                local_get(0),
                i32_const(2),
                noarg(InstructionType.I32_ADD),
                local_set(0),
                void_block(
                    local_get(0),
                    i32_const(10),
                    noarg(InstructionType.I32_LT_U),
                    br_if(1),
                ),
            ),
            local_get(0),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_call(self):
        funcaddr1 = self._add_i32_func(i32_const(1))
        # Calling in this way ensures that we pick up where we left off even if
        # we fall off the end of funcaddr1 instead of returning.
        funcaddr = self._add_i32_func(
            i32_const(2),
            call(funcaddr1),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_call_and_return(self):
        funcaddr1 = self._add_i32_func(i32_const(1), noarg(InstructionType.RETURN))
        funcaddr = self._add_i32_func(
            i32_const(2),
            call(funcaddr1),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_call_args(self):
        funcaddr1 = self._add_i32_i32_func(
            local_get(0),
            i32_const(1),
            noarg(InstructionType.I32_ADD),
        )
        funcaddr = self._add_i32_func(
            i32_const(2),
            call(funcaddr1),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_call_locals_are_local(self):
        funcaddr1 = self._add_i32_i32_func(
            i32_const(1000),
            local_set(1),
            local_get(0),
            i32_const(1),
            noarg(InstructionType.I32_ADD),
        )
        funcaddr = self._add_i32_func(
            i32_const(2),
            call(funcaddr1),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_call_args_ordered_correctly(self):
        funcaddr1 = self._add_i32_i32_i32_func(
            local_get(0),
            local_get(1),
            noarg(InstructionType.I32_SUB),
        )
        funcaddr = self._add_i32_func(
            i32_const(3),
            i32_const(2),
            call(funcaddr1),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    @parameterized.named_parameters(
        ("#0", 0, 11),
        ("#1", 1, 12),
    )
    def test_call_indirect(self, funcidx: int, expected: int):
        self.module_inst.func_types = [FuncType([], []), FuncType([], [ValueType.I32])]
        funcaddr1 = self._add_i32_func(i32_const(1))
        funcaddr2 = self._add_i32_func(i32_const(2))
        self.module_inst.tableaddrs = [1, 0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(2)),
                [
                    Value(ValueType.FUNCREF, None),
                    Value(ValueType.FUNCREF, None),
                ],
            )
        )
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(2)),
                [
                    Value(ValueType.FUNCREF, funcaddr1),
                    Value(ValueType.FUNCREF, funcaddr2),
                ],
            )
        )
        funcaddr = self._add_i32_func(
            i32_const(funcidx),
            op2(InstructionType.CALL_INDIRECT, 1, 0),  # table 0, functype 1
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(funcaddr)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected))

    def test_call_indirect_traps_on_index_out_of_bounds(self):
        self.module_inst.func_types = [FuncType([], []), FuncType([], [ValueType.I32])]
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(2)),
                [
                    Value(ValueType.FUNCREF, None),
                    Value(ValueType.FUNCREF, None),
                ],
            )
        )
        funcaddr = self._add_i32_func(
            i32_const(2),
            op2(InstructionType.CALL_INDIRECT, 1, 0),  # table 0, functype 1
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )

        with self.assertRaisesRegex(RuntimeError, "out of bounds"):
            self.machine.invoke_func(funcaddr)

    def test_call_indirect_traps_on_null_ref(self):
        self.module_inst.func_types = [FuncType([], []), FuncType([], [ValueType.I32])]
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(2)),
                [
                    Value(ValueType.FUNCREF, None),
                    Value(ValueType.FUNCREF, None),
                ],
            )
        )
        funcaddr = self._add_i32_func(
            i32_const(1),
            op2(InstructionType.CALL_INDIRECT, 1, 0),  # table 0, functype 1
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )

        with self.assertRaisesRegex(RuntimeError, "null reference"):
            self.machine.invoke_func(funcaddr)

    def test_call_indirect_traps_on_type_mismatch(self):
        self.module_inst.func_types = [FuncType([], []), FuncType([], [ValueType.I32])]
        funcaddr1 = self._add_i32_func(i32_const(1))
        self.module_inst.tableaddrs = [0]
        self.machine.add_table(
            TableInstance(
                TableType(ValueType.FUNCREF, Limits(2)),
                [
                    Value(ValueType.FUNCREF, funcaddr1),
                    Value(ValueType.FUNCREF, funcaddr1),
                ],
            )
        )
        funcaddr = self._add_i32_func(
            i32_const(1),
            op2(InstructionType.CALL_INDIRECT, 0, 0),  # table 0, functype 0
            i32_const(10),
            noarg(InstructionType.I32_ADD),
        )

        with self.assertRaisesRegex(RuntimeError, "type mismatch"):
            self.machine.invoke_func(funcaddr)


if __name__ == "__main__":
    absltest.main()
