"""Parsing of the binary format of a module."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import abc
import binascii
import dataclasses
from io import BytesIO
from typing import BinaryIO, Type

import leb128

from dergwasm.interpreter import insn
from dergwasm.interpreter import values


@dataclasses.dataclass
class ExternalType:
    """The base class for external types."""


@dataclasses.dataclass
class FuncType(ExternalType):
    """The type of a function."""

    parameters: list[values.ValueType]
    results: list[values.ValueType]

    @staticmethod
    def read(f: BytesIO) -> FuncType:
        """Reads and returns a FuncType.

        Returns:
            The FuncType read.

        Raises:
            EOFError: upon reaching end of file.
        """
        try:
            tag = f.read(1)[0]
        except IndexError as e:
            raise EOFError from e
        if tag != 0x60:
            raise ValueError(f"Expected 0x60 tag for functype, but got {tag:02X}")
        num_parameters = leb128.u.decode_reader(f)[0]
        parameters = [values.ValueType(f.read(1)[0]) for _ in range(num_parameters)]
        num_results = leb128.u.decode_reader(f)[0]
        results = [values.ValueType(f.read(1)[0]) for _ in range(num_results)]
        return FuncType(parameters, results)

    def __repr__(self) -> str:
        return f"FuncType({self.parameters}, {self.results})"


@dataclasses.dataclass
class TableType(ExternalType):
    """The type of a table."""

    reftype: values.ValueType  # can only be FUNCREF or EXTERNREF
    min_limit: int
    max_limit: int | None

    @staticmethod
    def read(f: BytesIO) -> TableType:
        """Reads and returns a TableType."""
        reftype = values.ValueType(f.read(1)[0])
        tag = f.read(1)[0]
        min_limit = leb128.u.decode_reader(f)[0]
        if tag == 0x00:
            max_limit = None
        elif tag == 0x01:
            max_limit = leb128.u.decode_reader(f)[0]
        else:
            raise ValueError(f"Unknown tabletype limit tag {tag:02X}")
        return TableType(reftype, min_limit, max_limit)

    def __repr__(self) -> str:
        return f"TableType({self.reftype}, {self.min_limit}, {self.max_limit})"


@dataclasses.dataclass
class MemType(ExternalType):
    """The type of a memory."""

    min_limit: int
    max_limit: int | None

    @staticmethod
    def read(f: BytesIO) -> MemType:
        """Reads and returns a MemType."""
        tag = f.read(1)[0]
        min_limit = leb128.u.decode_reader(f)[0]
        if tag == 0x00:
            max_limit = None
        elif tag == 0x01:
            max_limit = leb128.u.decode_reader(f)[0]
        else:
            raise ValueError(f"Unknown memtype limit tag {tag:02X}")
        return MemType(min_limit, max_limit)

    def __repr__(self) -> str:
        return f"MemType({self.min_limit}, {self.max_limit})"


@dataclasses.dataclass
class GlobalType(ExternalType):
    """The type of a global."""

    value_type: values.ValueType
    mutable: bool

    @staticmethod
    def read(f: BytesIO) -> GlobalType:
        """Reads and returns a GlobalType."""
        value_type = values.ValueType(f.read(1)[0])
        mutable = bool(f.read(1)[0])
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
    def read(f: BytesIO) -> Import:
        """Reads and returns an Import.

        `desc` is the descriptor. If it's an int, it's an index into the FuncSection
        `types` list.

        Returns:
            The Import read.

        Raises:
            ValueError: upon encountering a bad import desc tag.
        """
        name_size = leb128.u.decode_reader(f)[0]
        module = f.read(name_size).decode("utf-8")
        name_size = leb128.u.decode_reader(f)[0]
        name = f.read(name_size).decode("utf-8")
        tag = f.read(1)[0]
        if tag == 0x00:
            desc = leb128.u.decode_reader(f)[0]
        elif tag == 0x01:
            desc = TableType.read(f)
        elif tag == 0x02:
            desc = MemType.read(f)
        elif tag == 0x03:
            desc = GlobalType.read(f)
        else:
            raise ValueError(f"Unknown import desc tag {tag:02X}")

        return Import(module, name, desc)

    def __repr__(self) -> str:
        return f"Import({self.module}:{self.name}, {self.desc})"


@dataclasses.dataclass
class Export:
    """An export."""

    name: str
    desc_type: Type[FuncType | TableType | MemType | GlobalType]
    desc_idx: int  # The index into the respective section of the given type.

    @staticmethod
    def read(f: BytesIO) -> Export:
        """Reads and returns an Export.

        Returns:
            The Export read.

        Raises:
            ValueError: upon encountering a bad export desc tag.
        """
        name_size = leb128.u.decode_reader(f)[0]
        name = f.read(name_size).decode("utf-8")
        tag = f.read(1)[0]
        if tag > 3:
            raise ValueError(f"Unknown import desc tag {tag:02X}")
        desc_type = [FuncType, TableType, MemType, GlobalType][tag]
        desc_idx = leb128.u.decode_reader(f)[0]
        return Export(name, desc_type, desc_idx)


@dataclasses.dataclass
class Table:
    """A table specification."""

    table_type: TableType

    @staticmethod
    def read(f: BytesIO) -> Table:
        """Reads and returns a Table."""
        return Table(TableType.read(f))


@dataclasses.dataclass
class Mem:
    """A memory specification."""

    mem_type: MemType

    @staticmethod
    def read(f: BytesIO) -> Mem:
        """Reads and returns a Mem."""
        return Mem(MemType.read(f))


@dataclasses.dataclass
class Element:
    """An element specification."""

    # TODO: Skip this for now.
    @staticmethod
    def read(f: BytesIO) -> Element:
        """Reads and returns an Element."""
        return Element()


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

            # Ends in END or ELSE.
            # continuation_pc = END + 1
            # If END, else_continuation_pc = END + 1.
            # IF ELSE, else_continuation_pc = ELSE + 1.
            true_insns = flatten_instructions(i.operands[0].instructions, pc + 1)
            i.operands[0].instructions = []
            pc += len(true_insns) + 1
            i.else_continuation_pc = pc

            false_insns = flatten_instructions(i.operands[0].else_instructions, pc)
            i.operands[0].else_instructions = []
            pc += len(false_insns)
            i.continuation_pc = pc

            if true_insns[-1].instruction_type == insn.InstructionType.ELSE:
                true_insns[-1].continuation_pc = pc

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
    """A code specification."""

    local_vars: list[tuple[int, values.ValueType]]  # How many of each type of local.
    insns: list[insn.Instruction]

    @staticmethod
    def read(f: BytesIO) -> Code:
        """Reads and returns a Code."""
        # code size is used only for validation.
        _ = leb128.u.decode_reader(f)[0]
        num_locals = leb128.u.decode_reader(f)[0]
        local_vars = [
            (leb128.u.decode_reader(f)[0], values.ValueType(f.read(1)[0]))
            for _ in range(num_locals)
        ]
        insns = []
        while True:
            instruction = insn.Instruction.read(f)
            insns.append(instruction)
            if instruction.instruction_type == insn.InstructionType.END:
                break

        # Now we need to flatten out the instructions so we can compute instruction
        # program counter labels.

        return Code(local_vars, flatten_instructions(insns, 0))

    def __repr__(self) -> str:
        return "".join([i.to_str(0) for i in self.insns])


def read_expr(f: BytesIO) -> list[insn.Instruction]:
    """Reads an expression: a list of instructions terminated by an end instruction."""
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
    local_vars: list[values.ValueType]
    body: list[insn.Instruction]


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
    def read(f: BytesIO) -> Data:
        """Reads and returns a Data."""
        tag = leb128.u.decode_reader(f)[0]
        memidx = 0
        offset = None
        is_active = False

        if tag == 0x00:
            is_active = True
            offset = read_expr(f)
        elif tag == 0x02:
            is_active = True
            memidx = leb128.u.decode_reader(f)[0]
            offset = read_expr(f)

        data_size = leb128.u.decode_reader(f)[0]
        data = f.read(data_size)
        return Data(is_active, memidx, offset, data)


@dataclasses.dataclass
class Global:
    """A global variable specification."""

    global_type: GlobalType
    init: list[insn.Instruction]

    @staticmethod
    def read(f: BytesIO) -> Global:
        """Reads and returns a Global."""
        global_type = GlobalType.read(f)
        init = read_expr(f)
        return Global(global_type, init)


class ModuleSection(abc.ABC):
    """Base class for module sections."""

    @staticmethod
    @abc.abstractmethod
    def read(f: BytesIO) -> ModuleSection:
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
    def read(f: BytesIO) -> ModuleSection:
        name_len = leb128.u.decode_reader(f)[0]
        name = f.read(name_len).decode("utf-8")
        data_len = leb128.u.decode_reader(f)[0]
        data = f.read(data_len)
        print(f"Read custom section {name}: {data}")
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
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a type section."""
        num_types = leb128.u.decode_reader(f)[0]
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
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns an imports section."""
        num_imports = leb128.u.decode_reader(f)[0]
        imports = [Import.read(f) for _ in range(num_imports)]
        print(f"Read imports: {imports}")
        return ImportSection(imports)


@dataclasses.dataclass
class FunctionSection(ModuleSection):
    """A function section."""

    funcs: list[Func]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a function section."""
        num_funcs = leb128.u.decode_reader(f)[0]
        functype_indices = [leb128.u.decode_reader(f)[0] for _ in range(num_funcs)]
        print(
            f"Read function types (length {len(functype_indices)}): {functype_indices}"
        )
        return FunctionSection([Func(typeidx, [], []) for typeidx in functype_indices])


