# Interfaces to modules

Since the only values WASM understands are the basic numeric values, and it's inconvenient to translate other values to basic numeric values by hand, Dergwasm provides a `ModuleReflector` which goes through the functions in a host module and creates proxies which marshal and unmarshal parameters and return values for the host function.

## ReflectHostFunc

This function takes a module name and function name for WASM to use, and a Delegate for the host function, and "connects" them.

The connection is done by synthesizing code.

Host functions may take a Machine parameter and a Frame parameter, and these will just contain the machine and the current frame under which the function is executing. All other parameters result in synthetic code for popping the value off the caller's stack and marshalling it into the appropriate type. Then the synthetic code calls the actual host function. Return values, if any, result in synthetic code to unmarshal the value and pushing it onto the caller's stack.

## ReflectHostFuncs

This function takes a class instance, and converts every public, nonpublic, static, or instance method in it that are annotated with `ModFn` via `ReflectHostFunc`. The `ModFn` annotation tells us what the WASM function name is.

The class must also have a `Mod` annotation which tells us what the name of the WASM module is.

## Environments: bunches of host functions

For environments, see [Environments](../Environments/README.md).
