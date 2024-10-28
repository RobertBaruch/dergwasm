# API generation

`generate_api.py` is a Python program which extracts the host functions defined by ResoniteEnv, stores the API information as JSON, and then passes the JSON data to language-specific generators. These generators create language-specific libraries and/or header files.

The JSON file is an array of JSON objects:

* Module: string, the WASM module name
* Name: string, the WASM function name
* Parameters: an array of objects representing the input parameters to the function:
  * Name: string, the name of the parameter
  * Types: array of `ValueType` ints from executing `Value.ValueType<>` via `ModuleReflector.ValueTypesFor` on the parameter type.
  * CSType: string, C# type of the parameter
* Returns: an array of objects representing the output return values from the function:
  * Name: null
  * Types: array of `ValueType` ints from executing `Value.ValueType<>` via `ModuleReflector.ValueTypesFor` on the parameter type.
  * CSType: string, C# type of the return value
