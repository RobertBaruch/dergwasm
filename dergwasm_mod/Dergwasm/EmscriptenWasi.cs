using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Derg.Wasm;
using FrooxEngine;
using Derg.Mem;
using Derg.Modules;
using Derg.Wasm;

namespace Derg
{
    public class Stream
    {
        public int fd;
        public string path;
        public byte[] content;

        // Byte arrays are limited to 0X7FFFFFC7 in size.
        // See https://learn.microsoft.com/en-us/dotnet/api/system.array
        public ulong position;

        // Called when this stream is synced.
        public Func<Stream, int> sync;
    }

    [Mod("wasi_snapshot_preview1")]
    public class EmscriptenWasi
    {
        public const ulong MAX_ARRAY_LENGTH = 0x7FFFFFC7;
        public const int FD_STDIN = 0;
        public const int FD_STDOUT = 1;
        public const int FD_STDERR = 2;

        Machine machine;
        public EmscriptenEnv emscriptenEnv;
        public Dictionary<int, Stream> streams = new Dictionary<int, Stream>();
        SortedSet<int> availableFds = new SortedSet<int>();

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
            machine.RegisterReturningHostFunc<int, long, uint, int, int>(
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

        int getAvailableFd()
        {
            if (availableFds.Count > 0)
            {
                int fd = availableFds.Min;
                availableFds.Remove(fd);
                return fd;
            }
            // File descriptors 0, 1, and 2 are reserved for stdin, stdout, and stderr.
            return streams.Count + 3;
        }

        // Creates a stream for the given slot, which must be a file. The `path` is
        // the normalized path to the slot.
        //
        // We do not support binary files yet.
        public Stream CreateStream(ISlot slot, string path, Func<Stream, int> syncer = null)
        {
            return CreateStream(
                path,
                Encoding.UTF8.GetBytes(slot.GetComponent<ValueField<string>>().Value),
                syncer
            );
        }

        // Creates a stream for the given path and content. The `path` is required to be
        // normalized.
        public Stream CreateStream(string path, byte[] content, Func<Stream, int> syncer = null)
        {
            int fd = getAvailableFd();
            Stream stream = new Stream()
            {
                fd = fd,
                path = path,
                content = content,
                position = 0,
                sync = syncer,
            };
            streams.Add(stream.fd, stream);
            return stream;
        }

        // Closes a file descriptor. Closing does not guarantee that the data has been
        // successfully saved. Use Sync() to ensure that the data has been saved.
        public int Close(int fd)
        {
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;
            streams.Remove(fd);
            // There's an opportunity for more cleverness, since there's no need to store fd
            // numbers higher than the highest fd number in use. But it's not worth the
            // code complexity at this point.
            availableFds.Add(fd);
            return 0;
        }

        // Reads data from a file descriptor, returning the amount of data read, or -errno.
        // No data is ever read from beyond the end of the file.
        public int Read(int fd, byte[] data)
        {
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;

            ulong newpos = streams[fd].position + (ulong)data.Length;
            if (newpos > (ulong)streams[fd].content.Length)
            {
                newpos = (ulong)streams[fd].content.Length;
            }
            int len = (int)(newpos - streams[fd].position);

            Buffer.BlockCopy(streams[fd].content, (int)streams[fd].position, data, 0, len);
            streams[fd].position = newpos;
            return len;
        }

        // Reads data from a file descriptor into the machine's heap, returning the amount
        // of data read, or -errno. No data is ever read from beyond the end of the file.
        //
        // If the data read would overflow the memory, then -EFAULT is returned, and the
        // file position is not updated.
        public int Read(int fd, int memptr, int len)
        {
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;

            ulong newpos = streams[fd].position + (ulong)len;
            if (newpos > (ulong)streams[fd].content.Length)
            {
                newpos = (ulong)streams[fd].content.Length;
            }
            int nread = (int)(newpos - streams[fd].position);

            try
            {
                Buffer.BlockCopy(
                    streams[fd].content,
                    (int)streams[fd].position,
                    machine.Heap,
                    memptr,
                    nread
                );
            }
            catch (Exception)
            {
                return -Errno.EFAULT;
            }
            streams[fd].position = newpos;
            return nread;
        }

        // Writes the given data to stdout.
        public int WriteStdout(byte[] data)
        {
            try
            {
                string str = Encoding.UTF8.GetString(data);

                if (emscriptenEnv.outputWriter != null)
                {
                    emscriptenEnv.outputWriter(str);
                }
                else
                {
                    Console.Write(str);
                }
                return data.Length;
            }
            catch (Exception)
            {
                return -Errno.EINVAL;
            }
        }

        // Writes the given data from the machine's heap to stdout.
        public int WriteStdout(int memptr, int len)
        {
            try
            {
                string str = emscriptenEnv.GetUTF8StringFromMem(memptr, (uint)len);

                if (emscriptenEnv.outputWriter != null)
                {
                    emscriptenEnv.outputWriter(str);
                }
                else
                {
                    Console.Write(str);
                }
                return len;
            }
            catch (Exception)
            {
                return -Errno.EINVAL;
            }
        }

        // Writes the given data to a file descriptor. If the file size after writing would
        // exceed the maximum array length, then -EFBIG is returned.
        public int Write(int fd, byte[] data)
        {
            if (fd == FD_STDOUT)
                return WriteStdout(data);
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;

            ulong newpos = streams[fd].position + (ulong)data.Length;
            if (newpos > MAX_ARRAY_LENGTH)
                return -Errno.EFBIG;

            if (newpos > (ulong)streams[fd].content.Length)
            {
                Array.Resize(ref streams[fd].content, (int)newpos);
            }
            Buffer.BlockCopy(data, 0, streams[fd].content, (int)streams[fd].position, data.Length);
            streams[fd].position = newpos;
            return data.Length;
        }

        // Writes the given data from the machine's heap to a file descriptor. If the file size
        // after writing would exceed the maximum array length, then -EFBIG is returned.
        public int Write(int fd, int memptr, int len)
        {
            if (fd == FD_STDOUT)
                return WriteStdout(memptr, len);
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;

            ulong newpos = streams[fd].position + (ulong)len;
            if (newpos > MAX_ARRAY_LENGTH)
                return -Errno.EFBIG;

            if (newpos > (ulong)streams[fd].content.Length)
            {
                Array.Resize(ref streams[fd].content, (int)newpos);
            }

            try
            {
                Buffer.BlockCopy(
                    machine.Heap,
                    memptr,
                    streams[fd].content,
                    (int)streams[fd].position,
                    len
                );
            }
            catch (Exception)
            {
                // Undo the resize.
                Array.Resize(ref streams[fd].content, (int)streams[fd].position);
                return -Errno.EFAULT;
            }

            streams[fd].position = newpos;
            return len;
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
        //    errno: An out ref to an int to store the error, if any.
        //
        // Returns:
        //    The new file offset.
        public ulong LSeek(int fd, long offset, uint whence, out int errno)
        {
            if (!streams.ContainsKey(fd))
            {
                errno = Errno.EBADF;
                return 0;
            }

            long newpos = 0;
            switch (whence)
            {
                case 0: // SEEK_SET
                    newpos = offset;
                    break;

                case 1: // SEEK_CUR
                    newpos = (long)streams[fd].position + offset;
                    break;

                case 2: // SEEK_END
                    newpos = streams[fd].content.Length + offset;
                    break;

                default:
                    errno = Errno.EINVAL;
                    return 0;
            }

            if (newpos < 0)
            {
                newpos = 0;
            }
            else if (newpos > streams[fd].content.Length)
            {
                newpos = streams[fd].content.Length;
            }
            streams[fd].position = (ulong)newpos;

            errno = 0;
            return streams[fd].position;
        }

        public int Sync(int fd)
        {
            if (fd == FD_STDIN)
                return -Errno.EBADF;
            if (fd == FD_STDOUT || fd == FD_STDERR)
                return 0;
            if (!streams.ContainsKey(fd))
                return -Errno.EBADF;
            if (streams[fd].sync == null)
                return 0;
            return streams[fd].sync(streams[fd]);
        }

        // Terminates the process, closing all open file descriptors.
        //
        // Args:
        //    exit_code: The exit code of the process. An exit code of 0 indicates successful
        //      termination of the program. Any other values are dependent on the environment.
        //
        // Raises:
        //   ExitTrap: Always raises this exception.
        void ProcExit(Frame frame, int exit_code)
        {
            foreach (int fd in streams.Keys)
                Close(fd);
            throw new ExitTrap(exit_code);
        }

        [ModFn("environ_get")]
        int EnvironGet(Frame frame, int environPtrPtr, int environBufPtr)
        {
            //throw new Trap(
            //    $"Unimplemented call to EnvironGet(0x{environPtrPtr:X8}, 0x{environBufPtr:X8})"
            //);
            return 0;
        }

        [ModFn("environ_sizes_get")]
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
        // Args:
        //    fd: The file descriptor to write to.
        //    iov: A pointer to an array of __wasi_ciovec_t structures, each describing
        //      a buffer to write data from.
        //    iovcnt: The number of vectors (__wasi_ciovec_t) in the iovs array.
        //    nwrittenPtr: A pointer to store the number of bytes written. May be 0
        //      to not write the count.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        [ModFn("fd_write")]
        int FdWrite(Frame frame, int fd, int iov, int iovcnt, int nwrittenPtr)
        {
            if (iov == 0)
                return -Errno.EFAULT;
            if (fd != 1)
            {
                return -Errno.EBADF;
            }

            Memory mem = machine.GetMemoryFromIndex(0);

            MemoryStream iovStream = new MemoryStream(mem.Data);
            iovStream.Position = iov;
            BinaryReader iovReader = new BinaryReader(iovStream);

            uint nwritten = 0;

            {
                for (int i = 0; i < iovcnt; i++)
                {
                    int ptr = iovReader.ReadInt32();
                    uint len = iovReader.ReadUInt32();
                    int ret = Write(fd, ptr, (int)len);
                    if (ret < 0)
                    {
                        return ret;
                    }
                    nwritten += (uint)ret;
                }
            }

            if (nwrittenPtr != 0)
            {
                machine.HeapSet(new Ptr<uint>(nwrittenPtr), nwritten);
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
        [ModFn("fd_seek")]
        int FdSeek(Frame frame, int fd, long offset, uint whence, Pointer<uint> newOffsetPtr)
        {
            int errno;
            ulong pos = LSeek(fd, offset, whence, out errno);
            if (errno != 0)
            {
                return -errno;
            }
            if (newOffsetPtr != 0)
            {
                machine.HeapSet(new Ptr<long>(newOffsetPtr), (long)pos);
            }
            return 0;
        }

        // Reads data from a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to read from.
        //    iov: A pointer to an array of __wasi_iovec_t structures describing
        //      the buffers where the data will be stored.
        //    iovcnt: The number of vectors (__wasi_iovec_t) in the iovs array.
        //    nreadPtr: A pointer to store the number of bytes read.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        [ModFn("fd_read")]
        int FdRead(Frame frame, int fd, int iov, int iovcnt, int nreadPtr)
        {
            if (iov == 0)
                return -Errno.EFAULT;

            uint nread = 0;

            using (MemoryStream iovStream = new MemoryStream(machine.Heap))
            {
                iovStream.Position = iov;
                BinaryReader iovReader = new BinaryReader(iovStream);

                for (int i = 0; i < iovcnt; i++)
                {
                    int ptr = iovReader.ReadInt32();
                    uint len = iovReader.ReadUInt32();
                    int ret = Read(fd, ptr, (int)len);
                    if (ret < 0)
                    {
                        return ret;
                    }
                    nread += (uint)ret;
                }
            }

            if (nreadPtr != 0)
            {
                machine.HeapSet(new Ptr<uint>(nreadPtr), nread);
            }
            return 0;
        }

        // Closes a file descriptor.
        //
        // Args:
        //    fd: The file descriptor to close.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        [ModFn("fd_close")]
        int FdClose(Frame frame, int fd) => fd_close(fd);

        // Syncs the file to "disk".
        //
        // Args:
        //    fd: The file descriptor to sync.
        //
        // Returns:
        //    0 on success, or -ERRNO on failure.
        int FdSync(Frame frame, int fd) => Sync(fd);
        [ModFn("fd_sync")]
        int FdSync(Frame frame, int fd) => 0;
    }
}
