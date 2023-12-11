using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public class EmscriptenWasi
    {
        Machine machine;

        public EmscriptenWasi(Machine machine)
        {
            this.machine = machine;
        }

        public void RegisterHostFuncs()
        {
            machine.RegisterHostFunc(
                "env",
                "emscripten_memcpy_js",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { }
                ),
                new HostProxy<int, int, int>(EmscriptenMemcpyJs)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "environ_get",
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int>(EnvironGet)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "environ_sizes_get",
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int>(EnvironSizesGet)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "proc_exit",
                new FuncType(new Derg.ValueType[] { Derg.ValueType.I32 }, new Derg.ValueType[] { }),
                new HostProxy<int>(ProcExit)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_write",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int, int, int>(FdWrite)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_seek",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I64,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, long, int, int, int>(FdSeek)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_read",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int, int, int>(FdRead)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_close",
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int>(FdClose)
            );

            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_sync",
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int>(FdSync)
            );
        }

        // Terminates the process.
        //
        // Args:
        //    exit_code: The exit code of the process. An exit code of 0 indicates successful
        //      termination of the program. Any other values are dependent on the environment.
        //
        // Does not return.
        void ProcExit(int exit_code)
        {
            Console.WriteLine(
                $"ProcExit called with exit_code={exit_code}. {100000 - ((Machine)machine).stepBudget} instructions executed."
            );
            //Environment.Exit(exit_code);
            throw new ExitTrap(exit_code);
        }

        int EnvironGet(int environPtrPtr, int environBufPtr)
        {
            //throw new Trap(
            //    $"Unimplemented call to EnvironGet(0x{environPtrPtr:X8}, 0x{environBufPtr:X8})"
            //);
            return 0;
        }

        int EnvironSizesGet(int argcPtr, int argvBufSizePtr)
        {
            //throw new Trap(
            //    $"Unimplemented call to EnvironSizesGet(0x{argcPtr:X8}, 0x{argvBufSizePtr:X8})"
            //);
            return 0;
        }

        void EmscriptenMemcpyJs(int dest, int src, int len)
        {
            Console.WriteLine($"EmscriptenMemcpyJs({dest}, {src}, {len})");
            Memory mem = machine.GetMemoryFromIndex(0);
            try
            {
                Array.Copy(mem.Data, src, mem.Data, dest, len);
            }
            catch (Exception)
            {
                throw new Trap(
                    "EmscriptenMemcpyJs: Access out of bounds: source offset "
                        + $"0x{src:X8}, destination offset 0x{dest:X8}, length 0x{len:X8} bytes"
                );
            }
        }

        // Writes data to a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to write to.
        //    iovs: A pointer to an array of __wasi_ciovec_t structures, each describing
        //      a buffer to write data from.
        //    iovs_len: The number of vectors (__wasi_ciovec_t) in the iovs array.
        //    nwritten_ptr: A pointer to store the number of bytes written.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdWrite(int fd, int iov, int iovcnt, int pnum)
        {
            Console.WriteLine($"FdWrite({fd}, 0x{iov:X8}, {iovcnt}, 0x{pnum:X8})");
            Memory mem = machine.GetMemoryFromIndex(0);

            MemoryStream iovStream = new MemoryStream(mem.Data);
            iovStream.Position = iov;
            BinaryReader iovReader = new BinaryReader(iovStream);

            int count = 0;

            for (int i = 0; i < iovcnt; i++)
            {
                int ptr = iovReader.ReadInt32();
                int len = iovReader.ReadInt32();
                Console.WriteLine($"  iov[{i}]: ptr=0x{ptr:X8}, len={len}");
                byte[] data = new byte[len];
                Array.Copy(mem.Data, ptr, data, 0, len);
                Console.WriteLine($"  data: {System.Text.Encoding.UTF8.GetString(data)}");
                count += len;
            }

            iovStream.Position = pnum;
            BinaryWriter countWriter = new BinaryWriter(iovStream);
            countWriter.Write(count);
            return 0;
        }

        // Seeks to a position in a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to seek.
        //    offset: The 64-bit offset to seek to.
        //    whence: The origin of the seek.This is one of:
        //        0: SEEK_SET(seek from the beginning of the file)
        //        1: SEEK_CUR(seek from the current position in the file)
        //        2: SEEK_END(seek from the end of the file)
        //    newoffset_ptr: The address of an i64 to store the new offset.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdSeek(int fd, long offset, int whence, int newOffsetPtr)
        {
            throw new Trap(
                $"Unimplemented call to FdSeek({fd}, {offset}, {whence}, 0x{newOffsetPtr:X8})"
            );
        }

        // Reads data from a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to read from.
        //    iovs: A pointer to an array of __wasi_iovec_t structures describing
        //      the buffers where the data will be stored.
        //    iovs_len: The number of vectors (__wasi_iovec_t) in the iovs array.
        //    nread_ptr: A pointer to store the number of bytes read.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdRead(int fd, int iovs, int iovs_len, int nreadPtr)
        {
            throw new Trap(
                $"Unimplemented call to FdRead({fd}, 0x{iovs:X8}, {iovs_len}, 0x{nreadPtr:X8})"
            );
        }

        // Closes a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to close.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdClose(int fd)
        {
            throw new Trap($"Unimplemented call to FdClose({fd})");
        }

        // Syncs the file to disk.
        //
        // Args:
        //    fd: The file descriptor to sync.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdSync(int fd)
        {
            throw new Trap($"Unimplemented call to FdSync({fd})");
        }

        // The various invoke_* functions are used to handle exceptions. The format of
        // such a function is invoke_{r}{p}* where {r} is the return type (v = void, i = i32),
        // and {p} is the parameter type. For example, invoke_iiii is a function that takes
        // four i32 parameters and returns an i32.
        //
        // The implementation in JavaScript looks like this:
        //
        // function invoke_iiii(index,a1,a2,a3) {
        //   var sp = stackSave();
        //   try {
        //     return dynCall_iiii(index, a1, a2, a3);
        //   } catch(e) {
        //     stackRestore(sp);
        //     if (e !== e+0) throw e;
        //     _setThrew(1, 0);
        //   }
        // }
        //
        // The dynCall_iiii function is an exported function in the WASM code, while the
        // invoke_iiii function is an imported function in the WASM code.
        //
        // _setThrew is just a call to the WASM exported function setThrew.
        //
        // From what I can tell, exceptions are implemented by having the WASM code call an
        // invoke_* function for the try portion of a try/catch block, where the index parameter
        // is an index for the particular try/catch block. invoke_* first saves the current
        // stack pointer, then calls a dynCall_* function, which actually is the try portion.
        // If an exception gets thrown, the stack is restored, and a call is made to setThrew.
        // This seems to set a specific memory location. Presumably this then allows continuation
        // of the function that called the invoke_* function in the first place.
        //
        // Because we maintain a separate stack for each function, saving and restoring the stack
        // pointer sounds like a no-op. However, the "stack" referred to is actually emscripten's
        // stack structure, which is a memory location pointer stored in $global0. stackSave and
        // stackRestore are functions in the WASM code, so they do need to be called.
    }
}
