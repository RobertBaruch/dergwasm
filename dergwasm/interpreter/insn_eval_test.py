"""Unit tests for insn_eval.py."""

from absl.testing import absltest, parameterized

# from absl import flags

from dergwasm.interpreter.binary import FuncType, Module, flatten_instructions
from dergwasm.interpreter import machine_impl
from dergwasm.interpreter import module_instance
from dergwasm.interpreter import insn_eval
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter.values import Value, Frame, ValueType
from dergwasm.interpreter.machine import ModuleFuncInstance
from dergwasm.interpreter.testing.util import (
    call,
    data_drop,
    i32_const,
    i64_const,
    i64_load,
    memory_init,
    br,
    br_table,
    br_if,
    i32_block,
    i32_loop,
    if_,
    if_else,
    local_get,
    local_set,
    local_tee,
    void_block,
    i32_load,
    noarg,
)


class InsnEvalTest(parameterized.TestCase):
    machine: machine_impl.MachineImpl
    module: Module
    module_inst: module_instance.ModuleInstance
    starting_stack_depth: int

    def _add_i32_func(self, *instructions: Instruction) -> int:
        """Add an i32 function to the machine that takes no arguments.

        There is also one i32 local var.
        """
        func = ModuleFuncInstance(
            FuncType([], [ValueType.I32]),
            self.module_inst,
            [ValueType.I32],
            list(instructions),
        )
        func.body = flatten_instructions(func.body, 0)
        funcidx = self.machine.add_func(func)
        self.module_inst.funcaddrs.append(funcidx)
        return funcidx

    def _add_i32_i32_func(self, *instructions: Instruction) -> int:
        """Add an i32 function to the machine that takes an i32 argument.

        There is also one i32 local var.
        """
        func = ModuleFuncInstance(
            FuncType([ValueType.I32], [ValueType.I32]),
            self.module_inst,
            [ValueType.I32],
            list(instructions),
        )
        func.body = flatten_instructions(func.body, 0)
        funcidx = self.machine.add_func(func)
        self.module_inst.funcaddrs.append(funcidx)
        return funcidx

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
        self.machine.add_data(b"foo")
        self.machine.add_data(b"bar")
        self.machine.add_data(b"baz")
        self.module_inst.dataaddrs = [2, 1, 0]
        self.machine.add_mem(bytearray(65536))
        self.module_inst.memaddrs = [0]
        self.machine.new_frame(Frame(0, [], self.module_inst, 0))
        self.starting_stack_depth = self._stack_depth()

    def test_nop(self):
        insn_eval.nop(self.machine, noarg(InstructionType.NOP))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_const(self):
        insn_eval.i32_const(self.machine, i32_const(42))
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 42))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i64_const(self):
        insn_eval.i64_const(self.machine, i64_const(42))
        self.assertEqual(self.machine.pop(), Value(ValueType.I64, 42))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_add(self):
        self.machine.push(Value(ValueType.I32, 2))
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.i32_add(self.machine, noarg(InstructionType.I32_ADD))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_add_mod(self):
        self.machine.push(Value(ValueType.I32, 0xFEDC0000))
        self.machine.push(Value(ValueType.I32, 0x56780000))

        insn_eval.i32_add(self.machine, noarg(InstructionType.I32_ADD))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 0x55540000))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_mul(self):
        self.machine.push(Value(ValueType.I32, 2))
        self.machine.push(Value(ValueType.I32, 3))

        insn_eval.i32_mul(self.machine, noarg(InstructionType.I32_MUL))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 6))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_mul_mod(self):
        self.machine.push(Value(ValueType.I32, 0xFEDC1234))
        self.machine.push(Value(ValueType.I32, 0x56789ABC))

        insn_eval.i32_mul(self.machine, noarg(InstructionType.I32_MUL))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 0x8CF0A630))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_sub(self):
        self.machine.push(Value(ValueType.I32, 2))
        self.machine.push(Value(ValueType.I32, 1))

        insn_eval.i32_sub(self.machine, noarg(InstructionType.I32_SUB))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_sub_mod(self):
        self.machine.push(Value(ValueType.I32, 0x00001234))
        self.machine.push(Value(ValueType.I32, 0x56789ABC))

        insn_eval.i32_sub(self.machine, noarg(InstructionType.I32_SUB))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 0xA9877778))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    @parameterized.named_parameters(
        ("i32.const 0, i32.load 0", 0, 0, 0x03020100),
        ("i32.const 1, i32.load 0", 1, 0, 0x04030201),
        ("i32.const 0, i32.load 1", 0, 1, 0x04030201),
        ("i32.const 1, i32.load 1", 1, 1, 0x05040302),
    )
    def test_i32_load(self, base: int, offset: int, expected: int):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.get_mem(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        insn_eval.i32_load(self.machine, i32_load(4, offset))

        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i32_load_raises_on_access_out_of_bounds(self):
        self.machine.push(Value(ValueType.I32, 65535))

        with self.assertRaisesRegex(RuntimeError, "i32.load: access out of bounds"):
            insn_eval.i32_load(self.machine, i32_load(4, 0))

    @parameterized.named_parameters(
        ("i32.const 0, i64.load 0", 0, 0, 0x0706050403020100),
        ("i32.const 1, i64.load 0", 1, 0, 0x0807060504030201),
        ("i32.const 0, i64.load 1", 0, 1, 0x0807060504030201),
        ("i32.const 1, i64.load 1", 1, 1, 0x0908070605040302),
    )
    def test_i64_load(self, base: int, offset: int, expected: int):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.get_mem(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        insn_eval.i64_load(self.machine, i64_load(4, offset))

        self.assertEqual(self.machine.pop(), Value(ValueType.I64, expected))
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_i64_load_raises_on_access_out_of_bounds(self):
        self.machine.push(Value(ValueType.I32, 65535))

        with self.assertRaisesRegex(RuntimeError, "i64.load: access out of bounds"):
            insn_eval.i64_load(self.machine, i64_load(4, 0))

    @parameterized.named_parameters(
        (
            "i32.store 0x12345678 to base 0, offset 0",
            InstructionType.I32_STORE,
            0,
            0,
            b"\x78\x56\x34\x12\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store 0x12345678 to base 1, offset 0",
            InstructionType.I32_STORE,
            1,
            0,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store 0x12345678 to base 0, offset 1",
            InstructionType.I32_STORE,
            0,
            1,
            b"\x00\x78\x56\x34\x12\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store 0x12345678 to base 1, offset 1",
            InstructionType.I32_STORE,
            1,
            1,
            b"\x00\x01\x78\x56\x34\x12\x06\x07\x08\x09",
        ),
        (
            "i32.store16 0x12345678 to base 0, offset 0",
            InstructionType.I32_STORE16,
            0,
            0,
            b"\x78\x56\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store16 0x12345678 to base 1, offset 0",
            InstructionType.I32_STORE16,
            1,
            0,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store16 0x12345678 to base 0, offset 1",
            InstructionType.I32_STORE16,
            0,
            1,
            b"\x00\x78\x56\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store16 0x12345678 to base 1, offset 1",
            InstructionType.I32_STORE16,
            1,
            1,
            b"\x00\x01\x78\x56\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store8 0x12345678 to base 0, offset 0",
            InstructionType.I32_STORE8,
            0,
            0,
            b"\x78\x01\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store8 0x12345678 to base 1, offset 0",
            InstructionType.I32_STORE8,
            1,
            0,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store8 0x12345678 to base 0, offset 1",
            InstructionType.I32_STORE8,
            0,
            1,
            b"\x00\x78\x02\x03\x04\x05\x06\x07\x08\x09",
        ),
        (
            "i32.store8 0x12345678 to base 1, offset 1",
            InstructionType.I32_STORE8,
            1,
            1,
            b"\x00\x01\x78\x03\x04\x05\x06\x07\x08\x09",
        ),
    )
    def test_i32_store(self, insn_type: InstructionType, base: int, offset: int, expected: bytes):
        self.machine.push(Value(ValueType.I32, base))
        self.machine.push(Value(ValueType.I32, 0x12345678))
        self.machine.get_mem(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        instruction = Instruction(insn_type, [4, offset], 0, 0)

        insn_eval.eval_insn(self.machine, instruction)

        self.assertEqual(self.machine.get_mem(0)[0:10], expected)
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    @parameterized.named_parameters(
        ("i32.store", InstructionType.I32_STORE),
        ("i32.store16", InstructionType.I32_STORE16),
        ("i32.store8", InstructionType.I32_STORE8),
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
        self.machine.get_mem(0)[0:10] = b"\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09"

        instruction = Instruction(insn_type, [4, offset], 0, 0)

        insn_eval.eval_insn(self.machine, instruction)

        self.assertEqual(self.machine.get_mem(0)[0:10], expected)
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    @parameterized.named_parameters(
        ("i64.store", InstructionType.I64_STORE),
        ("i64.store32", InstructionType.I64_STORE32),
        ("i64.store16", InstructionType.I64_STORE16),
        ("i64.store8", InstructionType.I64_STORE8),
    )
    def test_i64_store_raises_on_access_out_of_bounds(self, insn_type: InstructionType):
        self.machine.push(Value(ValueType.I32, 65536))
        self.machine.push(Value(ValueType.I64, 0xDDCCBBAA12345678))

        instruction = Instruction(insn_type, [4, 0], 0, 0)

        with self.assertRaisesRegex(RuntimeError, "access out of bounds"):
            insn_eval.eval_insn(self.machine, instruction)

    def test_memory_init(self):
        self.machine.push(Value(ValueType.I32, 2))  # dest offset
        self.machine.push(Value(ValueType.I32, 1))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        insn_eval.memory_init(self.machine, memory_init(1, 0))

        self.assertEqual(self.machine.get_mem(0)[0:5], b"\x00\x00ar\x00")
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_memory_init_traps_source_out_of_bounds(self):
        self.machine.push(Value(ValueType.I32, 1))  # dest offset
        self.machine.push(Value(ValueType.I32, 60))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        with self.assertRaisesRegex(RuntimeError, "source is out of bounds"):
            insn_eval.memory_init(self.machine, memory_init(1, 0))

    def test_memory_init_traps_dest_out_of_bounds(self):
        self.machine.push(Value(ValueType.I32, 65535))  # dest offset
        self.machine.push(Value(ValueType.I32, 1))  # source offset
        self.machine.push(Value(ValueType.I32, 2))  # data size

        with self.assertRaisesRegex(RuntimeError, "destination is out of bounds"):
            insn_eval.memory_init(self.machine, memory_init(1, 0))

    def test_data_drop(self):
        insn_eval.data_drop(self.machine, data_drop(2))

        self.assertEqual(self.machine.datas[0], b"")
        self.assertEqual(self.machine.datas[1], b"bar")
        self.assertEqual(self.machine.datas[2], b"baz")
        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    @parameterized.named_parameters(
        ("local.get 0", 0, Value(ValueType.I32, 1)),
        ("local.get 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_get(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 1), Value(ValueType.F32, 2.2)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, 0))

        insn_eval.local_get(self.machine, local_get(localidx))

        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)

    @parameterized.named_parameters(
        ("i32.const 1, local.set 0", 0, Value(ValueType.I32, 1)),
        ("f32.const 2.2, local.set 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_set(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 0), Value(ValueType.F32, 0)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, 0))
        self.machine.push(expected)

        insn_eval.local_set(self.machine, local_set(localidx))

        self.assertEqual(local_vars[localidx], expected)
        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)

    @parameterized.named_parameters(
        ("i32.const 1, local.tee 0", 0, Value(ValueType.I32, 1)),
        ("f32.const 2.2, local.tee 1", 1, Value(ValueType.F32, 2.2)),
    )
    def test_local_tee(self, localidx: int, expected: Value):
        local_vars = [Value(ValueType.I32, 0), Value(ValueType.F32, 0)]
        self.machine.new_frame(Frame(0, local_vars, self.module_inst, 0))
        self.machine.push(expected)

        insn_eval.local_tee(self.machine, local_tee(localidx))

        self.assertEqual(local_vars[localidx], expected)
        self.assertEqual(self.machine.pop(), expected)
        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)

    def test_invoke_void_func_falls_off_end(self):
        func = ModuleFuncInstance(
            FuncType([], []),
            self.module_inst,
            [],
            [noarg(InstructionType.NOP)],
        )
        func.body = flatten_instructions(func.body, 0)
        func_idx = self.machine.add_func(func)
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth)

    def test_invoke_func_falls_off_end(self):
        func = ModuleFuncInstance(
            FuncType([], [ValueType.I32, ValueType.I32]),
            self.module_inst,
            [],
            [i32_const(1), i32_const(2)],
        )
        func.body = flatten_instructions(func.body, 0)
        func_idx = self.machine.add_func(func)
        self.machine.invoke_func(func_idx)

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
        func_idx = self.machine.add_func(func)
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_invoke_func_with_return(self):
        func_idx = self._add_i32_func(
            i32_const(1),
            noarg(InstructionType.RETURN),
            noarg(InstructionType.DROP),
            i32_const(2),
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_invoke_void_func_with_return_using_br(self):
        func = ModuleFuncInstance(
            FuncType([], []),
            self.module_inst,
            [],
            [noarg(InstructionType.NOP), br(0), noarg(InstructionType.NOP)],
        )
        func.body = flatten_instructions(func.body, 0)
        func_idx = self.machine.add_func(func)
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth)

    def test_invoke_func_with_return_using_br(self):
        func_idx = self._add_i32_func(
            i32_const(1), br(0), noarg(InstructionType.DROP), i32_const(2)
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
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
        func_idx = self._add_i32_func(
            i32_const(c1),
            i32_const(c2),
            noarg(InstructionType.I32_LT_U),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        ("1 == 2 is False", 1, 2, Value(ValueType.I32, 0)),
        ("2 == 1 is False", 2, 1, Value(ValueType.I32, 0)),
        ("1 == 1 is True", 1, 1, Value(ValueType.I32, 1)),
    )
    def test_i32_eq(self, c1: int, c2: int, expected: Value):
        func_idx = self._add_i32_func(
            i32_const(c1),
            i32_const(c2),
            noarg(InstructionType.I32_EQ),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        ("1 | 2 is 3", 1, 2, Value(ValueType.I32, 3)),
        ("2 | 1 is 3", 2, 1, Value(ValueType.I32, 3)),
        ("1 | 1 is 1", 1, 1, Value(ValueType.I32, 1)),
    )
    def test_i32_or(self, c1: int, c2: int, expected: Value):
        func_idx = self._add_i32_func(
            i32_const(c1),
            i32_const(c2),
            noarg(InstructionType.I32_OR),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        ("[5,6] select 0 = 6", 0, 5, 6, Value(ValueType.I32, 6)),
        ("[5,6] select 1 = 5", 1, 5, 6, Value(ValueType.I32, 5)),
    )
    def test_select(self, c: int, v1: int, v2: int, expected: Value):
        func_idx = self._add_i32_func(
            i32_const(v1),
            i32_const(v2),
            i32_const(c),
            noarg(InstructionType.SELECT),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    @parameterized.named_parameters(
        ("1 eqz is False", 1, Value(ValueType.I32, 0)),
        ("0 eqz is True", 0, Value(ValueType.I32, 1)),
    )
    def test_i32_eqz(self, c: int, expected: Value):
        func_idx = self._add_i32_func(
            i32_const(c),
            noarg(InstructionType.I32_EQZ),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), expected)

    def test_block_end(self):
        func_idx = self._add_i32_func(i32_block(i32_const(1)))
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_return(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_const(1),
                noarg(InstructionType.RETURN),
            ),
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_nested_block_ends(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                ),
            ),
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_nested_block_returns(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    noarg(InstructionType.RETURN),
                ),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_br_0_skips_own_block(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_const(1),
                br(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_br_1_skips_parent_block(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    br(1),
                    noarg(InstructionType.DROP),
                    i32_const(2),
                ),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_block_br_0_continues_parent_block(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_const(1),
                    br(0),
                    noarg(InstructionType.DROP),
                    i32_const(2),
                ),
                noarg(InstructionType.DROP),
                i32_const(3),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertEqual(self._stack_depth(), self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_br_if_0(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_const(1),
                i32_const(0),
                br_if(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 2))

    def test_br_if_1(self):
        func_idx = self._add_i32_func(
            i32_block(
                i32_const(1),
                i32_const(1),
                br_if(0),
                noarg(InstructionType.DROP),
                i32_const(2),
            )
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    @parameterized.named_parameters(
        ("0", 0, 1),
        ("1", 1, 2),
        ("default", 2, 3),
    )
    def test_br_table(self, idx: int, expected_result: int):
        func_idx = self._add_i32_func(
            i32_block(
                i32_block(
                    i32_block(
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
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    @parameterized.named_parameters(
        ("False", 0, 1),
        ("True", 2, 2),
    )
    def test_if(self, val: int, expected_result: int):
        func_idx = self._add_i32_func(
            i32_const(val),
            if_(
                i32_const(2),
                noarg(InstructionType.RETURN),
            ),
            i32_const(1),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    @parameterized.named_parameters(
        ("False", 0, 2),
        ("True", 2, 3),
    )
    def test_if_else(self, val: int, expected_result: int):
        func_idx = self._add_i32_func(
            i32_const(val),
            if_else(
                [i32_const(2)],
                [i32_const(1)],
            ),
            i32_const(1),
            noarg(InstructionType.I32_ADD),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, expected_result))

    def test_loop_one_iterations(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        func_idx = self._add_i32_func(
            i32_const(1),
            i32_loop(
                func_type_idx,
                i32_const(2),
                noarg(InstructionType.I32_ADD),
            ),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_return(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        func_idx = self._add_i32_func(
            i32_const(1),
            i32_loop(
                func_type_idx,
                i32_const(2),
                noarg(InstructionType.I32_ADD),
                noarg(InstructionType.RETURN),
            ),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_br_1(self):
        func_type_idx = self.add_func_type([ValueType.I32], [ValueType.I32])
        func_idx = self._add_i32_func(
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
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))

    def test_loop_continue(self):
        func_type_idx = self.add_func_type([], [])
        func_idx = self._add_i32_func(
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
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_loop_continue_from_inner_block(self):
        func_type_idx = self.add_func_type([], [])
        func_idx = self._add_i32_func(
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
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 11))

    def test_call(self):
        func_idx1 = self._add_i32_func(i32_const(1))
        func_idx = self._add_i32_func(
            call(func_idx1),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_call_and_return(self):
        func_idx1 = self._add_i32_func(i32_const(1), noarg(InstructionType.RETURN))
        func_idx = self._add_i32_func(
            call(func_idx1),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 1))

    def test_call_args(self):
        func_idx1 = self._add_i32_i32_func(
            local_get(0),
            i32_const(1),
            noarg(InstructionType.I32_ADD),
        )
        func_idx = self._add_i32_func(
            i32_const(2),
            call(func_idx1),
        )
        self.machine.invoke_func(func_idx)

        self.assertStackDepth(self.starting_stack_depth + 1)
        self.assertEqual(self.machine.pop(), Value(ValueType.I32, 3))


if __name__ == "__main__":
    absltest.main()
