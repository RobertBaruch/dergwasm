Copy the contents of this directory to micropython/user_modules/resonite.

Then compile with:

```
cd ports/webassembly
make clean
make V=1 USER_C_MODULES=../../user_modules
```

See [micropython-usermod](https://micropython-usermod.readthedocs.io/) (slightly out of date) and
[MicroPython external C modules](https://docs.micropython.org/en/latest/develop/cmodules.html).

NOTE: The API uses 64-bit values. For Emscripten, this means you must compile with `-s WASM_BIGINT`.
