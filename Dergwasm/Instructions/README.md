# The Instructions directory

This directory contains files implementing [WASM instructions](https://webassembly.github.io/spec/core/syntax/instructions.html), plus utilities for instructions.

## Instruction implementations

* ControlInstructions.cs: Implements [WASM control instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#control-instructions):
  * `nop`
  * `unreachable`
  * `block`
  * `loop`
  * `if`
  * `else`
  * `br`
  * `br_if`
  * `br_table`
  * `return`
  * `call`
  * `call_indirect`
* MemoryInstructions.cs: Implements [WASM memory instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#memory-instructions):
  * `i`*`nn`*`.load` | `f`*`nn`*`.load`
  * `i`*`nn`*`.store` | `f`*`nn`*`.store`
  * `i`*`nn`*`.load8_`*`sx`* | `i`*`nn`*`.load16_`*`sx`* | `i64.load32_`*`sx`*
  * `i`*`nn`*`.store8` | `i`*`nn`*`.store16` | `i64.store32`
  * `memory.size`
  * `memory.grow`
  * `memory.fill`
  * `memory.copy`
  * `memory.init`
  * `data.drop`
* NumericInstructions.cs: Implements [WASM numeric instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#numeric-instructions):
  * `i`*`nn`*`.const` | `f`*`nn`*`.const`
  * `i`*`nn`*`.`*`iunop`* | `f`*`nn`*`.`*`funop`*
  * `i`*`nn`*`.`*`ibinop`* | `f`*`nn`*`.`*`fbinop`*
  * `i`*`nn`*`.`*`itestop`*
  * `i`*`nn`*`.`*`irelop`* | `f`*`nn`*`.`*`frelop`*
  * `i`*`nn`*`extend8_s` | `i`*`nn`*`.extend16_s` | `i64.extend32_s`
  * `i32.wrap_i64` | `i64.extend_32_`*`sx`*
  * `i`*`nn`*`.trunc_f`*`mm`*`_`*`sx`* | `i`*`nn`*`.trunc_sat_f`*`mm`*`_`*`sx`*
  * `f32.demote_f64` | `f64.promote_f32` | `f`*`nn`*`.convert_i`*`mm`*`_`*`sx`*
  * `i`*`nn`*`.reinterpret_f`*`nn`* | `f`*`nn`*`.reinterpret_i`*`nn`*
* ParametricInstructions.cs: Implements [WASM parametric instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#parametric-instructions):
  * `drop`
  * `select`
* ReferenceInstructions.cs: Implements [WASM reference instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#reference-instructions):
  * `ref.null`
  * `ref.is_null`
  * `ref.func`
* TableInstructions.cs: Implements [WASM table instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#table-instructions):
  * `table.get`
  * `table.set`
  * `table.size`
  * `table.grow`
  * `table.fill`
  * `table.copy`
  * `table.init`
  * `elem.drop`
* VariableInstructions.cs: Implements [WASM variable instructions](https://webassembly.github.io/spec/core/syntax/instructions.html#variable-instructions):
  * `local.get`
  * `local.set`
  * `local.tee`
  * `global.get`
  * `global.set`

## Utilities

* LEB128.cs: Single-file utility to read and write integers in the LEB128 (7-bit little endian base-128) format. From [rzubek's mini-leb128 Git repository](https://github.com/rzubek/mini-leb128/blob/master/LEB128.cs).
* Instructions.cs:
  * enum `InstructionType`, conveniently mapping enumerated instruction constants to their binary integer representation.
  * struct `Instruction`, representing an instruction with an `InstructionType` and its operands (`Value`s).
  * Decoders for instructions. Some instructions have blocks of instructions as operands (e.g. `loop`), so instruction operands are first represented as "unflattened". After all instructions are read, the instruction list is flattened into linear form.
* InstructionEvaluation.cs: How to execute instructions.
