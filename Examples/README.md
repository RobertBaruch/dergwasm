# Examples

This directory contains language-specific examples of compiling down to WASM code. There is also a main program in `LoadModule`, written in C#, which can load the code you compiled, print out information about the code such as what external functions it expects, and optionally run it with detailed instruction-level debugging.

## hello_world.c

We can compile `hello_world.c` using Emscripten. Using the `-o` option and specifying a file ending in `.wasm` tells Emscripten not to generate any JavaScript code (the usual runtime environment for WASM), but only WASM code. The following command line shows compilation without any optimization flags.

```sh
emcc hello_world.c -o hello_world_c.wasm
```

This results in `hello_world_c.wasm`, a 14238-byte WASM program (using Emscripten 3.1.71).

Here is the output of running `LoadModule` on `hello_world_c.wasm`, with some explanation:

```txt
Reading WASM file 'Examples/hello_world/hello_world_c.wasm'
Reading 21 types
  Type[0]: (I32, I32, I32) -> (I32)
  Type[1]: () -> (I32)
  Type[2]: () -> ()
  Type[3]: (I32) -> ()
  Type[4]: (I32) -> (I32)
  Type[5]: (I32, I32) -> (I32)
  Type[6]: (I32, I64, I32) -> (I64)
  Type[7]: (I32, F64, I32, I32, I32, I32) -> (I32)
  Type[8]: (I32, I32) -> ()
  Type[9]: (I64, I32) -> (I32)
  Type[10]: (I32, I64, I64, I32) -> ()
  Type[11]: (I32, I32, I32, I32) -> (I32)
  Type[12]: (F64, I32) -> (F64)
  Type[13]: (I32, I32, I32, I32, I32) -> (I32)
  Type[14]: (I32, I32, I32, I32, I32, I32, I32) -> (I32)
  Type[15]: (I32, I32, I32) -> ()
  Type[16]: (I32, I32, I32, I32) -> ()
  Type[17]: (I64, I32, I32) -> (I32)
  Type[18]: (I32, I32, I32, I32, I32) -> ()
  Type[19]: (F64) -> (I64)
  Type[20]: (I64, I64) -> (F64)
```

The types section is a table of WASM function signatures (parameter types and return types). Each entry in the table can be referred to in the specification of a function. Here there are 21 function signatures.

```txt
Reading 2 imports
Module requires imported function[0] wasi_snapshot_preview1.proc_exit (I32) -> ()
Module requires imported function[1] wasi_snapshot_preview1.fd_write (I32, I32, I32, I32) -> (I32)
```

The imports section is a list of functions that are expected to exist in other modules. In this case, the "standard" module `wasi_snapshot_preview1` is requested, with two of its functions `proc_exit` and `fd_write`. This makes sense, since the program has to write to stdout, and also exit.

```txt
Reading 56 functions
Reading 1 tables
Reading 1 memories
Reading 3 globals
```

The module contains various functions, tables, and so on, which implement `hello_world`.

```txt
Reading 10 exports
Exporting table $0 as __indirect_function_table
Exporting function $2 as _start () -> ()
Exporting function $55 as strerror (I32) -> (I32)
Exporting function $48 as emscripten_stack_init () -> ()
Exporting function $49 as emscripten_stack_get_free () -> (I32)
Exporting function $50 as emscripten_stack_get_base () -> (I32)
Exporting function $51 as emscripten_stack_get_end () -> (I32)
Exporting function $52 as _emscripten_stack_restore (I32) -> ()
Exporting function $53 as emscripten_stack_get_current () -> (I32)
```

The module provides one table and eight functions to the external environment. None of these will actually be used, except for `_start`, which is the Emscripten-provided entry point for running the main program.

```txt
Reading 1 element segments
Reading 56 function bodies
Reading 2 data segments
Instruction: I32_CONST Value[hi=0000000000000000, u64=0000000000010000]
Instruction: END
Reading 2788 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=0000000000010AE8]
Instruction: END
Reading 148 bytes of data
```

Data segments are used for initializing memory. When the module is instantiated, any "active" data segment is immediately copied into memory, starting at some address. The address to load a segment to is given by running some limited WASM code and looking at the value left on the stack. In the cases above, the first data segment is loaded to address `0x10000`, and is 2788 bytes of data long, so the segment goes from `0x10000-0x10AE3`. The second segment is loaded to address `0x10AE8` and is 148 bytes long, so the segment goes from `0x10AE8-0x10B7B`.

```txt
Reading custom section
  name: target_features
  len : 0x00000039
```

Custom sections contain data that the compiler outputs, but are not expected to be understood by external tools.

```txt
Running _start
Hello, World!
```

Finally, we call the module's `_start` function. At some point the code calls the environment to print out `Hello, World!` (with a newline) and then exits.

## hello_world.c (optimized)

In contrast, when compiled with full optimization, the WASM code is smaller (2029 bytes), and running the code shows its compactness.

```sh
emcc hello_world.c -O3 -o hello_world_c_opt.wasm
```

```txt
Reading WASM file 'Examples/hello_world/hello_world_c_opt.wasm'
Reading 7 types
  Type[0]: (I32, I32, I32) -> (I32)
  Type[1]: (I32) -> ()
  Type[2]: () -> ()
  Type[3]: (I32, I64, I32) -> (I64)
  Type[4]: () -> (I32)
  Type[5]: (I32) -> (I32)
  Type[6]: (I32, I32, I32, I32) -> (I32)
Reading 2 imports
Module requires imported function[0] wasi_snapshot_preview1.proc_exit (I32) -> ()
Module requires imported function[1] wasi_snapshot_preview1.fd_write (I32, I32, I32, I32) -> (I32)
Reading 10 functions
Reading 1 tables
Reading 1 memories
Reading 1 globals
Reading 5 exports
Exporting table $0 as __indirect_function_table
Exporting function $1 as _start () -> ()
Exporting function $8 as _emscripten_stack_restore (I32) -> ()
Exporting function $9 as emscripten_stack_get_current () -> (I32)
Reading 1 element segments
Reading 10 function bodies
Reading 7 data segments
Instruction: I32_CONST Value[hi=0000000000000000, u64=0000000000000400]
Instruction: END
Reading 13 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=0000000000000410]
Instruction: END
Reading 1 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=000000000000041C]
Instruction: END
Reading 1 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=0000000000000434]
Instruction: END
Reading 14 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=000000000000044C]
Instruction: END
Reading 1 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=000000000000045C]
Instruction: END
Reading 5 bytes of data
Instruction: I32_CONST Value[hi=0000000000000000, u64=00000000000004A0]
Instruction: END
Reading 2 bytes of data
Running _start
Hello, World!
```
