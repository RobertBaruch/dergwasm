"""Main entry point for the interpreter."""

# pylint: disable=missing-function-docstring,missing-class-docstring
# pylint: disable=unused-argument
# pylint: disable=invalid-name

import struct
import sys
import time
from typing import Callable, cast

from dergwasm.interpreter import binary, insn, insn_eval
from dergwasm.interpreter.machine import Machine, HostFuncInstance, ModuleFuncInstance
from dergwasm.interpreter.machine_impl import MachineImpl
from dergwasm.interpreter import values
from dergwasm.interpreter.module_instance import ModuleInstance

start_time_ms: int = 0


# env.emscripten_memcpy_js
# (type (;4;) (func (param i32 i32 i32)))
def emscripten_memcpy_js(machine: Machine, dest: int, src: int, n: int) -> None:
    print(f"Called emscripten_memcpy_js({dest=}, {src=}, {n=})")
    machine_data = machine.get_mem_data(0)
    machine_data[dest : dest + n] = machine_data[src : src + n]


def emscripten_scan_registers(machine: Machine, em_scan_func: int) -> None:
    """Scan "registers", by which we mean data that is not in memory.

    In Wasm, that means data stored in locals, including locals in functions higher up
    the stack - the Wasm VM has spilled them, but none of that is observable to
    user code).

    Note that this function scans Wasm locals. Depending on the LLVM
    optimization level, this may not scan the original locals in your source
    code. For example in ``-O0`` locals may be stored on the stack. To make
    sure you scan everything necessary, you can also do
    ``emscripten_scan_stack``.

    This function requires Asyncify - it relies on that option to spill the
    local state all the way up the stack. As a result, it will add overhead
    to your program.
    """
    print(f"Called emscripten_scan_registers({em_scan_func=})")
    raise NotImplementedError


def emscripten_resize_heap(machine: Machine, requested_size: int) -> int:
    """Resizes the heap.

    Attempts to geometrically or linearly increase the heap so that it
    grows to the new size of at least `requested_size` bytes. The heap size may
    be overallocated, see src/settings.js variables MEMORY_GROWTH_GEOMETRIC_STEP,
    MEMORY_GROWTH_GEOMETRIC_CAP and MEMORY_GROWTH_LINEAR_STEP. This function
    cannot be used to shrink the size of the heap.

    Args:
        size: The new size of the heap.

    Returns:
        1 on success, 0 on failure.
    """
    print(f"Called emscripten_resize_heap({requested_size=})")
    raise NotImplementedError


def emscripten_throw_longjmp(machine: Machine) -> None:
    """Throws a longjmp.

    Not entirely certain what this does, but firmware.js seems to just return infinity.

    Args:
        buf: The address of the longjmp buffer.
        value: The value to pass to the longjmp.
    """
    print("Called emscripten_throw_longjmp()")
    raise NotImplementedError


# wasi_snapshot_preview1.fd_write
# See: https://wasix.org/docs/api-reference/wasi/fd_write
def fd_write(
    machine: Machine, fd: int, iovs: int, iovs_len: int, nwritten_ptr: int
) -> int:
    """Writes to a file descriptor.

    Args:
        fd: The file descriptor to write to.
        iovs: The address of an array of __wasi_ciovec_t structs. Such a struct is
            simply a pointer and a length.
        iovs_len: The length of the array pointed to by iovs.
        nwritten_ptr: The address of an i32 to store the number of bytes written.

    Returns:
        0 on success, or -ERRNO on failure.
    """
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
            print(buf_data.decode("utf-8"))
            nwritten += buf_len
        ptr += 8
    print(f"  nwritten={nwritten}")
    struct.pack_into("<I", machine_data, nwritten_ptr, nwritten)
    return 0


