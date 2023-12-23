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
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int, int, int, int>(FdSeek)
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
        void ProcExit(Frame frame, int exit_code)
        {
            //Console.WriteLine(
            //    $"ProcExit called with exit_code={exit_code}. {100000 - ((Machine)machine).stepBudget} instructions executed."
            //);
            //Environment.Exit(exit_code);
            throw new ExitTrap(exit_code);
        }

        int EnvironGet(Frame frame, int environPtrPtr, int environBufPtr)
        {
            //throw new Trap(
            //    $"Unimplemented call to EnvironGet(0x{environPtrPtr:X8}, 0x{environBufPtr:X8})"
            //);
            return 0;
        }

        int EnvironSizesGet(Frame frame, int argcPtr, int argvBufSizePtr)
        {
            //throw new Trap(
            //    $"Unimplemented call to EnvironSizesGet(0x{argcPtr:X8}, 0x{argvBufSizePtr:X8})"
            //);
            return 0;
        }

        void EmscriptenMemcpyJs(Frame frame, int dest, int src, int len)
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
        int FdWrite(Frame frame, int fd, int iov, int iovcnt, int pnum)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("=================================================");
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
            Console.WriteLine("=================================================");
            Console.WriteLine("=================================================");
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
        int FdSeek(Frame frame, int fd, int offset_lo, int offset_hi, int whence, int newOffsetPtr)
        {
            throw new Trap(
                $"Unimplemented call to FdSeek({fd}, {offset_lo}, {offset_hi}, {whence}, 0x{newOffsetPtr:X8})"
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
        int FdRead(Frame frame, int fd, int iovs, int iovs_len, int nreadPtr)
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
        int FdClose(Frame frame, int fd)
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
        int FdSync(Frame frame, int fd)
        {
            throw new Trap($"Unimplemented call to FdSync({fd})");
        }
    }
}
