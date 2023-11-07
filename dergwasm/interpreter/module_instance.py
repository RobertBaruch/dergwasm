"""An instance of a module."""

# pylint: disable=too-many-branches,too-many-locals

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

from dergwasm.interpreter.binary import (
    DataSection,
    ElementSection,
    ExportSection,
    ExternalType,
    GlobalSection,
    GlobalType,
    ImportSection,
    MemType,
    MemorySection,
    Module,
    FuncType,
    TableType,
    TypeSection,
    FunctionSection,
    TableSection,
)
from dergwasm.interpreter import insn_eval
from dergwasm.interpreter.machine import (
    Machine,
    MemInstance,
    ModuleFuncInstance,
    TableInstance,
    GlobalInstance,
    ElementSegmentInstance,
)
from dergwasm.interpreter import values
from dergwasm.interpreter.insn import Instruction, InstructionType


class ModuleInstance:
    """Instance of a module."""

    # Function instances, table instances, memory instances, and global instances are
    # referenced with an indirection through their respective addresses in the store
    # (the "machine").
    module: Module
    func_types: list[FuncType]
    funcaddrs: list[int]  # Module's func idx -> Machine's func addr
    tableaddrs: list[int]
    memaddrs: list[int]
    globaladdrs: list[int]
    dataaddrs: list[int]
    elementaddrs: list[int]
    exports: dict[str, values.RefVal]

    def __init__(self, module: Module) -> None:
        self.module = module
        self.func_types = []
        self.funcaddrs = []
        self.tableaddrs = []
        self.memaddrs = []
        self.globaladdrs = []
        self.dataaddrs = []
        self.elementaddrs = []
        self.exports = {}

    def allocate(self, machine_inst: Machine, externvals: list[values.RefVal]) -> None:
        """Allocates the module."""

        # Copy over the func_types.
        type_section = self.module.get_section(TypeSection)
        self.func_types = type_section.types

        # Allocate functions.
        function_section = self.module.get_section(FunctionSection)

        for func_spec in function_section.funcs:
            func_type = type_section.types[func_spec.typeidx]
            func = ModuleFuncInstance(
                func_type, self, func_spec.local_var_types, func_spec.body
            )
            self.funcaddrs.append(machine_inst.add_func(func))
        print(f"Allocated {len(self.funcaddrs)} functions")

        # Allocate tables.
        table_section = self.module.get_section(TableSection)
        for table_spec in table_section.tables:
            table = TableInstance(
                table_spec.table_type,
                [
                    values.Value(table_spec.table_type.reftype, None)
                    for _ in range(table_spec.table_type.limits.min)
                ],
            )
            self.tableaddrs.append(machine_inst.add_table(table))
        print(f"Allocated {len(self.tableaddrs)} tables")

        # Allocate memories.
        memory_section = self.module.get_section(MemorySection)
        for memory_spec in memory_section.memories:
            mem = MemInstance(
                memory_spec.mem_type, bytearray(memory_spec.mem_type.limits.min * 65536)
            )  # zero filled
            self.memaddrs.append(machine_inst.add_mem(mem))
        print(f"Allocated {len(self.memaddrs)} memories")

        # Allocate globals.
        global_section = self.module.get_section(GlobalSection)
        for global_spec in global_section.global_vars:
            default_value: int | None = 0  # Applies to i32, i64, f32, f64, v128
            if (
                global_spec.global_type.value_type == values.ValueType.FUNCREF
                or global_spec.global_type.value_type == values.ValueType.EXTERNREF
            ):
                default_value = None
            global_value = GlobalInstance(
                global_spec.global_type,
                global_spec.init,
                values.Value(global_spec.global_type.value_type, default_value),
            )
            self.globaladdrs.append(machine_inst.add_global(global_value))
        print(f"Allocated {len(self.globaladdrs)} globals")

        # Allocate element segments.
        element_section = self.module.get_section(ElementSection)
        for element_segment in element_section.elements:
            segment = ElementSegmentInstance(
                element_segment.elem_type,
                element_segment.offset_expr,
                element_segment.tableidx,
                element_segment.elem_indexes,
                element_segment.elem_exprs,
                element_segment.is_active,
                element_segment.is_passive,
                element_segment.is_declarative,
                [
                    values.Value(element_segment.elem_type, None)
                    for _ in range(element_segment.size())
                ],
            )
            self.elementaddrs.append(machine_inst.add_element(segment))
        print(f"Allocated {len(self.elementaddrs)} element segments")

        # Allocate data segments.
        data_section = self.module.get_section(DataSection)
        for data_spec in data_section.data:
            self.dataaddrs.append(machine_inst.add_data(data_spec.data))
        print(f"Allocated {len(self.dataaddrs)} data segments")

        # Prepend the external funcs, tables, mems, and globals. These always come
        # first.
        prepend_funcaddrs = []
        prepend_tableaddrs = []
        prepend_memaddrs = []
        prepend_globaladdrs = []
        for externval in externvals:
            if externval.addr is None:
                raise ValueError(f"Externval {externval} has null address")
            if externval.val_type == values.RefValType.EXTERN_FUNC:
                prepend_funcaddrs.append(externval.addr)
            elif externval.val_type == values.RefValType.EXTERN_TABLE:
                prepend_tableaddrs.append(externval.addr)
            elif externval.val_type == values.RefValType.EXTERN_MEMORY:
                prepend_memaddrs.append(externval.addr)
            elif externval.val_type == values.RefValType.EXTERN_GLOBAL:
                prepend_globaladdrs.append(externval.addr)
            else:
                raise ValueError(f"Unknown externval type {externval.val_type}")

        self.funcaddrs = prepend_funcaddrs + self.funcaddrs
        self.tableaddrs = prepend_tableaddrs + self.tableaddrs
        self.memaddrs = prepend_memaddrs + self.memaddrs
        self.globaladdrs = prepend_globaladdrs + self.globaladdrs
        print(f"With externvals, allocated {len(self.funcaddrs)} functions")
        print(f"With externvals, allocated {len(self.tableaddrs)} tables")
        print(f"With externvals, allocated {len(self.memaddrs)} memories")
        print(f"With externvals, allocated {len(self.globaladdrs)} globals")

        # Add the exports.
        export_section = self.module.get_section(ExportSection)
        for export in export_section.exports:
            if export.desc_type == FuncType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_FUNC, self.funcaddrs[export.desc_idx]
                )
            elif export.desc_type == TableType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_TABLE, self.tableaddrs[export.desc_idx]
                )
            elif export.desc_type == MemType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_MEMORY, self.memaddrs[export.desc_idx]
                )
            elif export.desc_type == GlobalType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_GLOBAL, self.globaladdrs[export.desc_idx]
                )
            else:
                raise ValueError(f"Unknown export desc type {export.desc_type}")

    @staticmethod
    def instantiate(
        module: Module,
        externvals: list[values.RefVal],
        machine: Machine,
    ) -> ModuleInstance:
        """Instantiates the module.

        Args:
          externvals: A list of external values to use for the module's required
            imports, in the order of those imports.
        """
        required_imports = []
        for _import in module.get_section(ImportSection).imports:
            if isinstance(_import.desc, int):
                required_imports.append(
                    f"{_import.module}:{_import.name} "
                    f"{module.get_section(TypeSection).types[_import.desc]}"
                )
            else:
                required_imports.append(
                    f"{_import.module}:{_import.name} {_import.desc}"
                )

        # Check that the number of externvals matches the number of imports.
        if len(externvals) != len(module.get_section(ImportSection).imports):
            raise ValueError(
                f"Expected {len(module.get_section(ImportSection).imports)} imports, "
                f"but got {len(externvals)} externalvals\n"
                f"Required imports:\n{required_imports}"
            )

        # Check that the externval FuncTypes match the required import FuncTypes.
        for i, externval in enumerate(externvals):
            # Look up the externval's type.
            addr = externval.addr
            if addr is None:
                raise ValueError(f"Externval {externval} has null address")
            externtype: ExternalType | None
            if externval.val_type == values.RefValType.EXTERN_FUNC:
                externtype = machine.get_func(addr).functype
            elif externval.val_type == values.RefValType.EXTERN_TABLE:
                externtype = None  # TODO: After defining TableInstance
            elif externval.val_type == values.RefValType.EXTERN_MEMORY:
                externtype = None  # TODO: Return memory limits
            elif externval.val_type == values.RefValType.EXTERN_GLOBAL:
                externtype = machine.get_global(addr).global_type

            import_section = module.get_section(ImportSection)
            import_desc_type = import_section.imports[i].desc
            if import_desc_type != externtype:
                raise ValueError(
                    f"Expected externval for import {required_imports[i]} "
                    f"to be {import_desc_type}, but got {externtype}\n"
                )

        instance = ModuleInstance(module)
        instance.allocate(machine, externvals)

        # First initialize the globals.
        # Push a frame (the "initial" frame) onto the stack with no local vars.

        # Determine the value of each global by running its init code.
        global_section = module.get_section(GlobalSection)
        for i, g in enumerate(global_section.global_vars):
            # Run the init code block.
            machine.new_frame(values.Frame(1, [], instance, -1))
            machine.push(values.Label(1, len(g.init)))
            machine.execute_expr(g.init)
            # Pop the result off the stack and set the global with it. Strictly
            # speaking, after determining the init values for the globals, we're
            # supposed to allocate  a *new* instance of the module, but this time
            # with the global initialization values. I really don't see the point in
            # doing that, but possibly that's just the standard enforcing that
            # immutable globals can only be initialized, not set.
            machine.set_global(instance.globaladdrs[i], machine.pop_value())
            machine.clear_stack()
            print(f"Initialized global idx {i} (addr {instance.globaladdrs[i]})")

        # Populate the ref lists in each element section.
        element_section = module.get_section(ElementSection)
        for i, s in enumerate(element_section.elements):
            segment_instance = machine.get_element(instance.elementaddrs[i])
            if segment_instance is None:
                raise ValueError(f"Element segment {i} is null during instantiation")
            if s.elem_indexes is not None:
                for j, idx in enumerate(s.elem_indexes):
                    segment_instance.refs[j].value = idx
                print(
                    f"Initialized element segment {i} with {len(s.elem_indexes)} "
                    "indices"
                )
            else:
                if s.elem_exprs is None:
                    raise ValueError(
                        f"Element segment {i} has neither elem_indexes nor elem_exprs"
                    )
                machine.new_frame(values.Frame(1, [], instance, -1))
                for j, expr in enumerate(s.elem_exprs):
                    machine.execute_expr(expr)
                    segment_instance.refs[j] = machine.pop_value()
                machine.clear_stack()
                print(f"Initialized element segment {i} with {len(s.elem_exprs)} exprs")

        # For each active element segment, copy the segment into its table.
        for i, s in enumerate(element_section.elements):
            segment_instance = machine.get_element(instance.elementaddrs[i])
            if segment_instance is None:
                raise ValueError(f"Element segment {i} is null during instantiation")
            if segment_instance.is_active:
                if segment_instance.offset_expr is None:
                    raise ValueError(
                        f"Element segment {i} is active but has no offset_expr"
                    )
                if segment_instance.tableidx is None:
                    raise ValueError(
                        f"Element segment {i} is active but has no tableidx"
                    )
                n = values.Value(values.ValueType.I32, len(segment_instance.refs))
                machine.new_frame(values.Frame(1, [], instance, -1))
                machine.push(values.Label(1, len(segment_instance.offset_expr)))
                machine.execute_expr(segment_instance.offset_expr)
                offset = machine.pop()
                machine.clear_stack()

                machine.push(offset)
                machine.push(values.Value(values.ValueType.I32, 0))
                machine.push(n)
                insn_eval.eval_insn(
                    machine,
                    Instruction(
                        InstructionType.TABLE_INIT, [segment_instance.tableidx, i], 0, 0
                    ),
                )
                insn_eval.eval_insn(
                    machine, Instruction(InstructionType.ELEM_DROP, [i], 0, 0)
                )
                print(
                    f"Copied active element segment {i} into table "
                    f"{segment_instance.tableidx} (offset {offset}, length {n})"
                )

        # For each declarative element segment, drop it (?).
        for i, s in enumerate(element_section.elements):
            segment_instance = machine.get_element(instance.elementaddrs[i])
            if segment_instance is not None and segment_instance.is_declarative:
                insn_eval.eval_insn(
                    machine, Instruction(InstructionType.ELEM_DROP, [i], 0, 0)
                )
                print(f"Dropped declarative element segment {i}")

        # For each active data segment, copy the data into memory.
        data_section = module.get_section(DataSection)
        for i, d in enumerate(data_section.data):
            if not d.is_active:
                continue
            if d.memidx != 0:
                raise ValueError(
                    f"Only one memory is supported, got data memidx {d.memidx}"
                )
            # Run the offset expression. By definition, it is not None. Also by
            # validation, it returns a value.
            assert d.offset is not None
            machine.new_frame(values.Frame(1, [], instance, -1))
            machine.push(values.Label(1, len(d.offset)))
            machine.execute_expr(d.offset)
            offset = machine.peek()
            machine.push(values.Value(values.ValueType.I32, 0))
            machine.push(values.Value(values.ValueType.I32, len(d.data)))
            insn_eval.memory_init(
                machine, Instruction(InstructionType.MEMORY_INIT, [i, 0], 0, 0)
            )
            insn_eval.data_drop(
                machine, Instruction(InstructionType.DATA_DROP, [i], 0, 0)
            )
            machine.clear_stack()
            print(
                f"Copied active data {i} into memory (offset {offset} len "
                f"{len(d.data)})"
            )

        # Execute the start function
        # start_section: module.StartSection = module.sections[module.StartSection]

        machine.clear_stack()
        return instance
