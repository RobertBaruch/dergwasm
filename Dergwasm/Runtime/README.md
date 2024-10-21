# Runtime code for WASM execution

## Machine

The Machine is the virtual machine that a single thread of WASM code runs under. It holds the ["store"](https://webassembly.github.io/spec/core/exec/runtime.html#store), which consists of:

* `memories`: [Mutable memory instances](https://webassembly.github.io/spec/core/exec/runtime.html#memory-instances), representing linear memory. In the spec, only one memory instance is supported by WASM, but it is open to multiple memory instance. Memory instances are also always multiples of 64kB. Memory must be accessed through the memory instructions. Memories cannot be created by WASM code, but existing memories can be increased in size via the `memory.grow` instruction. Memories can never be decreased in size.
* `globals`: [Mutable global values](https://webassembly.github.io/spec/core/exec/runtime.html#global-instances). These are not stored in WASM memory, and must be accessed through the variable instructions.
* `funcs`: Functions, or WASM code. These are not stored in WASM memory.
* `tables`: [Typed mutable vectors of references](https://webassembly.github.io/spec/core/exec/runtime.html#data-instances). These are not stored in WASM memory, and must be accessed through the table instructions.
* `dataSegments`: [Immutable arrays of bytes](https://webassembly.github.io/spec/core/syntax/modules.html#data-segments). These are not stored in WASM memory. They may only be copied into memory, either "passively" during a `memory.init` instruction, or "actively" during instantiation. Data segments cannot be created by WASM code, but may be "dropped" permanently by WASM code via the `data.drop` instruction.
* `elementSegments`: [Typed immutable vectors of references](https://webassembly.github.io/spec/core/syntax/modules.html#element-segments). These are not stored in WASM memory. They may only be copied into tables, either "passively" during a `table.init` instruction, or "actively" during instantiation. Element segments cannot be created by WASM code, but may be "dropped" permanently by WASM code via the `elem.drop` instruction.

Each of these store items is identified by a numeric "address" which starts from 0 and monotonically increases, with no holes. The address space of each of these items is separate. Thus, we have memory 0, memory 1, memory 2, and so on, and global 0, global 1, global 2, and so on.

We call memory 0 the "heap", and we can copy data between C# and the heap.

## Module

A [module](https://webassembly.github.io/spec/core/syntax/modules.html#modules) is a single WASM file, with a module name, specifying functions, tables, memories, globals, element segments and data segments. When a module is instantiated, these go into the machine.

A module may specify "exports", which are functions, tables, memories, and globals which the module makes available to other modules. Exports have names.

Modules may specify "imports" which are functions, tables, memories, and globals which the module needs. These are specified by module and name, which means that during compilation, modules know what other modules will be included in the final instantiation of the machine.

Finally, a module may specify a [start function](https://webassembly.github.io/spec/core/syntax/modules.html#start-function), which is the function in the module that the machine must run during the module's instantiation (after its tables and memories have been initialized). This effectively initializes the module.

## Functions and frames

A function is a linear list of instructions. Although a WASM machine is said to have a stack, functions do not have access to the stacks of callers or callees. Functions are guaranteed not to underflow their stacks due to the validation step that is supposed to be done during module instantiation. However, Dergwasm does not implement validation, instead assuming that WASM code is valid. Executing invalid WASM code leads to undefined behavior at worst, or a Trap exception at best.

A "host" function differs from a function in that it is a function provided by the machine. They can be considered "system" functions or "nonnative" functions. As an example, printing to the console has to be implemented as a host function, since WASM does not have a console. Host functions needed by a module are specified in its imports, although the module name for such an import isn't a WASM module, but rather a kind of namespace that the machine knows about.

A frame consists of a reference to the function that is executing, any function local values, a program counter (an index to the instruction within the function's list of instructions), a stack of values, and a reference to the caller's frame (returned to after the function is done).

## The stack

As mentioned above, each function has its own stack of values. Dergwasm implements the stack as a stack of 128-bit values, which is enough to store any numeric value (any of I32, I64, F32, F64, V128) in WASM, and any non-numeric value (a reference). Since WASM modules are supposed to be validated, and validation includes checking that all operations on the  stack are performed on the correct types, it means that we don't have to store type information. As mentioned above, Dergwasm assumes modules are valid.
