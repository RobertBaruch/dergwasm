"""Unit tests for binary.py."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=too-many-lines,too-many-public-methods
# pylint: disable=invalid-name

from io import BytesIO

from absl.testing import absltest, parameterized

from dergwasm.interpreter import binary
from dergwasm.interpreter import values
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
#         self.assertEqual(function_section.funcs[0].results[0], values.ValueType.i32)

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


class FuncTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_tag(self):
        # Test reading a FuncType from a binary stream
        data = bytes.fromhex("FF 02 7F 7F 01 7F")
        f = BytesIO(data)
        with self.assertRaises(ValueError):
            binary.FuncType.read(f)

    def test_read(self):
        # Test reading a FuncType from a binary stream
        data = bytes.fromhex("60 02 7F 7F 01 7F")
        f = BytesIO(data)
        func_type = binary.FuncType.read(f)

        # Test that the FuncType has the expected parameters and results
        self.assertEqual(len(func_type.parameters), 2)
        self.assertEqual(func_type.parameters[0], values.ValueType.I32)
        self.assertEqual(func_type.parameters[1], values.ValueType.I32)
        self.assertEqual(len(func_type.results), 1)
        self.assertEqual(func_type.results[0], values.ValueType.I32)


class TableTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_limit_tag(self):
        data = bytes.fromhex("70 02 01")
        f = BytesIO(data)

        with self.assertRaises(ValueError):
            binary.TableType.read(f)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = bytes.fromhex("70 00 01")
        f = BytesIO(data)

        table_type = binary.TableType.read(f)
        self.assertEqual(table_type.reftype, values.ValueType.FUNCREF)
        self.assertEqual(table_type.limits.min, 1)
        self.assertIsNone(table_type.limits.max)

    def test_read_with_max_limit(self):
        # Test reading a table type with a maximum limit
        data = bytes.fromhex("70 01 01 02")
        f = BytesIO(data)

        table_type = binary.TableType.read(f)
        self.assertEqual(table_type.reftype, values.ValueType.FUNCREF)
        self.assertEqual(table_type.limits.min, 1)
        self.assertEqual(table_type.limits.max, 2)


class MemTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_limit_tag(self):
        data = bytes.fromhex("02 01")
        f = BytesIO(data)

        with self.assertRaises(ValueError):
            binary.MemType.read(f)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = bytes.fromhex("00 01")
        f = BytesIO(data)

        mem_type = binary.MemType.read(f)
        self.assertEqual(mem_type.limits.min, 1)
        self.assertIsNone(mem_type.limits.max)

    def test_read_with_max_limit(self):
        # Test reading a table type with a maximum limit
        data = bytes.fromhex("01 01 02")
        f = BytesIO(data)

        mem_type = binary.MemType.read(f)
        self.assertEqual(mem_type.limits.min, 1)
        self.assertEqual(mem_type.limits.max, 2)


class GlobalTypeTest(parameterized.TestCase):
    @parameterized.named_parameters(
        ("I32_mutable", "7F 01", values.ValueType.I32, True),
        ("I64_mutable", "7E 01", values.ValueType.I64, True),
        ("I64_immutable", "7E 00", values.ValueType.I64, False),
    )
    def test_read(
        self,
        hexdata: str,
        expected_value_type: values.ValueType,
        expected_mutable: bool,
    ):
        data = bytes.fromhex(hexdata)
        f = BytesIO(data)
        global_type = binary.GlobalType.read(f)
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
