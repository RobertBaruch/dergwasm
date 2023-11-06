"""Unit tests for binary.py."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=too-many-lines,too-many-public-methods
# pylint: disable=invalid-name

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations
from io import BytesIO

from absl.testing import absltest, parameterized  # type: ignore
import leb128  # type: ignore

from dergwasm.interpreter.binary import (
    Code,
    Data,
    ElementSegment,
    Export,
    FuncType,
    Global,
    GlobalType,
    Import,
    Mem,
    Table,
    TableType,
    MemType,
    flatten_instructions,
)
from dergwasm.interpreter.values import Limits, ValueType
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter.testing import util


def nop() -> Instruction:
    return Instruction(InstructionType.NOP, [], 0, 0)


def end() -> Instruction:
    return Instruction(InstructionType.END, [], 0, 0)


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

    def add_nop1_expr(self) -> Buffer:
        self.add_byte(InstructionType.NOP.value)
        self.add_byte(InstructionType.END.value)
        return self

    def add_nop2_expr(self) -> Buffer:
        self.add_byte(InstructionType.NOP.value)
        self.add_byte(InstructionType.NOP.value)
        self.add_byte(InstructionType.END.value)
        return self

    def rewind(self) -> BytesIO:
        self.data.seek(0)
        return self.data


class FuncTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_tag(self):
        data = Buffer().add_byte(0xFF).rewind()

        with self.assertRaises(ValueError):
            FuncType.read(data)

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

        func_type = FuncType.read(data)

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
            TableType.read(data)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = (
            Buffer()
            .add_value_type(ValueType.FUNCREF)
            .add_byte(0)  # tag: No max limit
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        table_type = TableType.read(data)

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

        table_type = TableType.read(data)

        self.assertEqual(table_type.reftype, ValueType.FUNCREF)
        self.assertEqual(table_type.limits.min, 1)
        self.assertEqual(table_type.limits.max, 2)


class TableTest(parameterized.TestCase):
    def test_read(self):
        # Test reading a table type with a maximum limit
        data = (
            Buffer()
            .add_value_type(ValueType.FUNCREF)
            .add_byte(1)  # tag: has max limit
            .add_unsigned_int(1)  # min limit
            .add_unsigned_int(2)  # max limit
            .rewind()
        )

        table_spec = Table.read(data)

        self.assertEqual(table_spec.table_type.reftype, ValueType.FUNCREF)
        self.assertEqual(table_spec.table_type.limits.min, 1)
        self.assertEqual(table_spec.table_type.limits.max, 2)


class MemTypeTest(absltest.TestCase):
    def test_read_raises_on_bad_limit_tag(self):
        data = (
            Buffer()
            .add_byte(2)  # tag: invalid
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        with self.assertRaises(ValueError):
            MemType.read(data)

    def test_read_no_max_limit(self):
        # Test reading a table type with no maximum limit
        data = (
            Buffer()
            .add_byte(0)  # tag: no max limit
            .add_unsigned_int(1)  # min limit
            .rewind()
        )

        mem_type = MemType.read(data)

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

        mem_type = MemType.read(data)
        self.assertEqual(mem_type.limits.min, 1)
        self.assertEqual(mem_type.limits.max, 2)


class MemTest(parameterized.TestCase):
    def test_read(self):
        data = (
            Buffer()
            .add_byte(1)  # tag: with max limit
            .add_unsigned_int(1)  # min limit
            .add_unsigned_int(2)  # max limit
            .rewind()
        )

        mem_spec = Mem.read(data)
        self.assertEqual(mem_spec.mem_type.limits.min, 1)
        self.assertEqual(mem_spec.mem_type.limits.max, 2)


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
        global_type = GlobalType.read(data)

        self.assertEqual(global_type.value_type, expected_value_type)
        self.assertEqual(global_type.mutable, expected_mutable)


class GlobalTest(parameterized.TestCase):
    def test_read(self):
        data = (
            Buffer()
            .add_value_type(ValueType.I32)  # GlobalType
            .add_byte(1)
            .add_nop2_expr()  # init
            .rewind()
        )

        global_spec = Global.read(data)

        self.assertEqual(global_spec.global_type, GlobalType(ValueType.I32, True))
        self.assertEqual(
            global_spec.init, flatten_instructions([nop(), nop(), end()], 0)
        )


class ImportTest(parameterized.TestCase):
    def test_read_imported_func(self):
        data = (
            Buffer()
            .add_string("module")  # module name
            .add_string("name")  # field name
            .add_byte(0)  # kind: function
            .add_unsigned_int(100)  # type index
            .rewind()
        )

        import_spec = Import.read(data)

        self.assertEqual(import_spec.module, "module")
        self.assertEqual(import_spec.name, "name")
        self.assertEqual(import_spec.desc, 100)

    def test_read_imported_table(self):
        data = (
            Buffer()
            .add_string("module")  # module name
            .add_string("name")  # field name
            .add_byte(1)  # kind: table
            .add_value_type(ValueType.FUNCREF)  # TableType
            .add_byte(0)
            .add_unsigned_int(1)
            .rewind()
        )

        import_spec = Import.read(data)

        self.assertEqual(import_spec.module, "module")
        self.assertEqual(import_spec.name, "name")
        self.assertEqual(import_spec.desc, TableType(ValueType.FUNCREF, Limits(1)))

    def test_read_imported_mem(self):
        data = (
            Buffer()
            .add_string("module")  # module name
            .add_string("name")  # field name
            .add_byte(2)  # kind: memory
            .add_byte(0)  # MemType
            .add_unsigned_int(1)
            .rewind()
        )

        import_spec = Import.read(data)

        self.assertEqual(import_spec.module, "module")
        self.assertEqual(import_spec.name, "name")
        self.assertEqual(import_spec.desc, MemType(Limits(1)))

    def test_read_imported_global(self):
        data = (
            Buffer()
            .add_string("module")  # module name
            .add_string("name")  # field name
            .add_byte(3)  # kind: global
            .add_value_type(ValueType.I32)  # GlobalType
            .add_byte(1)
            .rewind()
        )

        import_spec = Import.read(data)

        self.assertEqual(import_spec.module, "module")
        self.assertEqual(import_spec.name, "name")
        self.assertEqual(import_spec.desc, GlobalType(ValueType.I32, True))

    def test_read_fails_on_bad_tag(self):
        data = (
            Buffer()
            .add_string("module")  # module name
            .add_string("name")  # field name
            .add_byte(4)  # kind: invalid
            .rewind()
        )

        with self.assertRaises(ValueError):
            Import.read(data)


class ExportTest(parameterized.TestCase):
    @parameterized.named_parameters(
        ("FuncType", 0, FuncType),
        ("TableType", 1, TableType),
        ("MemType", 2, MemType),
        ("GlobalType", 3, GlobalType),
    )
    def test_read(self, tag: int, expected_type: type):
        data = Buffer().add_string("name").add_byte(tag).add_unsigned_int(100).rewind()

        export_spec = Export.read(data)

        self.assertEqual(export_spec.name, "name")
        self.assertEqual(export_spec.desc_type, expected_type)
        self.assertEqual(export_spec.desc_idx, 100)

    def test_read_fails_on_bad_tag(self):
        data = Buffer().add_string("name").add_byte(4).rewind()  # tag: invalid

        with self.assertRaises(ValueError):
            Export.read(data)


class ElementSegmentTest(parameterized.TestCase):
    def test_read_tag_0(self):
        data = (
            Buffer()
            .add_unsigned_int(0)  # tag
            .add_nop2_expr()  # expr
            .add_unsigned_int(2)  # vec(funcidx)
            .add_unsigned_int(100)
            .add_unsigned_int(101)
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertEqual(
            element_segment.offset_expr, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(element_segment.tableidx, 0)
        self.assertEqual(element_segment.elem_indexes, [100, 101])
        self.assertIsNone(element_segment.elem_exprs)
        self.assertTrue(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_1(self):
        data = (
            Buffer()
            .add_unsigned_int(1)  # tag
            .add_byte(0)  # elemkind
            .add_unsigned_int(2)  # vec(funcidx)
            .add_unsigned_int(100)
            .add_unsigned_int(101)
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertIsNone(element_segment.offset_expr)
        self.assertIsNone(element_segment.tableidx)
        self.assertEqual(element_segment.elem_indexes, [100, 101])
        self.assertIsNone(element_segment.elem_exprs)
        self.assertFalse(element_segment.is_active)
        self.assertTrue(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_2(self):
        data = (
            Buffer()
            .add_unsigned_int(2)  # tag
            .add_unsigned_int(200)  # tableidx
            .add_nop2_expr()  # expr
            .add_byte(0)  # elemkind
            .add_unsigned_int(2)  # vec(funcidx)
            .add_unsigned_int(100)
            .add_unsigned_int(101)
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertEqual(
            element_segment.offset_expr, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(element_segment.tableidx, 200)
        self.assertEqual(element_segment.elem_indexes, [100, 101])
        self.assertIsNone(element_segment.elem_exprs)
        self.assertTrue(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_3(self):
        data = (
            Buffer()
            .add_unsigned_int(3)  # tag
            .add_byte(0)  # elemkind
            .add_unsigned_int(2)  # vec(funcidx)
            .add_unsigned_int(100)
            .add_unsigned_int(101)
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertIsNone(element_segment.offset_expr)
        self.assertIsNone(element_segment.tableidx)
        self.assertEqual(element_segment.elem_indexes, [100, 101])
        self.assertIsNone(element_segment.elem_exprs)
        self.assertFalse(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertTrue(element_segment.is_declarative)

    def test_read_tag_4(self):
        data = (
            Buffer()
            .add_unsigned_int(4)  # tag
            .add_nop2_expr()  # expr
            .add_unsigned_int(2)  # vec(expr)
            .add_nop2_expr()  # expr
            .add_nop1_expr()
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertEqual(
            element_segment.offset_expr, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(element_segment.tableidx, 0)
        self.assertIsNone(element_segment.elem_indexes)
        self.assertEqual(
            element_segment.elem_exprs,
            [
                flatten_instructions([nop(), nop(), end()], 0),
                flatten_instructions([nop(), end()], 0),
            ],
        )
        self.assertTrue(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_5(self):
        data = (
            Buffer()
            .add_unsigned_int(5)  # tag
            .add_value_type(ValueType.FUNCREF)  # reftype: FUNCREF
            .add_unsigned_int(2)  # vec(expr)
            .add_nop2_expr()  # expr
            .add_nop1_expr()
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertIsNone(element_segment.offset_expr)
        self.assertIsNone(element_segment.tableidx)
        self.assertIsNone(element_segment.elem_indexes)
        self.assertEqual(
            element_segment.elem_exprs,
            [
                flatten_instructions([nop(), nop(), end()], 0),
                flatten_instructions([nop(), end()], 0),
            ],
        )
        self.assertFalse(element_segment.is_active)
        self.assertTrue(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_6(self):
        data = (
            Buffer()
            .add_unsigned_int(6)  # tag
            .add_unsigned_int(200)  # tableidx
            .add_nop2_expr()  # expr
            .add_value_type(ValueType.FUNCREF)  # reftype: FUNCREF
            .add_unsigned_int(2)  # vec(expr)
            .add_nop2_expr()  # expr
            .add_nop1_expr()
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertEqual(
            element_segment.offset_expr, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(element_segment.tableidx, 200)
        self.assertIsNone(element_segment.elem_indexes)
        self.assertEqual(
            element_segment.elem_exprs,
            [
                flatten_instructions([nop(), nop(), end()], 0),
                flatten_instructions([nop(), end()], 0),
            ],
        )
        self.assertTrue(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertFalse(element_segment.is_declarative)

    def test_read_tag_7(self):
        data = (
            Buffer()
            .add_unsigned_int(7)  # tag
            .add_value_type(ValueType.FUNCREF)  # reftype: FUNCREF
            .add_unsigned_int(2)  # vec(expr)
            .add_nop2_expr()  # expr
            .add_nop1_expr()
            .rewind()
        )

        element_segment = ElementSegment.read(data)

        self.assertEqual(element_segment.elem_type, ValueType.FUNCREF)
        self.assertIsNone(element_segment.offset_expr)
        self.assertIsNone(element_segment.tableidx)
        self.assertIsNone(element_segment.elem_indexes)
        self.assertEqual(
            element_segment.elem_exprs,
            [
                flatten_instructions([nop(), nop(), end()], 0),
                flatten_instructions([nop(), end()], 0),
            ],
        )
        self.assertFalse(element_segment.is_active)
        self.assertFalse(element_segment.is_passive)
        self.assertTrue(element_segment.is_declarative)


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
        flattened = flatten_instructions(insns, 0)

        self.assertSequenceEqual(
            [i.instruction_type for i in flattened], expected_flattened_types
        )
        self.assertSequenceEqual(
            [i.continuation_pc for i in flattened], expected_continuation_pcs
        )
        self.assertSequenceEqual(
            [i.else_continuation_pc for i in flattened], expected_else_continuation_pcs
        )


class CodeTest(parameterized.TestCase):
    def test_read(self):
        data = (
            Buffer()
            .add_unsigned_int(3)  # code size
            .add_unsigned_int(2)  # vec(local)
            .add_unsigned_int(1)  # num local vars
            .add_value_type(ValueType.I32)  # local var type
            .add_unsigned_int(2)  # num local vars
            .add_value_type(ValueType.I64)  # local var type
            .add_nop2_expr()  # func body
            .rewind()
        )

        code_spec = Code.read(data)

        self.assertEqual(
            code_spec.local_var_types, [ValueType.I32, ValueType.I64, ValueType.I64]
        )
        self.assertEqual(
            code_spec.insns, flatten_instructions([nop(), nop(), end()], 0)
        )


class DataTest(parameterized.TestCase):
    def test_read_type_0(self):
        data = (
            Buffer()
            .add_unsigned_int(0)  # tag
            .add_nop2_expr()  # offset expr
            .add_unsigned_int(3)  # data size
            .add_byte(0x01)
            .add_byte(0x02)
            .add_byte(0x03)
            .rewind()
        )

        data_segment = Data.read(data)

        self.assertEqual(data_segment.memidx, 0)
        self.assertEqual(
            data_segment.offset, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(data_segment.data, b"\x01\x02\x03")
        self.assertTrue(data_segment.is_active)

    def test_read_type_1(self):
        data = (
            Buffer()
            .add_unsigned_int(1)  # tag
            .add_unsigned_int(3)  # data size
            .add_byte(0x01)
            .add_byte(0x02)
            .add_byte(0x03)
            .rewind()
        )

        data_segment = Data.read(data)

        self.assertEqual(data_segment.memidx, 0)
        self.assertIsNone(data_segment.offset)
        self.assertEqual(data_segment.data, b"\x01\x02\x03")
        self.assertFalse(data_segment.is_active)

    def test_read_type_2(self):
        data = (
            Buffer()
            .add_unsigned_int(2)  # tag
            .add_unsigned_int(200)  # memidx
            .add_nop2_expr()  # offset expr
            .add_unsigned_int(3)  # data size
            .add_byte(0x01)
            .add_byte(0x02)
            .add_byte(0x03)
            .rewind()
        )

        data_segment = Data.read(data)

        self.assertEqual(data_segment.memidx, 200)
        self.assertEqual(
            data_segment.offset, flatten_instructions([nop(), nop(), end()], 0)
        )
        self.assertEqual(data_segment.data, b"\x01\x02\x03")
        self.assertTrue(data_segment.is_active)


if __name__ == "__main__":
    absltest.main()