# wasi_snapshot_preview1.fd_seek
def fd_seek(
    machine: Machine,
    fd: int,
    offset_lo: int,
    offset_hi: int,
    whence: int,
    newoffset_ptr: int,
) -> int:
    """Seeks to a position in a file descriptor.

    Args:
        fd: The file descriptor to seek.
        offset_lo: The low 32 bits of the 64-bit offset to seek to.
        offset_hi: The high 32 bits of the 64-bit offset to seek to.
        whence: The origin of the seek. This is one of:
            0: SEEK_SET (seek from the beginning of the file)
            1: SEEK_CUR (seek from the current position in the file)
            2: SEEK_END (seek from the end of the file)
        newoffset_ptr: The address of an i64 to store the new offset.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    offset = offset_lo | (offset_hi << 32)
    print(f"Called fd_seek({fd=}, {offset=}, {whence=}, 0x{newoffset_ptr:08X})")
    raise NotImplementedError


# wasi_snapshot_preview1.fd_read
def fd_read(machine: Machine, fd: int, iovs: int, iovs_len: int, nread_ptr: int) -> int:
    """Reads from a file descriptor.

    Args:
        fd: The file descriptor to read from.
        iovs: The address of an array of __wasi_ciovec_t structs. Such a struct is
            simply a pointer and a length.
        iovs_len: The length of the array pointed to by iovs.
        nread_ptr: The address of an i32 to store the number of bytes read.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called fd_read({fd}, 0x{iovs:08X}, {iovs_len}, 0x{nread_ptr:08X})")
    raise NotImplementedError


# wasi_snapshot_preview1.fd_close
def fd_close(machine: Machine, fd: int) -> int:
    """Closes a file descriptor.

    Args:
        fd: The file descriptor to close.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called fd_close({fd})")
    raise NotImplementedError


def fd_sync(machine: Machine, fd: int) -> int:
    """Flushes the file descriptor to disk.

    Args:
        fd: The file descriptor to flush.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called fd_sync({fd})")
    raise NotImplementedError


def environ_get(machine: Machine, environ_ptr_ptr: int, environ_buf: int) -> int:
    print(f"Called environ_get(0x{environ_ptr_ptr:08X}, 0x{environ_buf:08X})")
    raise NotImplementedError


def environ_sizes_get(machine: Machine, argc_ptr: int, argv_bug_size_ptr: int) -> int:
    print(f"Called environ_sizes_get(0x{argc_ptr:08X}, 0x{argv_bug_size_ptr:08X})")
    raise NotImplementedError


# wasi_snapshot_preview1.proc_exit
def proc_exit(machine: Machine, exit_code: int) -> None:
    print("Called proc_exit()")
    sys.exit(0)


def mp_js_hook(machine: Machine) -> None:
    """For Micropython, reads a single character from the keyboard."""
    print("Called mp_js_hook()")
    raise NotImplementedError


def mp_js_write(machine: Machine, addr: int, size: int) -> None:
    """Writes the buffer to stdout."""
    print(f"Called mp_js_write(0x{addr:08X}, {size=})")
    data = machine.get_mem_data(0)[addr : addr + size]
    print(f"  data={data}")
    print(data.decode("utf-8"))


