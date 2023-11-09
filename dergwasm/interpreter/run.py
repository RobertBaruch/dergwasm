"""Main entry point for the interpreter."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=unused-argument
# pylint: disable=invalid-name

import struct
import sys
from typing import cast

from dergwasm.interpreter import binary
from dergwasm.interpreter.machine import Machine, HostFuncInstance, ModuleFuncInstance
from dergwasm.interpreter.machine_impl import MachineImpl
from dergwasm.interpreter import values
from dergwasm.interpreter.module_instance import ModuleInstance


# env.emscripten_memcpy_js
# (type (;4;) (func (param i32 i32 i32)))
def emscripten_memcpy_js(machine: Machine, dest: int, src: int, n: int) -> None:
    print(f"Called emscripten_memcpy_js({dest}, {src}, {n})")
    machine_data = machine.get_mem_data(0)
    machine_data[dest : dest + n] = machine_data[src : src + n]


# wasi_snapshot_preview1.fd_write
# (type (;5;) (func (param i32 i32 i32 i32) (result i32)))
# __wasi_errno_t __wasi_fd_write(
# __wasi_fd_t fd,
#
# /**
#  * List of scatter/gather vectors from which to retrieve data.
#  */
# const __wasi_ciovec_t *iovs,
#
# /**
#  * The length of the array pointed to by `iovs`.
#  */
# size_t iovs_len,
#
# /**
#  * The number of bytes written.
#  */
# __wasi_size_t *nwritten
# )
#
# /**
#  * A region of memory for scatter/gather writes.
#  */
# typedef struct __wasi_ciovec_t {
#     /**
#      * The address of the buffer to be written.
#      */
#     const uint8_t * buf;
#
#     /**
#      * The length of the buffer to be written.
#      */
#     __wasi_size_t buf_len;
#
# } __wasi_ciovec_t;
# See: https://wasix.org/docs/api-reference/wasi/fd_write
def fd_write(
    machine: Machine, fd: int, iovs: int, iovs_len: int, nwritten_ptr: int
) -> int:
    print(f"Called fd_write({fd}, 0x{iovs:08X}, {iovs_len}, 0x{nwritten_ptr:08X})")
    machine_data = machine.get_mem_data(0)
    ptr = iovs
    nwritten = 0
    for i in range(iovs_len):
        buf = struct.unpack("<I", machine_data[ptr : ptr + 4])[0]
        buf_len = struct.unpack("<I", machine_data[ptr + 4 : ptr + 8])[0]
        print(f"  iovs[{i}]: buf=0x{buf:08X}, buf_len={buf_len} (0x{buf_len:08X})")
        if buf_len > 0:
            buf_data = machine_data[buf : buf + buf_len]
            print(f"    buf_data={buf_data}")
            s = buf_data.decode("utf-8")
            print(s)
            nwritten += buf_len
        ptr += 8
    print(f"  nwritten={nwritten}")
    struct.pack_into("<I", machine_data, nwritten_ptr, nwritten)
    return 0


# wasi_snapshot_preview1.fd_seek
def fd_seek(
    machine: Machine, fd: int, offset: int, whence: int, newoffset_ptr: int
) -> int:
    print(f"Called fd_seek({fd=}, {offset=}, {whence=}, 0x{newoffset_ptr:08X})")
    return 0


# wasi_snapshot_preview1.fd_read
def fd_read(machine: Machine, fd: int, iovs: int, iovs_len: int, nread_ptr: int) -> int:
    print(f"Called fd_read({fd}, 0x{iovs:08X}, {iovs_len}, 0x{nread_ptr:08X})")
    return 0


# wasi_snapshot_preview1.fd_close
def fd_close(machine: Machine, fd: int) -> int:
    print(f"Called fd_close({fd})")
    return 0


def environ_get(machine: Machine, environ_ptr_ptr: int, environ_buf: int) -> int:
    print(f"Called environ_get(0x{environ_ptr_ptr:08X}, 0x{environ_buf:08X})")
    return 0


def environ_sizes_get(machine: Machine, argc_ptr: int, argv_bug_size_ptr: int) -> int:
    print(f"Called environ_sizes_get(0x{argc_ptr:08X}, 0x{argv_bug_size_ptr:08X})")
    return 0


# wasi_snapshot_preview1.proc_exit
def proc_exit(machine: Machine, exit_code: int) -> None:
    print("Called proc_exit()")
    sys.exit(0)


def run() -> None:
    """Runs the interpreter."""

    host_funcs_by_name: dict(str, HostFuncInstance) = {
        "env.emscripten_memcpy_js": HostFuncInstance(
            binary.FuncType([values.ValueType.I32] * 3, []), emscripten_memcpy_js
        ),
        "wasi_snapshot_preview1.fd_write": HostFuncInstance(
            binary.FuncType([values.ValueType.I32] * 4, [values.ValueType.I32]),
            fd_write,
        ),
        "wasi_snapshot_preview1.fd_read": HostFuncInstance(
            binary.FuncType([values.ValueType.I32] * 4, [values.ValueType.I32]),
            fd_read,
        ),
        "wasi_snapshot_preview1.fd_seek": HostFuncInstance(
            binary.FuncType(
                [
                    values.ValueType.I32,
                    values.ValueType.I64,
                    values.ValueType.I32,
                    values.ValueType.I32,
                ],
                [values.ValueType.I32],
            ),
            fd_seek,
        ),
        "wasi_snapshot_preview1.fd_close": HostFuncInstance(
            binary.FuncType([values.ValueType.I32], [values.ValueType.I32]), fd_close
        ),
        "wasi_snapshot_preview1.environ_get": HostFuncInstance(
            binary.FuncType([values.ValueType.I32] * 2, [values.ValueType.I32]),
            environ_get,
        ),
        "wasi_snapshot_preview1.environ_sizes_get": HostFuncInstance(
            binary.FuncType([values.ValueType.I32] * 2, [values.ValueType.I32]),
            environ_sizes_get,
        ),
        "wasi_snapshot_preview1.proc_exit": HostFuncInstance(
            binary.FuncType([values.ValueType.I32], []), proc_exit
        ),
    }

    machine_impl = MachineImpl()

    module = binary.Module.from_file("F:/dergwasm/hello_world_cpp.wasm")
    types_section = cast(binary.TypeSection, module.sections[binary.TypeSection])
    func_indexes = []
    print("Exports from the module:")
    export_section = cast(binary.ExportSection, module.sections[binary.ExportSection])
    for export in export_section.exports:
        print(f"{export.name}: {export.desc_type} {export.desc_idx}")

    print("Required imports to the module:")
    import_section = cast(binary.ImportSection, module.sections[binary.ImportSection])
    for import_ in import_section.imports:
        t = (
            types_section.types[import_.desc]
            if isinstance(import_.desc, int)
            else import_.desc
        )
        import_name = f"{import_.module}.{import_.name}"
        print(f"{import_name}: {t}")
        if import_name not in host_funcs_by_name:
            raise ValueError(f"Missing host func for {import_name}")
        host_func = host_funcs_by_name[import_name]
        if host_func.functype != t:
            raise ValueError(
                f"Host func {import_name} has type {host_func.functype}, "
                f"but the module expected {t}."
            )
        func_indexes.append(machine_impl.add_func(host_func))

    module_inst = ModuleInstance.instantiate(
        module,
        [
            values.RefVal(values.RefValType.EXTERN_FUNC, i)
            for i in range(len(func_indexes))
        ],
        machine_impl,
    )

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

    # main is (func (;3;) (type 7) (param i32 i32) (result i32)
    # Presumably int main(int argc, char *argv[]).

    if "__wasm_call_ctors" in module_inst.exports:
        wasm_call_ctors = module_inst.exports["__wasm_call_ctors"]
        print(f"Calling __wasm_call_ctors() at {wasm_call_ctors.addr}")
        assert wasm_call_ctors.addr is not None
        machine_impl.invoke_func(wasm_call_ctors.addr)
        machine_impl.clear_stack()

    start_name = "main"
    if start_name not in module_inst.exports:
        start_name = "_start"
    addr = module_inst.exports[start_name].addr
    print(f"Invoking function at machine funcaddr {addr}")
    assert addr is not None
    func_instance = machine_impl.get_func(addr)
    assert isinstance(func_instance, ModuleFuncInstance)

    machine_impl.push(values.Value(values.ValueType.I32, 0))
    machine_impl.push(values.Value(values.ValueType.I32, 0))
    machine_impl.invoke_func(addr)
    return_code = machine_impl.pop_value().intval()
    print(f"Return code: {return_code}")


if __name__ == "__main__":
    run()