@dataclasses.dataclass
class TableSection(ModuleSection):
    """A table section."""

    tables: list[Table]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a table section."""
        num_tables = leb128.u.decode_reader(f)[0]
        tables = [Table.read(f) for _ in range(num_tables)]
        print(f"Read tables: {tables}")
        return TableSection(tables)


@dataclasses.dataclass
class MemorySection(ModuleSection):
    """A memory section."""

    memories: list[Mem]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a memory section."""
        num_memories = leb128.u.decode_reader(f)[0]
        memories = [Mem.read(f) for _ in range(num_memories)]
        print(f"Read memories: {memories}")
        return MemorySection(memories)


@dataclasses.dataclass
class GlobalSection(ModuleSection):
    """A global section."""

    global_vars: list[Global]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a global section."""
        num_globals = leb128.u.decode_reader(f)[0]
        global_vars = [Global.read(f) for _ in range(num_globals)]
        print(f"Read globals: {global_vars}")
        return GlobalSection(global_vars)


@dataclasses.dataclass
class ExportSection(ModuleSection):
    """An export section."""

    exports: list[Export]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns an export section."""
        num_exports = leb128.u.decode_reader(f)[0]
        exports = [Export.read(f) for _ in range(num_exports)]
        print(f"Read exports: {exports}")
        return ExportSection(exports)