def mp_js_ticks_ms(machine: Machine) -> int:
    """Returns the number of milliseconds since the interpreter started."""
    print("Called mp_js_ticks_ms()")
    return (time.time_ns() // 1_000_000) - start_time_ms


# System calls. You can see the implementation of system calls at
# Emscripten's source file system/lib/wasmfs/syscalls.cpp
def syscall_chdir(machine: Machine, path: int) -> int:
    """Changes the current working directory.

    Args:
      path: The address of a null-terminated UTF8-encoded string containing the path to
          change to.

    Returns:
      0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_chdir(0x{path:08X})")
    raise NotImplementedError


def syscall_getcwd(machine: Machine, buf: int, size: int) -> int:
    """Gets the current working directory.

    Args:
      buf: The address of a buffer to store the current working directory.
      size: The size of the buffer.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_getcwd(0x{buf:08X}, {size=})")
    raise NotImplementedError


def syscall_mkdirat(machine: Machine, dir_handle: int, path: int, mode: int) -> int:
    """Creates a directory.

    Args:
      dir_handle: The handle of the directory to create the new directory in.
      path: The address of a null-terminated UTF8-encoded string containing the path to
          create.
      mode: The mode of the new directory.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_mkdirat({dir_handle=}, 0x{path:08X}, {mode=})")
    raise NotImplementedError


def syscall_openat(
    machine: Machine, dir_handle: int, path: int, mode: int, flags: int
) -> int:
    """Opens a file.

    Args:
      dir_handle: The handle of the directory to open the file in.
      path: The address of a null-terminated UTF8-encoded string containing the path to
          open.
      mode: The mode of the new file.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_openat({dir_handle=}, 0x{path:08X}, {mode=}, {flags=})")
    raise NotImplementedError


def syscall_renameat(
    machine: Machine, olddir_handle: int, oldpath: int, newdir_handle: int, newpath: int
) -> int:
    """Renames or moves a file.

    Args:
      olddir_handle: The handle of the directory containing the file to rename.
      oldpath: The address of a null-terminated UTF8-encoded string containing the path to
          the file to rename.
      newdir_handle: The handle of the directory to rename the file in.
      newpath: The address of a null-terminated UTF8-encoded string containing the new path.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(
        f"Called syscall_renameat({olddir_handle=}, 0x{oldpath:08X}, "
        f"{newdir_handle=}, 0x{newpath:08X})"
    )
    raise NotImplementedError


def syscall_poll(machine: Machine, fds: int, nfds: int, timeout: int) -> int:
    """Polls a number of file descriptors, waiting until one has data to read.

    Args:
      fds: The address of an array of file descriptors. Negative fds are ignored.
      nfds: The length of the array pointed to by fds.
      timeout: The timeout, in milliseconds.
    """
    print(f"Called syscall_poll(0x{fds:08X}, {nfds=}, {timeout=})")
    raise NotImplementedError


def syscall_getdents64(
    machine: Machine, dir_handle: int, direntp: int, count: int
) -> int:
    """Reads directory entries from a directory.

    Args:
      dir_handle: The handle of the directory to read from.
      direntp: The address of a buffer to store the directory entries.
      count: The maxiumum number of entires allowed in the dirent buffer.

    Returns:
        The number of dirent bytes read on success, or -ERRNO on failure.
    """
    print(f"Called syscall_getdents64({dir_handle=}, 0x{direntp:08X}, {count=})")
    raise NotImplementedError


def syscall_rmdir(machine: Machine, path: int) -> int:
    """Removes a directory.

    Args:
      path: The address of a null-terminated UTF8-encoded string containing the path to
          remove.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_rmdir(0x{path:08X})")
    raise NotImplementedError


def syscall_newfstatat(
    machine: Machine, fd: int, path: int, statbuf: int, flags: int
) -> int:
    """Gets the status of a file.

    Args:
      fd: If -100, get the status of the file at the path, relative to the current
        directory. Otherwise, ignore the path and get the status of the file given by
        the file descriptor.
      path: The address of a null-terminated UTF8-encoded string containing the path to
          get the status of. Ignored if fd is not -100.
      statbuf: The address of a buffer (a struct stat) to store the status.
      flags: Bitmapped flags to control the operation. The only flags allowed are:
          AT_SYMLINK_NOFOLLOW: 0x0100
          AT_NO_AUTOMOUNT: 0x0800
          AT_EMPTY_PATH: 0x1000

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_newfstatat({fd=}, 0x{path:08X}, 0x{statbuf:08X}, {flags=})")
    raise NotImplementedError


def syscall_stat64(machine: Machine, path: int, statbuf: int) -> int:
    """Gets the status of a file, following symbolic links.

    Args:
      path: The address of a null-terminated UTF8-encoded string containing the path to
          get the status of.
      statbuf: The address of a buffer (a struct stat) to store the status.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    return syscall_newfstatat(machine, -100, path, statbuf, 0)


def syscall_lstat64(machine: Machine, path: int, statbuf: int) -> int:
    """Gets the status of a file, not following symbolic links.

    Args:
      path: The address of a null-terminated UTF8-encoded string containing the path to
          get the status of.
      statbuf: The address of a buffer (a struct stat) to store the status.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    return syscall_newfstatat(machine, -100, path, statbuf, 0x0100)


def syscall_fstat64(machine: Machine, fd: int, statbuf: int) -> int:
    """Gets the status of a file descriptor.

    Args:
      fd: The file descriptor to get the status of.
      statbuf: The address of a buffer (a struct stat) to store the status.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    return syscall_newfstatat(machine, fd, 0, statbuf, 0x1000)


def syscall_statfs64(machine: Machine, path: int, bufsize: int, statbuf: int) -> int:
    """Gets the status of a file.

    Args:
      path: The address of a null-terminated UTF8-encoded string containing the path to
          get the status of.
      bufsize: The size of the buffer pointed to by statbuf.
      statbuf: The address of a buffer (a struct statfs) to store the status.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_statfs64(0x{path:08X}, {bufsize=}, 0x{statbuf:08X})")
    raise NotImplementedError


def syscall_fstatfs64(machine: Machine, fd: int, bufsize: int, statbuf: int) -> int:
    """Gets the status of a file descriptor.

    Args:
      fd: The file descriptor to get the status of.
      bufsize: The size of the buffer pointed to by statbuf.
      statbuf: The address of a buffer (a struct statfs) to store the status.

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_fstatfs64({fd=}, {bufsize=}, 0x{statbuf:08X})")
    raise NotImplementedError


def syscall_unlinkat(machine: Machine, dir_handle: int, path: int, flags: int) -> int:
    """Unlinks a file.

    Args:
      dir_handle: The handle of the directory containing the file to unlink.
      path: The address of a null-terminated UTF8-encoded string containing the path to
          the file to unlink.
      flags: Bitmapped flags to control the operation. The only flags allowed are:
          AT_REMOVEDIR: 0x200

    Returns:
        0 on success, or -ERRNO on failure.
    """
    print(f"Called syscall_unlinkat({dir_handle=}, 0x{path:08X}, {flags=})")
    raise NotImplementedError


# The various env.invoke_* functions invoke a corresponding dynCall_ function in wasm.
# This is primarily used by the Emscripten compiler to implement exceptions and
# setjmp/longjmp.
def invoke_i_is(machine: Machine, *args: int) -> int:
    """Invokes a wasm function taking some number of ints, and returns an int."""
    print(f"Called invoke_i_is({args})")
    # for arg in reversed(args):
    #     machine.push(values.Value(values.ValueType.I32, arg))
    # machine.invoke_func(funcaddr)
    # return machine.pop_value().intval()
    return 0


def invoke_v_is(machine: Machine, *args: int) -> None:
    """Invokes a wasm function taking some number of ints, and returns nothing."""
    print(f"Called invoke_v_is({args})")
    # for arg in reversed(args):
    #     machine.push(values.Value(values.ValueType.I32, arg))
    # machine.invoke_func(funcaddr)


def get_export_func(module_inst: ModuleInstance, machine_impl: MachineImpl, name: str) -> int:
    if name not in module_inst.exports:
        raise ValueError(f"Missing {name} export")

    addr = module_inst.exports[name].addr
    print(f"Invoking {name} at machine funcaddr {addr}")
    assert addr is not None
    func_instance = machine_impl.get_func(addr)
    assert isinstance(func_instance, ModuleFuncInstance)
    return addr


def exported_malloc(module_inst: ModuleInstance, machine_impl: MachineImpl, size: int) -> int:
    """Calls the exported malloc function.

    Args:
        module_inst: The instance of the firmware.wasm module.
        machine_impl: The machine implementation.
        size: The size of the memory to allocate, in bytes.

    Returns:
        The wasm memory address of the allocated memory.
    """
    malloc_addr = get_export_func(module_inst, machine_impl, "malloc")
    machine_impl.push(values.Value(values.ValueType.I32, size))
    machine_impl.invoke_func(malloc_addr)
    return machine_impl.pop_value().intval()


def exported_free(module_inst: ModuleInstance, machine_impl: MachineImpl, addr: int) -> None:
    """Calls the exported free function.

    Args:
        module_inst: The instance of the firmware.wasm module.
        machine_impl: The machine implementation.
        addr: The address of the memory to free.
    """
    free_addr = get_export_func(module_inst, machine_impl, "free")
    machine_impl.push(values.Value(values.ValueType.I32, addr))
    machine_impl.invoke_func(free_addr)


# The MicroPython API
# See https://github.com/micropython/micropython/tree/master/ports/webassembly
# You can see the implementation of these functions at
# https://github.com/micropython/micropython/blob/master/ports/webassembly/main.c

def mp_js_init(module_inst: ModuleInstance, machine_impl: MachineImpl, stack_size: int) -> None:
    """Initializes MicroPython with the given stack size in bytes.

    This must be called before attempting to interact with MicroPython.

    Args:
        module_inst: The instance of the firmware.wasm module.
        machine_impl: The machine implementation.
        stack_size: The size of the stack in bytes.
    """
    addr = get_export_func(module_inst, machine_impl, "mp_js_init")
    machine_impl.push(values.Value(values.ValueType.I32, stack_size))
    machine_impl.invoke_func(addr)


def mp_js_do_str(module_inst: ModuleInstance, machine_impl: MachineImpl, content: str) -> int:
    """Parses the given Python content and executes it.

    Args:
        module_inst: The instance of the firmware.wasm module.
        machine_impl: The machine implementation.
        content: The Python content to execute.

    Returns:
        An exit code.
    """
    addr = get_export_func(module_inst, machine_impl, "mp_js_do_str")

    # The MicroPython implementation has a memory space of 256 x 64k = 16MB.

    data = bytearray(content.encode("utf-8"))
    data.append(0)
    data_len = len(data) + 1

    data_addr = exported_malloc(module_inst, machine_impl, data_len)

    machine_impl.get_mem_data(0)[data_addr : data_addr + data_len] = data

    machine_impl.push(values.Value(values.ValueType.I32, data_addr))
    machine_impl.invoke_func(addr)
    exit_code = machine_impl.pop_value().intval()

    exported_free(module_inst, machine_impl, data_addr)

    return exit_code


host_funcs_by_name: dict[str, HostFuncInstance] = {}


def add_host_func(
    name: str,
    args: list[values.ValueType],
    rets: list[values.ValueType],
    func: Callable,
) -> None:
    """Adds a host function mapping.

    Args:
        name: The name of the function. This is usually {module}.{name}.
        args: The argument types.
        rets: The return types.
        func: The host function to call.
    """
    global host_funcs_by_name
    host_funcs_by_name[name] = HostFuncInstance(
        binary.FuncType(args, rets),
        func,
    )


def add_host_funcs() -> None:
    """Maps the module's required imports to host functions."""
    add_host_func(
        "env.emscripten_memcpy_js", [values.ValueType.I32] * 3, [], emscripten_memcpy_js
    )
    add_host_func(
        "env.emscripten_scan_registers",
        [values.ValueType.I32],
        [],
        emscripten_scan_registers,
    )
    add_host_func(
        "env.emscripten_resize_heap",
        [values.ValueType.I32],
        [values.ValueType.I32],
        emscripten_resize_heap,
    )
    add_host_func(
        "env._emscripten_throw_longjmp",
        [],
        [],
        emscripten_throw_longjmp,
    )
    add_host_func(
        "wasi_snapshot_preview1.fd_write",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        fd_write,
    )
    add_host_func(
        "wasi_snapshot_preview1.fd_read",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        fd_read,
    )
    add_host_func(
        "wasi_snapshot_preview1.fd_seek",
        [values.ValueType.I32] * 5,
        [values.ValueType.I32],
        fd_seek,
    )
    add_host_func(
        "wasi_snapshot_preview1.fd_close",
        [values.ValueType.I32],
        [values.ValueType.I32],
        fd_close,
    )
    add_host_func(
        "wasi_snapshot_preview1.fd_sync",
        [values.ValueType.I32],
        [values.ValueType.I32],
        fd_sync,
    )
    add_host_func(
        "wasi_snapshot_preview1.environ_get",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        environ_get,
    )
    add_host_func(
        "wasi_snapshot_preview1.environ_sizes_get",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        environ_sizes_get,
    )
    add_host_func(
        "wasi_snapshot_preview1.proc_exit", [values.ValueType.I32], [], proc_exit
    )
    add_host_func(
        "env.invoke_i", [values.ValueType.I32], [values.ValueType.I32], invoke_i_is
    )
    add_host_func(
        "env.invoke_ii", [values.ValueType.I32] * 2, [values.ValueType.I32], invoke_i_is
    )
    add_host_func(
        "env.invoke_iii",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        invoke_i_is,
    )
    add_host_func(
        "env.invoke_iiii",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        invoke_i_is,
    )
    add_host_func(
        "env.invoke_iiiii",
        [values.ValueType.I32] * 5,
        [values.ValueType.I32],
        invoke_i_is,
    )
    add_host_func("env.invoke_v", [values.ValueType.I32], [], invoke_v_is)
    add_host_func("env.invoke_vi", [values.ValueType.I32] * 2, [], invoke_v_is)
    add_host_func("env.invoke_vii", [values.ValueType.I32] * 3, [], invoke_v_is)
    add_host_func("env.invoke_viii", [values.ValueType.I32] * 4, [], invoke_v_is)
    add_host_func("env.invoke_viiii", [values.ValueType.I32] * 5, [], invoke_v_is)
    add_host_func("env.mp_js_hook", [], [], mp_js_hook)
    add_host_func("env.mp_js_write", [values.ValueType.I32] * 2, [], mp_js_write)
    add_host_func("env.mp_js_ticks_ms", [], [values.ValueType.I32], mp_js_ticks_ms)
    add_host_func(
        "env.__syscall_chdir",
        [values.ValueType.I32],
        [values.ValueType.I32],
        syscall_chdir,
    )
    add_host_func(
        "env.__syscall_getcwd",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        syscall_getcwd,
    )
    add_host_func(
        "env.__syscall_mkdirat",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_mkdirat,
    )
    add_host_func(
        "env.__syscall_openat",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        syscall_openat,
    )
    add_host_func(
        "env.__syscall_renameat",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        syscall_renameat,
    )
    add_host_func(
        "env.__syscall_poll",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_poll,
    )
    add_host_func(
        "env.__syscall_getdents64",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_getdents64,
    )
    add_host_func(
        "env.__syscall_rmdir",
        [values.ValueType.I32],
        [values.ValueType.I32],
        syscall_rmdir,
    )
    add_host_func(
        "env.__syscall_fstat64",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        syscall_fstat64,
    )
    add_host_func(
        "env.__syscall_stat64",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        syscall_stat64,
    )
    add_host_func(
        "env.__syscall_lstat64",
        [values.ValueType.I32] * 2,
        [values.ValueType.I32],
        syscall_lstat64,
    )
    add_host_func(
        "env.__syscall_newfstatat",
        [values.ValueType.I32] * 4,
        [values.ValueType.I32],
        syscall_newfstatat,
    )
    add_host_func(
        "env.__syscall_statfs64",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_statfs64,
    )
    add_host_func(
        "env.__syscall_fstatfs64",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_fstatfs64,
    )
    add_host_func(
        "env.__syscall_unlinkat",
        [values.ValueType.I32] * 3,
        [values.ValueType.I32],
        syscall_unlinkat,
    )


def run() -> None:
    """Runs the interpreter."""

    global host_funcs_by_name
    global start_time_ms

    add_host_funcs()
    machine_impl = MachineImpl()

    module = binary.Module.from_file("F:/dergwasm/firmware.wasm")
    types_section = cast(binary.TypeSection, module.sections[binary.TypeSection])
    import_section = cast(binary.ImportSection, module.sections[binary.ImportSection])
    func_section = cast(binary.FunctionSection, module.sections[binary.FunctionSection])
    func_indexes = []

    num_host_funcs = len(import_section.imports)
    print(f"There are {num_host_funcs} required host funcs in the module.")

    print("Exports from the module:")
    export_section = cast(binary.ExportSection, module.sections[binary.ExportSection])
    for export in export_section.exports:
        if export.desc_type == binary.FuncType:
            exported_func = func_section.funcs[export.desc_idx - num_host_funcs]
            print(
                f"{export.name}: func " f"{types_section.types[exported_func.typeidx]}"
            )
            continue
        print(f"{export.name}: {export.desc_type} {export.desc_idx}")

    print("Required imports to the module:")
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

    for export_name, val in module_inst.exports.items():
        print(f"{export_name}: {val}")
        if val.val_type == values.RefValType.EXTERN_FUNC:
            if val.addr is None:
                print("  = null ref")
            else:
                print(f"  = {machine_impl.get_func(val.addr).functype}")

    # Let's have a look through all the code to see if there are any instructions
    # in there that we haven't implemented.
    unimplemented = insn_eval.unimplemented_insns(machine_impl)
    needed: set[insn.InstructionType] = set()
    for func in func_section.funcs:
        for instruction in func.body:
            if instruction.instruction_type in unimplemented:
                needed.add(instruction.instruction_type)

    if needed:
        print("Unimplemented instructions:")
        for insn_type in needed:
            print(f"  {insn_type}")
        raise NotImplementedError("Unimplemented instructions")

    start_time_ms = time.time_ns() // 1_000_000

    if "__wasm_call_ctors" in module_inst.exports:
        wasm_call_ctors = module_inst.exports["__wasm_call_ctors"]
        print(f"Calling __wasm_call_ctors() at {wasm_call_ctors.addr}")
        assert wasm_call_ctors.addr is not None
        machine_impl.invoke_func(wasm_call_ctors.addr)
        machine_impl.clear_stack()

    data_addr = exported_malloc(module_inst, machine_impl, 0x1000)
    print(f"Allocated memory at 0x{data_addr:08X}")

    start_name = "main"
    if start_name not in module_inst.exports:
        start_name = "_start"
    if start_name not in module_inst.exports:
        raise ValueError("No start function (_start or main).")

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
