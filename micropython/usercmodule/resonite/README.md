This directory contains thin Micropython wrappers around the C API. The wrappers
become available as the `resonitenative` module. These are used in the classes
in the `resonite` module (in `micropython/fs/resonite`).

Copy the contents of this directory and the c directory to micropython/user_modules/resonite:

```
cd ports/webassembly
cp <dergwasm-root>/c/* ../../user_modules/resonite
cp <dergwasm-root>/micropython/usercmodule/resonite ../../user_modules/resonite
```

Then compile with:

```
make clean
make V=1 USER_C_MODULES=../../user_modules
```

You will also have to copy <<dergwasm-root>>/micropython/fs to slots under the Dergwasm slot in your world.

See [micropython-usermod](https://micropython-usermod.readthedocs.io/) (slightly out of date) and
[MicroPython external C modules](https://docs.micropython.org/en/latest/develop/cmodules.html).

NOTE: The API uses 64-bit values. For Emscripten, this means you must compile with `-s WASM_BIGINT`.
