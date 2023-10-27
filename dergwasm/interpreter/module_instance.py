"""An instance of a module."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

from dergwasm.interpreter import binary
from dergwasm.interpreter import insn_eval
from dergwasm.interpreter.machine import Machine, ModuleFuncInstance, TableInstance, GlobalInstance
from dergwasm.interpreter import values
from dergwasm.interpreter.insn import Instruction, InstructionType


class ModuleInstance:
    """Instance of a module."""

    # Function instances, table instances, memory instances, and global instances are
    # referenced with an indirection through their respective addresses in the store
    # (the "machine").
    module: binary.Module
    func_types: list[binary.FuncType]
    funcaddrs: list[int]  # Module's func idx -> Machine's func "addr" (idx)
    tableaddrs: list[int]
    memaddrs: list[int]
    globaladdrs: list[int]
    dataaddrs: list[int]
    exports: dict[str, values.RefVal]

    def __init__(self, module: binary.Module) -> None:
        self.module = module
        self.func_types = []
        self.funcaddrs = []
        self.tableaddrs = []
        self.memaddrs = []
        self.globaladdrs = []
        self.dataaddrs = []
        self.exports = {}

    def allocate(
        self, machine_inst: Machine, externvals: list[values.RefVal]
    ) -> None:
        """Allocates the module."""

        # Allocate functions.
        function_section: binary.FunctionSection = self.module.sections[
            binary.FunctionSection
        ]
        type_section: binary.TypeSection = self.module.sections[binary.TypeSection]

        for func_spec in function_section.funcs:
            func_type = type_section.types[func_spec.typeidx]
            func = ModuleFuncInstance(
                func_type, self, func_spec.local_vars, func_spec.body
            )
            self.funcaddrs.append(machine_inst.add_func(func))

        # Allocate tables.
        table_section: binary.TableSection = self.module.sections[binary.TableSection]
        for table_spec in table_section.tables:
            table = TableInstance(table_spec)
            self.tableaddrs.append(machine_inst.add_table(table))

        # Allocate memories.
        memory_section: binary.MemorySection = self.module.sections[
            binary.MemorySection
        ]
        for memory_spec in memory_section.memories:
            mem = bytearray(memory_spec.mem_type.min_limit * 65536)
            self.memaddrs.append(machine_inst.add_mem(mem))

        # Allocate globals.
        global_section: binary.GlobalSection = self.module.sections[
            binary.GlobalSection
        ]
        for global_spec in global_section.global_vars:
            default_value = 0
            if (
                global_spec.global_type.value_type == values.ValueType.FUNCREF
                or global_spec.global_type.value_type == values.ValueType.EXTERNREF
            ):
                default_value = values.RefVal(global_spec.global_type.value_type, None)
            global_value = GlobalInstance(
                global_spec.global_type.value_type, global_spec.init, default_value
            )
            self.globaladdrs.append(machine_inst.add_global(global_value))

        # Allocate element segments.
        # TODO: Skip for now.

        # Allocate data segments.
        data_section: binary.DataSection = self.module.sections[binary.DataSection]
        for data_spec in data_section.data:
            self.dataaddrs.append(machine_inst.add_data(data_spec.data))

        # Prepend the external funcs, tables, mems, and globals.
        prepend_funcaddrs = []
        prepend_tableaddrs = []
        prepend_memaddrs = []
        prepend_globaladdrs = []
        for externval in externvals:
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

        # Add the exports.
        export_section: binary.ExportSection = self.module.sections[
            binary.ExportSection
        ]
        for export in export_section.exports:
            if export.desc_type == binary.FuncType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_FUNC, self.funcaddrs[export.desc_idx]
                )
            elif export.desc_type == binary.TableType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_TABLE, self.tableaddrs[export.desc_idx]
                )
            elif export.desc_type == binary.MemType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_MEMORY, self.memaddrs[export.desc_idx]
                )
            elif export.desc_type == binary.GlobalType:
                self.exports[export.name] = values.RefVal(
                    values.RefValType.EXTERN_GLOBAL, self.globaladdrs[export.desc_idx]
                )
            else:
                raise ValueError(f"Unknown export desc type {export.desc_type}")

    @staticmethod
    def instantiate(
        module: binary.Module,
        externvals: list[values.RefVal],
        machine: Machine,
    ) -> ModuleInstance:
        """Instantiates the module.

        Args:
          externvals: A list of external values to use for the module's required
            imports, in the order of those imports.
        """
        required_imports = []
        for i in module.sections[binary.ImportSection].imports:
            if isinstance(i.desc, int):
                required_imports.append(
                    f"{i.module}:{i.name} {module.sections[binary.TypeSection].types[i.desc]}"
                )
            else:
                required_imports.append(f"{i.module}:{i.name} {i.desc}")

        # Check that the number of externvals matches the number of imports.
        if len(externvals) != len(module.sections[binary.ImportSection].imports):
            raise ValueError(
                f"Expected {len(module.sections[binary.ImportSection].imports)} imports, "
                f"but got {len(externvals)} externalvals\n"
                f"Required imports:\n{required_imports}"
            )

        # Check that the externval FuncTypes match the required import FuncTypes.
        for i, externval in enumerate(externvals):
            # Look up the externval's type.
            addr = externval.addr
            externtype: binary.ExternalType
            if externval.val_type == values.RefValType.EXTERN_FUNC:
                externtype = machine.get_func(addr).functype
            elif externval.val_type == values.RefValType.EXTERN_TABLE:
                externtype = None  # TODO: After defining TableInstance
            elif externval.val_type == values.RefValType.EXTERN_MEMORY:
                externtype = None  # TODO: Return memory limits
            elif externval.val_type == values.RefValType.EXTERN_GLOBAL:
                externtype = machine.get_global(addr).global_type

            import_section: binary.ImportSection = module.sections[binary.ImportSection]
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
        machine.new_frame(values.Frame(0, [], instance, 0))

        # Determine the value of each global by running its init code.
        global_section: binary.GlobalSection = module.sections[binary.GlobalSection]
        for i, g in enumerate(global_section.global_vars):
            # Run the init code block.
            machine.execute_block(g.init)
            # Pop the result off the stack and set the global with it. Strictly
            # speaking, after determining the init values for the globals, we're
            # supposed to allocate  a *new* instance of the module, but this time
            # with the global initialization values. I really don't see the point in
            # doing that, but possibly that's just the standard enforcing that
            # immutable globals can only be initialized, not set.
            machine.set_global(instance.globaladdrs[i], machine.pop())

        # Next get a list of reference vectors determined by the element sections.
        # TODO: We're skipping this step for now.

        machine.clear_stack()

        # Skip elements again

        # For each active data segment, copy the data into memory.
        data_section: binary.DataSection = module.sections[binary.DataSection]
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
            machine.new_frame(values.Frame(1, [], instance, 0))
            machine.push(values.Label(1, len(d.offset)))
            machine.execute_seq(d.offset)
            machine.push(values.Value(values.ValueType.I32, 0))
            machine.push(values.Value(values.ValueType.I32, len(d.data)))
            insn_eval.memory_init(machine,
                                  Instruction(InstructionType.MEMORY_INIT, [i, 0], 0, 0))
            insn_eval.data_drop(machine,
                                Instruction(InstructionType.DATA_DROP, [i], 0, 0))
            machine.clear_stack()

        # Execute the start function
        # start_section: module.StartSection = module.sections[module.StartSection]

        machine.clear_stack()
        return instance
