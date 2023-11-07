"""Parsing of the binary format of a module."""

# pytype: disable=too-many-return-statements

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import abc
import binascii
import dataclasses
from io import BufferedIOBase, BytesIO
from typing import Type, TypeVar, cast

import leb128  # type: ignore

from dergwasm.interpreter import insn
from dergwasm.interpreter import values


ModuleSectionT = TypeVar("ModuleSectionT", bound="ModuleSection")


def _read_byte(f: BufferedIOBase) -> int:
    """Reads a byte from the data stream.

    Raises:
        EOFError: upon reaching end of stream.
    """
    try:
        return f.read(1)[0]
    except IndexError as e:
        raise EOFError from e


def _read_value_type(f: BufferedIOBase) -> values.ValueType:
    """Reads a value type from the data stream.

    Raises:
        EOFError: upon reaching end of stream.
    """
    return values.ValueType(_read_byte(f))


def _read_unsigned_int(f: BufferedIOBase) -> int:
    """Reads an LEB128-encoded int from the data stream.

    Raises:
        EOFError: upon reaching end of stream.
    """
    try:
        return leb128.u.decode_reader(f)[0]
    except IndexError as e:
        raise EOFError from e


def _read_string(f: BufferedIOBase) -> str:
    """Reads a string from the data stream.

    Raises:
        EOFError: upon reaching end of stream.
    """
    name_size = _read_unsigned_int(f)
    return f.read(name_size).decode("utf-8")


@dataclasses.dataclass
class ExternalType:
    """The base class for external types."""


@dataclasses.dataclass
class FuncType(ExternalType):
    """The type of a function."""

    parameters: list[values.ValueType]
    results: list[values.ValueType]

    @staticmethod
    def read(f: BufferedIOBase) -> FuncType:
        """Reads and returns a FuncType.

        Returns:
            The FuncType read.

        Raises:
            EOFError: upon reaching end of file.
        """
        tag = _read_byte(f)
        if tag != 0x60:
            raise ValueError(f"Expected 0x60 tag for functype, but got {tag:02X}")
        num_parameters = _read_unsigned_int(f)
        parameters = [_read_value_type(f) for _ in range(num_parameters)]
        num_results = _read_unsigned_int(f)
        results = [_read_value_type(f) for _ in range(num_results)]
        return FuncType(parameters, results)

    def __repr__(self) -> str:
        return f"FuncType({self.parameters}, {self.results})"


@dataclasses.dataclass
class TableType(ExternalType):
    """The type of a table."""

    reftype: values.ValueType  # can only be FUNCREF or EXTERNREF
    limits: values.Limits

    @staticmethod
    def read(f: BufferedIOBase) -> TableType:
        """Reads and returns a TableType."""
        reftype = values.ValueType(_read_byte(f))
        tag = _read_byte(f)
        min_limit = _read_unsigned_int(f)
        if tag == 0x00:
            max_limit = None
        elif tag == 0x01:
            max_limit = _read_unsigned_int(f)
        else:
            raise ValueError(f"Unknown tabletype limit tag {tag:02X}")
        return TableType(reftype, values.Limits(min_limit, max_limit))

    def __repr__(self) -> str:
        return f"TableType({self.reftype}, {self.limits.min}, {self.limits.max})"


@dataclasses.dataclass
class MemType(ExternalType):
    """The type of a memory."""

    limits: values.Limits

    @staticmethod
    def read(f: BufferedIOBase) -> MemType:
        """Reads and returns a MemType."""
        tag = _read_byte(f)
        min_limit = _read_unsigned_int(f)
        if tag == 0x00:
            max_limit = None
        elif tag == 0x01:
            max_limit = _read_unsigned_int(f)
        else:
            raise ValueError(f"Unknown memtype limit tag {tag:02X}")
        return MemType(values.Limits(min_limit, max_limit))

    def __repr__(self) -> str:
        return f"MemType({self.limits.min}, {self.limits.max})"


