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
        public EmscriptenEnv emscriptenEnv;

        public EmscriptenWasi(Machine machine, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.emscriptenEnv = emscriptenEnv;
        }

        public void RegisterHostFuncs()
        {
            machine.RegisterReturningHostFunc<int, int, int>(
                "wasi_snapshot_preview1",
                "environ_get",
                EnvironGet
            );
            machine.RegisterReturningHostFunc<int, int, int>(
                "wasi_snapshot_preview1",
                "environ_sizes_get",
                EnvironSizesGet
            );

            machine.RegisterVoidHostFunc<int>("wasi_snapshot_preview1", "proc_exit", ProcExit);
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "wasi_snapshot_preview1",
                "fd_write",
                FdWrite
            );
            machine.RegisterReturningHostFunc<int, long, int, int, int>(
                "wasi_snapshot_preview1",
                "fd_seek",
                FdSeek
            );
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "wasi_snapshot_preview1",
                "fd_read",
                FdRead
            );
            machine.RegisterReturningHostFunc<int, int>(
                "wasi_snapshot_preview1",
                "fd_close",
                FdClose
            );
            machine.RegisterReturningHostFunc<int, int>(
                "wasi_snapshot_preview1",
                "fd_sync",
                FdSync
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
        // If fd == 1, writes to stdout. In this case, though, the buffer must be
        // decodable as a UTF-8 string. If the buffer couldn't be decoded, then
        // -EINVAL is returned.
        //
        // If fd != 1, -EBADF is returned.
        //
        // Args:
        //    fd: The file descriptor to write to.
        //    iov: A pointer to an array of __wasi_ciovec_t structures, each describing
        //      a buffer to write data from.
        //    iovcnt: The number of vectors (__wasi_ciovec_t) in the iovs array.
        //    pnum: A pointer to store the number of bytes written. May be 0 to not write the count.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdWrite(Frame frame, int fd, int iov, int iovcnt, int pnum)
        {
            if (iov == 0)
            {
                return -Errno.EFAULT;
            }
            if (fd != 1)
            {
                return -Errno.EBADF;
            }

            Memory mem = machine.GetMemoryFromIndex(0);

            MemoryStream iovStream = new MemoryStream(mem.Data);
            iovStream.Position = iov;
            BinaryReader iovReader = new BinaryReader(iovStream);

            uint count = 0;

            for (int i = 0; i < iovcnt; i++)
            {
                int ptr = iovReader.ReadInt32();
                uint len = iovReader.ReadUInt32();

                try
                {
                    string str = emscriptenEnv.GetUTF8StringFromMem(ptr, len);

                    if (emscriptenEnv.outputWriter != null)
                    {
                        emscriptenEnv.outputWriter(str);
                    }
                    else
                    {
                        Console.Write(str);
                    }
                }
                catch (Exception)
                {
                    return -Errno.EINVAL;
                }

                count += len;
            }

            if (pnum != 0)
            {
                iovStream.Position = pnum;
                BinaryWriter countWriter = new BinaryWriter(iovStream);
                countWriter.Write(count);
            }
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
        int FdSeek(Frame frame, int fd, long offset, int whence, int newOffsetPtr)
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
