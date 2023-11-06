"""Main entry point for the interpreter."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=unused-argument
# pylint: disable=invalid-name

import math
from typing import cast

from dergwasm.interpreter import binary
from dergwasm.interpreter import machine
from dergwasm.interpreter.machine_impl import MachineImpl
from dergwasm.interpreter import values
from dergwasm.interpreter.module_instance import ModuleInstance


def Math_atan(x: float) -> float:
    return math.atan(x)


def clear_screen() -> None:
    pass


def cos(x: float) -> float:
    return math.cos(x)


def draw_bullet(x: float, y: float) -> None:
    pass


def draw_enemy(x: float, y: float) -> None:
    pass


def draw_particle(x: float, y: float, r: float) -> None:
    pass


def draw_player(x: float, y: float, angle: float) -> None:
    pass


def draw_score(score: float) -> None:
    pass


def sin(x: float) -> float:
    return math.sin(x)


def run() -> None:
    """Runs the interpreter."""
    machine_impl = MachineImpl()

    # Add all host functions to the machine.
    Math_atan_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64], [values.ValueType.F64]), Math_atan
        )
    )
    clear_screen_idx = machine_impl.add_func(
        machine.HostFuncInstance(binary.FuncType([], []), clear_screen)
    )
    cos_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64], [values.ValueType.F64]), cos
        )
    )
    draw_bullet_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64, values.ValueType.F64], []),
            draw_bullet,
        )
    )
    draw_enemy_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64, values.ValueType.F64], []),
            draw_enemy,
        )
    )
    draw_particle_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType(
                [values.ValueType.F64, values.ValueType.F64, values.ValueType.F64], []
            ),
            draw_particle,
        )
    )
    draw_player_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType(
                [values.ValueType.F64, values.ValueType.F64, values.ValueType.F64], []
            ),
            draw_player,
        )
    )
    draw_score_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64], []), draw_score
        )
    )
    sin_idx = machine_impl.add_func(
        machine.HostFuncInstance(
            binary.FuncType([values.ValueType.F64], [values.ValueType.F64]), sin
        )
    )

    module = binary.Module.from_file("F:/dergwasm/index.wasm")
    print("Required imports to the module:")
    import_section = cast(binary.ImportSection, module.sections[binary.ImportSection])
    types_section = cast(binary.TypeSection, module.sections[binary.TypeSection])
    for import_ in import_section.imports:
        t = (
            types_section.types[import_.desc]
            if isinstance(import_.desc, int)
            else import_.desc
        )
        print(f"{import_.module}.{import_.name}: {t}")

    module_inst = ModuleInstance.instantiate(
        module,
        [
            # These are the imports that the module needs to access, but does not
            # provide. They must match one-to-one with the module's import specs.
            #
            # Since this is an ordered list, and we wouldn't actually know beforehand
            # what the order of imports are, ideally we would key our host functions
            # off of the name, and match them up to the import names.
            values.RefVal(values.RefValType.EXTERN_FUNC, Math_atan_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, clear_screen_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, cos_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, draw_bullet_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, draw_enemy_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, draw_particle_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, draw_player_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, draw_score_idx),
            values.RefVal(values.RefValType.EXTERN_FUNC, sin_idx),
        ],
        machine_impl,
    )
    print("Exports from the module:")
    for export, val in module_inst.exports.items():
        print(f"{export}: {val}")
        if val.val_type == values.RefValType.EXTERN_FUNC:
            if val.addr is None:
                print("  = null ref")
            else:
                print(f"  = {machine_impl.get_func(val.addr).functype}")

    # print("Functions in the machine:")
    # for i, f in enumerate(machine_impl.funcs):
    #     print(f"{i}: {type(f)}: {f.functype}")
    # print("Function indexes in the module:")
    # for i, f in enumerate(module_inst.funcaddrs):
    #     ff = machine_impl.get_func(f)
    #     print(f"{i}: {f} = {type(ff)}: {ff.functype}")

    draw_addr = module_inst.exports["update"].addr
    print(f"Invoking function at machine funcaddr {draw_addr}")
    assert draw_addr is not None
    draw = machine_impl.get_func(draw_addr)
    assert isinstance(draw, machine.ModuleFuncInstance)

    machine_impl.push(values.Value(values.ValueType.F64, 1000))
    machine_impl.invoke_func(draw_addr)


if __name__ == "__main__":
    run()
