"""Unit tests for binary.py."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=too-many-lines,too-many-public-methods
# pylint: disable=invalid-name

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations
from io import BytesIO

from absl.testing import absltest, parameterized
import leb128

from dergwasm.interpreter import binary
from dergwasm.interpreter.values import ValueType
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter.testing import util


def nop() -> Instruction:
    return Instruction(InstructionType.NOP, [], 0, 0)


# class TestModule(unittest.TestCase):
#     def test_read(self):
#         # Test reading a module from a binary stream
#         wasm_bytes = b"\x00\x61\x73\x6D\x01\x00\x00\x00\x01\x0A\x02\x60\x00\x00\x02\x7F\x7F\x01\x7F\x03\x02\x01\x00\x07\x07\x01\x03\x66\x6F\x6F\x00\x01\x03\x62\x61\x72\x00\x00\x0A\x09\x01\x07\x00\x20\x00\x41\x00\x6A\x0B"
#         wasm_stream = BytesIO(wasm_bytes)
#         module = binary.Module.read(wasm_stream)

#         # Test that the module has the expected sections
#         self.assertIsInstance(module.sections[binary.TypeSection], binary.TypeSection)
#         self.assertIsInstance(module.sections[binary.ImportSection], binary.ImportSection)
#         self.assertIsInstance(module.sections[binary.FunctionSection], binary.FunctionSection)
#         self.assertIsInstance(module.sections[binary.TableSection], binary.TableSection)
#         self.assertIsInstance(module.sections[binary.MemorySection], binary.MemorySection)
#         self.assertIsInstance(module.sections[binary.GlobalSection], binary.GlobalSection)
#         self.assertIsInstance(module.sections[binary.ExportSection], binary.ExportSection)
#         self.assertIsInstance(module.sections[binary.StartSection], binary.StartSection)
#         self.assertIsInstance(module.sections[binary.ElementSection], binary.ElementSection)
#         self.assertIsInstance(module.sections[binary.CodeSection], binary.CodeSection)
#         self.assertIsInstance(module.sections[binary.DataSection], binary.DataSection)
#         self.assertIsInstance(module.sections[binary.DataCountSection], binary.DataCountSection)

#         # Test that the function section has the expected functions
#         function_section = module.sections[binary.FunctionSection]
#         self.assertEqual(len(function_section.funcs), 1)
#         self.assertEqual(len(function_section.funcs[0].params), 0)
#         self.assertEqual(len(function_section.funcs[0].results), 1)
#         self.assertEqual(function_section.funcs[0].results[0], ValueType.i32)

#         # Test that the code section has the expected code
#         code_section = module.sections[binary.CodeSection]
#         self.assertEqual(len(code_section.code), 1)
#         self.assertEqual(len(code_section.code[0].local_vars), 1)
#         self.assertEqual(code_section.code[0].local_vars[0], (1, ValueType.i32))
#         self.assertEqual(len(code_section.code[0].insns), 2)
#         self.assertEqual(code_section.code[0].insns[0], Instruction.opcode("i32.const"))
#         self.assertEqual(code_section.code[0].insns[1], Instruction.opcode("i32.add"))

#     def test_from_file(self):
#         # Test reading a module from a file
#         module = binary.Module.from_file("test.wasm")

#         # Test that the module has the expected sections
#         self.assertIsInstance(module.sections[binary.TypeSection], binary.TypeSection)
#         self.assertIsInstance(module.sections[binary.ImportSection], binary.ImportSection)
#         self.assertIsInstance(module.sections[binary.FunctionSection], binary.FunctionSection)
#         self.assertIsInstance(module.sections[binary.TableSection], binary.TableSection)
#         self.assertIsInstance(module.sections[binary.MemorySection], binary.MemorySection)
#         self.assertIsInstance(module.sections[binary.GlobalSection], binary.GlobalSection)
#         self.assertIsInstance(module.sections[binary.ExportSection], binary.ExportSection)
#         self.assertIsInstance(module.sections[binary.StartSection], binary.StartSection)
#         self.assertIsInstance(module.sections[binary.ElementSection], binary.ElementSection)
#         self.assertIsInstance(module.sections[binary.CodeSection], binary.CodeSection)
#         self.assertIsInstance(module.sections[binary.DataSection], binary.DataSection)
#         self.assertIsInstance(module.sections[binary.DataCountSection], binary.DataCountSection)

#         # Test that the function section has the expected functions
#         function_section = module.sections[binary.FunctionSection]
#         self.assertEqual(len(function_section.funcs), 1)
#         self.assertEqual(len(function_section.funcs[0].params), 0)
#         self.assertEqual(len(function_section.funcs[0].results), 1)
#         self.assertEqual(function_section.funcs[0].results[0], ValueType.i32)

#         # Test that the code section has the expected code
#         code_section = module.sections[binary.CodeSection]
#         self.assertEqual(len(code_section.code), 1)
#         self.assertEqual(len(code_section.code[0].local_vars), 1)
#         self.assertEqual(code_section.code[0].local_vars[0], (1, ValueType.i32))
#         self.assertEqual(len(code_section.code[0].insns), 2)
#         self.assertEqual(code_section.code[0].insns[0], Instruction.opcode("i32.const"))
#         self.assertEqual(code_section.code[0].insns[1], Instruction.opcode("i32.add"))


class Buffer:
    """A byte buffer for easily building binary data."""

    def __init__(self) -> None:
        self.data = BytesIO()

    def add_byte(self, byte: int) -> Buffer:
        self.data.write(bytes([byte]))
        return self

    def add_value_type(self, value_type: ValueType) -> Buffer:
        self.add_byte(value_type.value)
        return self

    def add_unsigned_int(self, value: int) -> Buffer:
        self.data.write(leb128.u.encode(value))
        return self

    def add_string(self, string: str) -> Buffer:
        self.add_unsigned_int(len(string))
        self.data.write(string.encode("utf-8"))
        return self

    def rewind(self) -> BytesIO:
        self.data.seek(0)
        return self.data


class FuncTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_tag(self):
        data = Buffer().add_byte(0xFF).rewind()

        with self.assertRaises(ValueError):
            binary.FuncType.read(data)

    def test_read(self):
        data = (
            Buffer()
            .add_byte(0x60)  # tag
            .add_unsigned_int(2)  # num parameters
            .add_value_type(ValueType.I32)
            .add_value_type(ValueType.F32)
            .add_unsigned_int(1)  # num results
            .add_value_type(ValueType.I64)
            .rewind()
        )

        func_type = binary.FuncType.read(data)

        self.assertEqual(func_type.parameters, [ValueType.I32, ValueType.F32])
        self.assertEqual(func_type.results, [ValueType.I64])


class TableTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_limit_tag(self):
        data = (
            Buffer()
            .add_value_type(ValueType.FUNCREF)
            .add_byte(2)  # tag: invalid
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        with self.assertRaises(ValueError):
            binary.TableType.read(data)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = (
            Buffer()
            .add_value_type(ValueType.FUNCREF)
            .add_byte(0)  # tag: No max limit
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        table_type = binary.TableType.read(data)

        self.assertEqual(table_type.reftype, ValueType.FUNCREF)
        self.assertEqual(table_type.limits.min, 1)
        self.assertIsNone(table_type.limits.max)

    def test_read_with_max_limit(self):
        # Test reading a table type with a maximum limit
        data = (
            Buffer()
            .add_value_type(ValueType.FUNCREF)
            .add_byte(1)  # tag: has max limit
            .add_unsigned_int(1)  # min limit
            .add_unsigned_int(2)  # max limit
            .rewind()
        )

        table_type = binary.TableType.read(data)

        self.assertEqual(table_type.reftype, ValueType.FUNCREF)
        self.assertEqual(table_type.limits.min, 1)
        self.assertEqual(table_type.limits.max, 2)


class MemTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_limit_tag(self):
        data = (
            Buffer()
            .add_byte(2)  # tag: invalid
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        with self.assertRaises(ValueError):
            binary.MemType.read(data)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = (
            Buffer()
            .add_byte(0)  # tag: no max limit
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        mem_type = binary.MemType.read(data)

        self.assertEqual(mem_type.limits.min, 1)
        self.assertIsNone(mem_type.limits.max)

    def test_read_with_max_limit(self):
        # Test reading a table type with a maximum limit
        data = (
            Buffer()
            .add_byte(1)  # tag: with max limit
            .add_unsigned_int(1)  # min limit
            .add_unsigned_int(2)  # max limit
            .rewind()
        )

        mem_type = binary.MemType.read(data)
        self.assertEqual(mem_type.limits.min, 1)
        self.assertEqual(mem_type.limits.max, 2)


class GlobalTypeTest(parameterized.TestCase):
    @parameterized.named_parameters(
        (
            "I32_mutable",
            Buffer().add_value_type(ValueType.I32).add_byte(1).rewind(),
            ValueType.I32,
            True,
        ),
        (
            "I64_mutable",
            Buffer().add_value_type(ValueType.I64).add_byte(2).rewind(),
            ValueType.I64,
            True,
        ),
        (
            "I64_immutable",
            Buffer().add_value_type(ValueType.I64).add_byte(0).rewind(),
            ValueType.I64,
            False,
        ),
    )
    def test_read(
        self,
        data: BytesIO,
        expected_value_type: ValueType,
        expected_mutable: bool,
    ):
        global_type = binary.GlobalType.read(data)

        self.assertEqual(global_type.value_type, expected_value_type)
        self.assertEqual(global_type.mutable, expected_mutable)


class FlattenInstructionsTest(parameterized.TestCase):
    @parameterized.named_parameters(
        ("simple_insn", [nop()], [InstructionType.NOP], [1], [0]),
        (
            "simple_block",
            [util.i32_block(nop(), nop())],
            [
                InstructionType.BLOCK,
                InstructionType.NOP,
                InstructionType.NOP,
                InstructionType.END,
            ],
            [4, 2, 3, 4],
            [0, 0, 0, 0],
        ),
        (
            "nested_block",
            [util.i32_block(nop(), util.i32_block(nop()), nop())],
            [
                InstructionType.BLOCK,
                InstructionType.NOP,
                InstructionType.BLOCK,
                InstructionType.NOP,
                InstructionType.END,
                InstructionType.NOP,
                InstructionType.END,
            ],
            [7, 2, 5, 4, 5, 6, 7],
            [0, 0, 0, 0, 0, 0, 0],
        ),
        (
            "loop",
            [util.i32_loop(0, nop(), nop())],
            [
                InstructionType.LOOP,
                InstructionType.NOP,
                InstructionType.NOP,
                InstructionType.END,
            ],
            [0, 2, 3, 4],
            [0, 0, 0, 0],
        ),
        (
            "if",
            [util.if_(nop(), nop())],
            [
                InstructionType.IF,
                InstructionType.NOP,
                InstructionType.NOP,
                InstructionType.END,
            ],
            [4, 2, 3, 4],
            [4, 0, 0, 0],
        ),
        (
            "if_else",
            [util.if_else([nop(), nop()], [nop(), nop()])],
            [
                InstructionType.IF,
                InstructionType.NOP,
                InstructionType.NOP,
                InstructionType.ELSE,
                InstructionType.NOP,
                InstructionType.NOP,
                InstructionType.END,
            ],
            [7, 2, 3, 7, 5, 6, 7],
            [4, 0, 0, 0, 0, 0, 0],
        ),
    )
    def test_flatten(
        self,
        insns: list[Instruction],
        expected_flattened_types: list[InstructionType],
        expected_continuation_pcs: list[int],
        expected_else_continuation_pcs: list[int],
    ):
        flattened = binary.flatten_instructions(insns, 0)

        self.assertSequenceEqual(
            [i.instruction_type for i in flattened], expected_flattened_types
        )
        self.assertSequenceEqual(
            [i.continuation_pc for i in flattened], expected_continuation_pcs
        )
        self.assertSequenceEqual(
            [i.else_continuation_pc for i in flattened], expected_else_continuation_pcs
        )


if __name__ == "__main__":
    absltest.main()
