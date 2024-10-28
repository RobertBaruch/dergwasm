# Environments: bunches of host functions

An environment is a set of host functions bundled together in a module that WASM knows about. Environment classes are annotated with `Mod`, giving the WASM name for the module, and each function to be available to WASM is annotated with `ModFn`, giving the WASM name for the function.

For how host functions marshal and unmarshal their parameters and return values between C# and WASM, see [Modules](../Modules/README.md).

## FilesystemEnv

Module name: `env`

This class provides a filesystem for WASM. The WASM functions are defined by Emscripten.

Dergwasm implements a filesystem starting at a given Resonite slot which represents the root of the filesystem ("/"). Children of this slot are either files or directories. Both are slots, but files additionally have a `ValueField<string>` component attached to them, which contains the contents of the file.

Except for the root slot, the name of the file or directory is the name of its slot.

The code acts so that the parent of the root slot is the root slot itself.

The following WASM functions are implemented by the `FilesystemEnv`:

* `__syscall_chdir`
* `__syscall_rmdir`
* `__syscall_getcwd`
* `__syscall_mkdirat`
* `__syscall_openat`
* `__syscall_newfstatat`
* `__syscall_stat64`

The following WASM functions are defined, but unimplemented:

* `__syscall_renameat`
* `__syscall_unlinkat`
* `__syscall_poll`
* `__syscall_getdents64`
* `__syscall_fstat64`
* `__syscall_lstat64`
* `__syscall_statfs64`

Much of the implementation of these system calls is ported from the C code at [Emscripten's](https://github.com/emscripten-core/emscripten) `system/lib/wasmfs/syscalls.cpp`. The syscall functions above were chosen because they were required by MicroPython, and the ones that were implemented were the ones that were actually called by MicroPython.

The header file for all syscalls is in `system/lib/libc/musl/arch/emscripten/syscall_arch.h`.

## EmscriptenEnv

Module name: `env`

This class contains host functions required by Emscripten and MicroPython, and also utility functions for converting between C# types and WASM data.

The following WASM functions are implemented in this environment:

* `_emscripten_throw_longjmp`
* `emscripten_memcpy_js`
* `emscripten_scan_registers`
* `emscripten_resize_heap`
* `exit`

Indirect function calls resulting from Emscripten's setjmp/longjmp implementation:

* `invoke_v`
* `invoke_vi`
* `invoke_vii`
* `invoke_viii`
* `invoke_viiii`
* `invoke_i`
* `invoke_ii`
* `invoke_iii`
* `invoke_iiii`
* `invoke_iiiii`

MicroPython-specific calls:

* `mp_js_hook`
* `mp_js_ticks_ms`
* `mp_js_write`

### GetUTF8StringFromMem: How syscalls pass strings

Many of the filesystem calls require path names. For example, the header file defines `__syscall_chdir` as:

```c
int __syscall_chdir(intptr_t path);
```

The pointer is a pointer into the WASM heap (i.e. memory 0), and is simply a UTF-8 encoded NUL-terminated string. Thus, Dergwasm's implementation gets the data from the heap and converts it into a C# string via `GetUTF8StringFromMem`.

## EmscriptenWasi

Module name: `wasi_snapshot_preview1`

This environment implements functions in [WASI snapshot preview 1](https://github.com/WebAssembly/WASI/blob/main/legacy/preview1/witx/wasi_snapshot_preview1.witx), as used by Emscripten.

The following WASM functions are implemented in this environment:

* `proc_exit`
* `environ_get`
* `environ_sizes_get`
* `fd_write`
* `fd_seek`
* `fd_read`
* `fd_close`
* `fd_sync`

## ResoniteEnv

Module name: `resonite`

This environment implements functions for accessing Resonite data structures. The function names are of the form `<class>__<function>`. Languages that compile to WASM need to have some library that knows about these functions, and these are in the API directory.

The following WASM functions are implemented for Slots:

* `slot__root_slot`
* `slot__get_parent`
* `slot__get_active_user`
* `slot__get_active_user_root`
* `slot__get_object_root`
* `slot__get_name`
* `slot__set_name`
* `slot__get_num_children`
* `slot__get_child`
* `slot__get_children`
* `slot__find_child_by_name`
* `slot__find_child_by_tag`
* `slot__get_component`
* `slot__get_components`

The following WASM functions are implemented for Components:

* `component__get_type_name`
* `component__get_member`

The following WASM functions are implemented for Values:

* `value__get_int`
* `value__get_float`
* `value__get_double`
* `value__set_int`
* `value__set_float`
* `value__set_double`