@dataclasses.dataclass
class GlobalType(ExternalType):
    """The type of a global."""

    value_type: values.ValueType
    mutable: bool

    @staticmethod
    def read(f: BufferedIOBase) -> GlobalType:
        """Reads and returns a GlobalType."""
        value_type = values.ValueType(_read_byte(f))
        mutable = bool(_read_byte(f))
        return GlobalType(value_type, mutable)


@dataclasses.dataclass
class Import:
    """An import.

    Each import is labeled by a two-level name space, consisting of a module name and
    a name for an entity within that module. Importable definitions are functions,
    tables, memories, and globals. Each import is specified by a descriptor with a
    respective type that a definition provided during instantiation is required to
    match.

    Every import defines an index in the respective index space. In each index space,
    the indices of imports go before the first index of any definition contained in the
    module itself (what even does this mean).
    """

    module: str
    name: str
    # The int type will be replaced by FuncType after we fix up the module after reading
    # it all in.
    desc: int | FuncType | TableType | MemType | GlobalType

    @staticmethod
    def read(f: BufferedIOBase) -> Import:
        """Reads and returns an Import.

        `desc` is the descriptor. If it's an int, it's an index into the FuncSection
        `types` list.

        Returns:
            The Import read.

        Raises:
            ValueError: upon encountering a bad import desc tag.
        """
        module = _read_string(f)
        name = _read_string(f)
        tag = _read_byte(f)
        if tag == 0x00:  # type index, gets resolved later to FuncType.
            return Import(module, name, _read_unsigned_int(f))
        if tag == 0x01:
            return Import(module, name, TableType.read(f))
        if tag == 0x02:
            return Import(module, name, MemType.read(f))
        if tag == 0x03:
            return Import(module, name, GlobalType.read(f))
        raise ValueError(f"Unknown import desc tag {tag:02X}")

    def __repr__(self) -> str:
        return f"Import({self.module}:{self.name}, {self.desc})"


@dataclasses.dataclass
class Export:
    """An export."""

    name: str
    desc_type: Type[FuncType | TableType | MemType | GlobalType]
    desc_idx: int  # The index into the respective section of the given type.

    @staticmethod
    def read(f: BufferedIOBase) -> Export:
        """Reads and returns an Export.

        Returns:
            The Export read.

        Raises:
            ValueError: upon encountering a bad export desc tag.
        """
        name = _read_string(f)
        tag = _read_byte(f)
        if tag > 3:
            raise ValueError(f"Unknown import desc tag {tag:02X}")
        desc_type = [FuncType, TableType, MemType, GlobalType][tag]
        desc_idx = _read_unsigned_int(f)
        return Export(name, desc_type, desc_idx)


@dataclasses.dataclass
class Table:
    """A table specification.

    The initial contents of a table is uninitialized. Element segments can be used to
    initialize a subrange of a table from a static vector of elements.
    """

    table_type: TableType

    @staticmethod
    def read(f: BufferedIOBase) -> Table:
        """Reads and returns a Table."""
        return Table(TableType.read(f))


@dataclasses.dataclass
class Mem:
    """A memory specification."""

    mem_type: MemType

    @staticmethod
    def read(f: BufferedIOBase) -> Mem:
        """Reads and returns a Mem."""
        return Mem(MemType.read(f))


