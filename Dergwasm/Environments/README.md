# Environments: bunches of host functions

An environment is a set of host functions bundled together in a module that WASM knows about. Environment classes are annotated with `Mod`, giving the WASM name for the module, and each function to be available to WASM is annotated with `ModFn`, giving the WASM name for the function.

## FilesystemEnv

This class provides a filesystem for WASM, with module name `env`. The WASM functions are defined by Emscripten.

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

This class contains host functions required by Emscripten and MicroPython, with module name `env`, and also utility functions for converting between C# types and WASM data.

The following WASM functions are implemented in this class:

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