@dataclasses.dataclass
class StartSection(ModuleSection):
    """A start section."""

    start_idx: int

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a start section."""
        start_idx = leb128.u.decode_reader(f)[0]
        print(f"Read start: {start_idx}")
        return StartSection(start_idx)


@dataclasses.dataclass
class ElementSection(ModuleSection):
    """An element section."""

    elements: list[Element]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns an element section."""
        num_elements = leb128.u.decode_reader(f)[0]
        elements = [Element.read(f) for _ in range(num_elements)]
        print(f"Read elements: {elements}")
        return ElementSection(elements)


@dataclasses.dataclass
class CodeSection(ModuleSection):
    """A code section."""

    code: list[Code]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a code section."""
        num_code = leb128.u.decode_reader(f)[0]
        code = [Code.read(f) for _ in range(num_code)]
        print(f"Read code (len {len(code)})")
        return CodeSection(code)


@dataclasses.dataclass
class DataSection(ModuleSection):
    """A data section."""

    data: list[Data]

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a data section."""
        num_data = leb128.u.decode_reader(f)[0]
        data = [Data.read(f) for _ in range(num_data)]
        print(f"Read data: {data}")
        return DataSection(data)


@dataclasses.dataclass
class DataCountSection(ModuleSection):
    """A data count section."""

    data_count: int

    @staticmethod
    def read(f: BytesIO) -> ModuleSection:
        """Reads and returns a data count section."""
        data_count = leb128.u.decode_reader(f)[0]
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
    def read(f: BinaryIO) -> Module:
        """Reads a module from a binary stream."""
        if f.read(4) != b"\x00\x61\x73\x6D":
            raise ValueError("Magic number (0061736D) not found.")
        version = f.read(4)
        if version != b"\x01\x00\x00\x00":
            raise ValueError(f"Unsupported version {binascii.hexlify(version)}.")

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
        type_section: TypeSection = module.sections[TypeSection]
        import_section: ImportSection = module.sections[ImportSection]
        for import_ in import_section.imports:
            if isinstance(import_.desc, int):
                import_.desc = type_section.types[import_.desc]

        # Expand func section with code section
        func_section: FunctionSection = module.sections[FunctionSection]
        code_section: CodeSection = module.sections[CodeSection]
        assert len(func_section.funcs) == len(code_section.code)
        for i, func in enumerate(func_section.funcs):
            # "Unpack" the local var types.
            func.local_vars = []
            print(f"Fixup func {i}, local vars {code_section.code[i].local_vars}")
            for local in code_section.code[i].local_vars:
                func.local_vars.extend([local[1]] * local[0])
            print(f"  local_vars now {func.local_vars}")
            func.body = code_section.code[i].insns
        del module.sections[CodeSection]

        return module

    @staticmethod
    def read_section(f: BinaryIO) -> ModuleSection:
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
            section_id = f.read(1)[0]
        except IndexError as e:
            raise EOFError from e

        if section_id > len(section_types):
            raise ValueError(f"Unknown section ID {section_id}")

        section_len = leb128.u.decode_reader(f)[0]
        print(f"Reading section {section_types[section_id]} length {section_len}")
        section_data = BytesIO(f.read(section_len))

        return section_types[section_id].read(section_data)