@dataclasses.dataclass
class ElementSegment:
    """An element segment specification.

    Element segments are used to initialize sections of tables. The elements of tables
    are always references (either FUNCREF or EXTERNREF).

    Element segments have a mode that identifies them as either passive, active, or
    declarative:

    * A passive element segment's elements can be copied to a table using the table.init
      instruction.

    * An active element segment copies its elements into a table during instantiation,
      as specified by a table index and a constant expression defining an offset into
      that table.

    * A declarative element segment is not available at runtime but merely serves to
      forward-declare references that are formed in code with instructions like
      ref.func.
    """

    elem_type: values.ValueType

    offset_expr: list[insn.Instruction] | None = None
    tableidx: int | None = None

    # These are mutually exclusive.
    elem_indexes: list[int] | None = None
    elem_exprs: list[list[insn.Instruction]] | None = None

    is_active: bool = False
    is_passive: bool = False
    is_declarative: bool = False

    def size(self) -> int:
        """Returns the size of the element segment."""
        if self.elem_indexes is not None:
            return len(self.elem_indexes)
        if self.elem_exprs is not None:
            return len(self.elem_exprs)
        raise ValueError(
            "Element segment is not defined correctly: no indexes or exprs."
        )

    @staticmethod
    def read(f: BufferedIOBase) -> ElementSegment:
        """Reads and returns an ElementSegment."""
        desc_idx = _read_unsigned_int(f)

        if desc_idx == 0x00:
            # Active segment with default tableidx (0) and default element kind
            # (FUNCREF).
            tableidx = 0
            offset_expr = read_expr(f)
            elem_type = values.ValueType.FUNCREF
            elem_indexes = [_read_unsigned_int(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                tableidx=tableidx,
                offset_expr=offset_expr,
                elem_indexes=elem_indexes,
                is_active=True,
            )

        if desc_idx == 0x01:
            # Passive segment.
            elemkind = _read_unsigned_int(f)
            try:
                elem_type = [values.ValueType.FUNCREF][elemkind]
            except IndexError as e:
                raise ValueError(
                    f"Unknown table element segment elemkind {elemkind:02X}"
                ) from e
            elem_indexes = [_read_unsigned_int(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                elem_indexes=elem_indexes,
                is_passive=True,
            )

        if desc_idx == 0x02:
            # Active segment with tableidx and element kind.
            tableidx = _read_unsigned_int(f)
            offset_expr = read_expr(f)
            elemkind = _read_unsigned_int(f)
            try:
                elem_type = [values.ValueType.FUNCREF][elemkind]
            except IndexError as e:
                raise ValueError(
                    f"Unknown table element segment elemkind {elemkind:02X}"
                ) from e
            elem_indexes = [_read_unsigned_int(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                tableidx=tableidx,
                offset_expr=offset_expr,
                elem_indexes=elem_indexes,
                is_active=True,
            )

        if desc_idx == 0x03:
            # Declarative segment.
            elemkind = _read_unsigned_int(f)
            try:
                elem_type = [values.ValueType.FUNCREF][elemkind]
            except IndexError as e:
                raise ValueError(
                    f"Unknown table element segment elemkind {elemkind:02X}"
                ) from e
            elem_indexes = [_read_unsigned_int(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                elem_indexes=elem_indexes,
                is_declarative=True,
            )

        if desc_idx == 0x04:
            # Active segment with default tableidx (0) and element expressions.
            tableidx = 0
            offset_expr = read_expr(f)
            elem_type = values.ValueType.FUNCREF
            elem_exprs = [read_expr(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                tableidx=tableidx,
                offset_expr=offset_expr,
                elem_exprs=elem_exprs,
                is_active=True,
            )

        if desc_idx == 0x05:
            # Passive segment with element type and element expressions.
            elem_type = values.ValueType(_read_unsigned_int(f))
            elem_exprs = [read_expr(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                elem_exprs=elem_exprs,
                is_passive=True,
            )

        if desc_idx == 0x06:
            # Active segment with tableidx, element type, and element expressions.
            tableidx = _read_unsigned_int(f)
            offset_expr = read_expr(f)
            elem_type = values.ValueType(_read_unsigned_int(f))
            elem_exprs = [read_expr(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                tableidx=tableidx,
                offset_expr=offset_expr,
                elem_exprs=elem_exprs,
                is_active=True,
            )

        if desc_idx == 0x07:
            # Declarative segment with element type and element expressions.
            elem_type = values.ValueType(_read_unsigned_int(f))
            elem_exprs = [read_expr(f) for _ in range(_read_unsigned_int(f))]
            return ElementSegment(
                elem_type=elem_type,
                elem_exprs=elem_exprs,
                is_declarative=True,
            )

        raise ValueError(f"Unknown table element segment type tag {desc_idx:02X}")


def flatten_instructions(
    insns: list[insn.Instruction], pc: int
) -> list[insn.Instruction]:
    """Flattens a list of instructions.

    This is used to flatten out the instructions in a code section so we can compute
    instruction program counter labels.

    Every instruction has a continuation_pc. For all instructions except BLOCK, LOOP,
    and IF, this is the PC of the next instruction to execute. For BLOCK and LOOP,
    it is the PC to jump to when breaking out (i.e. if a BR 0 were to be executed).

    IF instructions have two continuations. The first is the PC just after the END
    instruction, as usual with blocks. The second is the else_continuation_pc, and
    is the PC just after the ELSE instruction, if there is one, otherwise the PC
    just after the END instruction.

    When an ELSE instruction is encountered, its continuation is the PC just after
    the END instruction.

    The continuation for BR, BR_IF, BR_TABLE, instructions are irrelevant. They just
    use the continuation_pc stored in the label they go to.

    The continuation for RETURN is irrelevant.
    """
    flattened_instructions = []
    for i in insns:
        i.else_continuation_pc = 0

        if i.instruction_type == insn.InstructionType.BLOCK:
            assert isinstance(i.operands[0], insn.Block)
            block_insns = [i]
            block_insns.extend(flatten_instructions(i.operands[0].instructions, pc + 1))
            i.operands[0].instructions = []
            flattened_instructions.extend(block_insns)
            pc += len(block_insns)
            # Where to go on breakout.
            i.continuation_pc = pc

        elif i.instruction_type == insn.InstructionType.LOOP:
            assert isinstance(i.operands[0], insn.Block)
            block_insns = [i]
            block_insns.extend(flatten_instructions(i.operands[0].instructions, pc + 1))
            i.operands[0].instructions = []
            i.operands[0].else_instructions = []
            flattened_instructions.extend(block_insns)
            # Where to go on breakout.
            i.continuation_pc = pc
            pc += len(block_insns)

        elif i.instruction_type == insn.InstructionType.IF:
            assert isinstance(i.operands[0], insn.Block)
            block_insns = [i]

            # The IF instruction contains a continuation_pc and an else_continuation_pc.
            # The continuation_pc is END+1. The else_continuation_pc is ELSE+1, if
            # there is an ELSE clause, otherwise END+1.
            #
            # Blocks end in END or ELSE:
            #  IF instr1 ELSE instr2 END
            #
            # If the condition is true, we continue with IF+1, then hit ELSE which
            #   causes us to jump to END+1. So this needs to make a label with
            #   continuation END+1, which will be resolved on ELSE. This is the
            #   instruction's continuation_pc.
            # If the condition is false, we jump to ELSE+1 (the instruction's
            #   else_continuation_pc), and hit END. So this needs to make a label with
            #   continuation END+1, which will be resolved on END. This is the
            #   instruction's continuation_pc.
            # In either case, we have a label which is branchable, and exits the IF.
            #
            # Alternatively, with no else:
            #  IF instr1 END
            #
            # If the condition is true, we continue with IF+1, then hit END. So this
            #   needs to make a label with continuation END+1, which will be resolved
            #   upon hitting END. This is the instruction's continuation_pc.
            # If the condition is false, we jump to END. So this neeeds to make a label
            #   with continuation END+1, which will be resolved upon hitting END. This
            #   is the instruction's continuation_pc.
            # In either case, we have a label which is branchable, and exits the IF.
            true_insns = flatten_instructions(i.operands[0].instructions, pc + 1)
            i.operands[0].instructions = []
            pc += len(true_insns) + 1
            i.else_continuation_pc = pc  # Either END+1 or ELSE+1.

            false_insns = flatten_instructions(i.operands[0].else_instructions, pc)
            i.operands[0].else_instructions = []
            pc += len(false_insns)
            i.continuation_pc = pc  # END+1

            block_insns.extend(true_insns)
            block_insns.extend(false_insns)
            flattened_instructions.extend(block_insns)

        else:
            flattened_instructions.append(i)
            pc += 1
            i.continuation_pc = pc

    return flattened_instructions


@dataclasses.dataclass
class Code:
    """A code specification.

    This is a pair of value type vectors and expressions. They represent the locals and
    body field of the functions in the funcs component of a module. The fields of the
    respective functions are encoded separately in the function section.
    """

    local_var_types: list[values.ValueType]
    insns: list[insn.Instruction]

    @staticmethod
    def read(f: BufferedIOBase) -> Code:
        """Reads and returns a Code."""
        _ = _read_unsigned_int(f)  # Code size, not needed.
        num_locals = _read_unsigned_int(f)
        local_vars_encoded = [
            (_read_unsigned_int(f), _read_value_type(f))
            for _ in range(num_locals)
        ]
        local_var_types = []
        for (num, value_type) in local_vars_encoded:
            local_var_types.extend([value_type] * num)
        insns = read_expr(f)

        return Code(local_var_types, insns)

    def __repr__(self) -> str:
        return "".join([i.to_str(0) for i in self.insns])


def read_expr(f: BufferedIOBase) -> list[insn.Instruction]:
    """Reads an expression: a list of instructions terminated by an end instruction.

    The returned list is flattened.
    """
    insns = []
    while True:
        instruction = insn.Instruction.read(f)
        insns.append(instruction)
        if instruction.instruction_type == insn.InstructionType.END:
            break
    return flatten_instructions(insns, 0)


@dataclasses.dataclass
class Func:
    """A function specification."""

    typeidx: int
    local_var_types: list[values.ValueType]  # Populated after module is read
    body: list[insn.Instruction]  # Populated after module is read


@dataclasses.dataclass
class Data:
    """A data segment.

    The initial contents of a memory are zero bytes. Data segments can be used to
    initialize a range of memory from a static vector of bytes.

    A passive (not active) data segment's contents can be copied into a memory using
    the memory.init instruction.

    An active data segment copies its contents into a memory during instantiation, as
    specified by a memory index and a constant expression defining an offset into that
    memory.
    """

    is_active: bool
    memidx: int
    offset: list[insn.Instruction] | None
    data: bytes

    @staticmethod
    def read(f: BufferedIOBase) -> Data:
        """Reads and returns a Data."""
        tag = _read_unsigned_int(f)
        memidx = 0
        offset = None
        is_active = False

        if tag == 0x00:
            is_active = True
            offset = read_expr(f)
        elif tag == 0x02:
            is_active = True
            memidx = _read_unsigned_int(f)
            offset = read_expr(f)

        data_size = _read_unsigned_int(f)
        data = f.read(data_size)
        return Data(is_active, memidx, offset, data)


@dataclasses.dataclass
class Global:
    """A global variable specification."""

    global_type: GlobalType
    init: list[insn.Instruction]

    @staticmethod
    def read(f: BufferedIOBase) -> Global:
        """Reads and returns a Global."""
        global_type = GlobalType.read(f)
        init = read_expr(f)
        return Global(global_type, init)


class ModuleSection(abc.ABC):
    """Base class for module sections."""

    @staticmethod
    @abc.abstractmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a module section.

        Returns:
            The module section read.
        """


@dataclasses.dataclass
class CustomSection(ModuleSection):
    """A custom section.

    Custom sections are for debugging information or third-party extensions, and are
    ignored by the WebAssembly semantics. Their contents consist of a name further
    identifying the custom section, followed by an uninterpreted sequence of bytes for
    custom use.
    """

    name: str
    data: bytes

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        name_len = _read_unsigned_int(f)
        name = f.read(name_len).decode("utf-8")
        data_len = _read_unsigned_int(f)
        data = f.read(data_len)
        print(f"Read custom section {name}: {data!r}")
        return CustomSection(name, data)


@dataclasses.dataclass
class TypeSection(ModuleSection):
    """A type section.

    Contains a list of function types that represent the types component of a module.
    All function types used in a module must be defined in this component. They are
    referenced by type indices.
    """

    types: list[FuncType]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a type section."""
        num_types = _read_unsigned_int(f)
        types = [FuncType.read(f) for _ in range(num_types)]
        print(f"Read types: {types}")
        return TypeSection(types)


@dataclasses.dataclass
class ImportSection(ModuleSection):
    """An imports section.

    Contains a list of imports that represent the imports component of a module. Imports
    are required for instantiation.

    In each index space, the indices of imports go before the first index of any
    definition contained in the module itself.
    """

    imports: list[Import]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns an imports section."""
        num_imports = _read_unsigned_int(f)
        imports = [Import.read(f) for _ in range(num_imports)]
        print(f"Read imports: {imports}")
        return ImportSection(imports)


@dataclasses.dataclass
class FunctionSection(ModuleSection):
    """A function section."""

    funcs: list[Func]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a function section."""
        num_funcs = _read_unsigned_int(f)
        functype_indices = [_read_unsigned_int(f) for _ in range(num_funcs)]
        print(
            f"Read function types (length {len(functype_indices)}): {functype_indices}"
        )
        return FunctionSection([Func(typeidx, [], []) for typeidx in functype_indices])


@dataclasses.dataclass
class TableSection(ModuleSection):
    """A table section."""

    tables: list[Table]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a table section."""
        num_tables = _read_unsigned_int(f)
        tables = [Table.read(f) for _ in range(num_tables)]
        print(f"Read tables: {tables}")
        return TableSection(tables)


@dataclasses.dataclass
class MemorySection(ModuleSection):
    """A memory section."""

    memories: list[Mem]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a memory section."""
        num_memories = _read_unsigned_int(f)
        memories = [Mem.read(f) for _ in range(num_memories)]
        print(f"Read memories: {memories}")
        return MemorySection(memories)


@dataclasses.dataclass
class GlobalSection(ModuleSection):
    """A global section."""

    global_vars: list[Global]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a global section."""
        num_globals = _read_unsigned_int(f)
        global_vars = [Global.read(f) for _ in range(num_globals)]
        print(f"Read globals: {global_vars}")
        return GlobalSection(global_vars)


@dataclasses.dataclass
class ExportSection(ModuleSection):
    """An export section."""

    exports: list[Export]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns an export section."""
        num_exports = _read_unsigned_int(f)
        exports = [Export.read(f) for _ in range(num_exports)]
        print(f"Read exports: {exports}")
        return ExportSection(exports)


@dataclasses.dataclass
class StartSection(ModuleSection):
    """A start section."""

    start_idx: int

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a start section."""
        start_idx = _read_unsigned_int(f)
        print(f"Read start: {start_idx}")
        return StartSection(start_idx)


@dataclasses.dataclass
class ElementSection(ModuleSection):
    """An element segment section."""

    elements: list[ElementSegment]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns an element section."""
        num_elements = _read_unsigned_int(f)
        elements = [ElementSegment.read(f) for _ in range(num_elements)]
        print(f"Read elements: {elements}")
        return ElementSection(elements)


@dataclasses.dataclass
class CodeSection(ModuleSection):
    """A code section."""

    code: list[Code]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a code section."""
        num_code = _read_unsigned_int(f)
        code = [Code.read(f) for _ in range(num_code)]
        print(f"Read code (len {len(code)})")
        return CodeSection(code)


@dataclasses.dataclass
class DataSection(ModuleSection):
    """A data section."""

    data: list[Data]

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a data section."""
        num_data = _read_unsigned_int(f)
        data = [Data.read(f) for _ in range(num_data)]
        print(f"Read data: {data}")
        return DataSection(data)


@dataclasses.dataclass
class DataCountSection(ModuleSection):
    """A data count section."""

    data_count: int

    @staticmethod
    def read(f: BufferedIOBase) -> ModuleSection:
        """Reads and returns a data count section."""
        data_count = _read_unsigned_int(f)
        print(f"Read data count: {data_count}")
        return DataCountSection(data_count)


class Module:
    """Reads in a module.

    Based on https://webassembly.github.io/spec/core/binary/modules.html#binary-module.
    """

    sections: dict[Type[ModuleSection], ModuleSection]

    def __init__(self):
        # default sections
        self.sections = {
            TypeSection: TypeSection([]),
            ImportSection: ImportSection([]),
            FunctionSection: FunctionSection([]),
            TableSection: TableSection([]),
            MemorySection: MemorySection([]),
            GlobalSection: GlobalSection([]),
            ExportSection: ExportSection([]),
            StartSection: StartSection(0),
            ElementSection: ElementSection([]),
            CodeSection: CodeSection([]),
            DataSection: DataSection([]),
            DataCountSection: DataCountSection(0),
        }

    @staticmethod
    def from_file(f: str) -> Module:
        """Reads a module from a file."""
        with open(f, "rb") as data:
            return Module.read(data)

    @staticmethod
    def read(f: BufferedIOBase) -> Module:
        """Reads a module from a binary stream."""
        if f.read(4) != b"\x00\x61\x73\x6D":
            raise ValueError("Magic number (0061736D) not found.")
        version = f.read(4)
        if version != b"\x01\x00\x00\x00":
            raise ValueError(f"Unsupported version {binascii.hexlify(version)!r}.")

        module = Module()
        while True:
            try:
                section = Module.read_section(f)
                if isinstance(section, CustomSection):
                    continue
                module.sections[type(section)] = section
            except EOFError:
                break

        # Fixups

        # Replace import func type indices with actual FuncTypes.
        type_section = module.get_section(TypeSection)
        import_section = module.get_section(ImportSection)
        for import_ in import_section.imports:
            if isinstance(import_.desc, int):
                import_.desc = type_section.types[import_.desc]

        # Expand func section with code section
        func_section = module.get_section(FunctionSection)
        code_section = module.get_section(CodeSection)
        assert len(func_section.funcs) == len(code_section.code)
        for i, func in enumerate(func_section.funcs):
            func.local_var_types = code_section.code[i].local_var_types
            func.body = code_section.code[i].insns
        del module.sections[CodeSection]

        return module

    @staticmethod
    def read_section(f: BufferedIOBase) -> ModuleSection:
        """Reads a section.

        Returns:
          The section read.

        Raises:
          EOFError: upon reaching end of file.
        """
        section_types: list[Type[ModuleSection]] = [
            CustomSection,
            TypeSection,
            ImportSection,
            FunctionSection,
            TableSection,
            MemorySection,
            GlobalSection,
            ExportSection,
            StartSection,
            ElementSection,
            CodeSection,
            DataSection,
            DataCountSection,
        ]

        try:
            section_id = _read_byte(f)
        except IndexError as e:
            raise EOFError from e

        if section_id > len(section_types):
            raise ValueError(f"Unknown section ID {section_id}")

        section_len = _read_unsigned_int(f)
        print(f"Reading section {section_types[section_id]} length {section_len}")
        section_data = BytesIO(f.read(section_len))

        return section_types[section_id].read(section_data)

    def get_section(self, t: Type[ModuleSectionT]) -> ModuleSectionT:
        """Gets a section of the given type."""
        return cast(ModuleSectionT, self.sections[t])
