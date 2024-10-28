using System.Text;
using Dergwasm.Wasm;
using Dergwasm.Environments;
using DergwasmTests.testing;
using Xunit;

namespace DergwasmTests
{
    public class EmscriptenWasiTests
    {
        TestMachine machine;
        EmscriptenEnv env;
        EmscriptenWasi wasi;

        public EmscriptenWasiTests()
        {
            machine = new TestMachine();
            env = new EmscriptenEnv(machine);
            wasi = new EmscriptenWasi(machine, env);
        }

        [Fact]
        public void CreatesStream()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(3, stream.fd);
            Assert.Equal("test.txt", stream.path);
            Assert.Equal(0UL, stream.position);
            Assert.Equal("0123456789", Encoding.UTF8.GetString(stream.content));
        }

        [Fact]
        public void CreatesStreamDifferentFd()
        {
            _ = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));
            Stream stream = wasi.CreateStream("test2.txt", Encoding.UTF8.GetBytes("9876543210"));

            Assert.Equal(4, stream.fd);
            Assert.Equal("test2.txt", stream.path);
            Assert.Equal(0UL, stream.position);
            Assert.Equal("9876543210", Encoding.UTF8.GetString(stream.content));
        }

        [Fact]
        public void FdClose()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(-Errno.EBADF, wasi.Close(0));
            Assert.Equal(-Errno.EBADF, wasi.Close(1));
            Assert.Equal(-Errno.EBADF, wasi.Close(2));
            Assert.Equal(-Errno.EBADF, wasi.Close(4));
            Assert.Equal(0, wasi.Close(3));
            Assert.Equal(-Errno.EBADF, wasi.Close(3));
        }

        [Fact]
        public void CreatesStreamFillsFdHole()
        {
            Stream stream3 = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));
            Stream stream4 = wasi.CreateStream("test2.txt", Encoding.UTF8.GetBytes("9876543210"));

            wasi.Close(stream3.fd);

            Stream streamN = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(3, streamN.fd);
        }

        [Fact]
        public void CloseStream()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(0, wasi.Close(stream.fd));
            Assert.Equal(-Errno.EBADF, wasi.Close(stream.fd));
        }

        [Fact]
        public void ReadToByteArray()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            byte[] buffer = new byte[6];
            Assert.Equal(6, wasi.Read(stream.fd, buffer));
            Assert.Equal("012345", Encoding.UTF8.GetString(buffer));
            Assert.Equal(6UL, stream.position);

            Assert.Equal(4, wasi.Read(stream.fd, buffer));
            Assert.Equal("678945", Encoding.UTF8.GetString(buffer));
            Assert.Equal(10UL, stream.position);

            Assert.Equal(0, wasi.Read(stream.fd, buffer));
        }

        [Fact]
        public void ReadToByteArrayEbadf()
        {
            byte[] buffer = new byte[6];
            Assert.Equal(-Errno.EBADF, wasi.Read(0, buffer));
            Assert.Equal(-Errno.EBADF, wasi.Read(1, buffer));
            Assert.Equal(-Errno.EBADF, wasi.Read(2, buffer));
            Assert.Equal(-Errno.EBADF, wasi.Read(3, buffer));
        }

        [Fact]
        public void ReadToHeap()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(0, wasi.Read(stream.fd, 0, 0));

            Assert.Equal(6, wasi.Read(stream.fd, 0, 6));
            Assert.Equal("012345", env.GetUTF8StringFromMem(0, 6));
            Assert.Equal(6UL, stream.position);

            Assert.Equal(4, wasi.Read(stream.fd, 0, 6));
            Assert.Equal("678945", env.GetUTF8StringFromMem(0, 6));
            Assert.Equal(10UL, stream.position);

            Assert.Equal(0, wasi.Read(stream.fd, 0, 6));
        }

        [Fact]
        public void ReadToHeapEbadf()
        {
            Assert.Equal(-Errno.EBADF, wasi.Read(0, 0, 6));
            Assert.Equal(-Errno.EBADF, wasi.Read(1, 0, 6));
            Assert.Equal(-Errno.EBADF, wasi.Read(2, 0, 6));
            Assert.Equal(-Errno.EBADF, wasi.Read(3, 0, 6));
        }

        [Fact]
        public void ReadToHeapEfault()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(-Errno.EFAULT, wasi.Read(stream.fd, machine.Heap.Length, 1));
            Assert.Equal(0, wasi.Read(stream.fd, machine.Heap.Length, 0));
        }

        [Fact]
        public void LSeek()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));
            int errno;

            Assert.Equal(0UL, wasi.LSeek(stream.fd, 0, 0, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(0UL, stream.position);

            Assert.Equal(5UL, wasi.LSeek(stream.fd, 5, 0, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(5UL, stream.position);

            Assert.Equal(5UL, wasi.LSeek(stream.fd, 0, 1, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(5UL, stream.position);

            Assert.Equal(6UL, wasi.LSeek(stream.fd, 1, 1, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(6UL, stream.position);

            Assert.Equal(4UL, wasi.LSeek(stream.fd, -2, 1, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(4UL, stream.position);

            Assert.Equal(10UL, wasi.LSeek(stream.fd, 0, 2, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(10UL, stream.position);

            Assert.Equal(8UL, wasi.LSeek(stream.fd, -2, 2, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(8UL, stream.position);

            Assert.Equal(0UL, wasi.LSeek(stream.fd, -100, 2, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(0UL, stream.position);

            Assert.Equal(10UL, wasi.LSeek(stream.fd, 100, 0, out errno));
            Assert.Equal(0, errno);
            Assert.Equal(10UL, stream.position);
        }

        [Fact]
        public void LSeekEbadf()
        {
            int errno;

            Assert.Equal(0UL, wasi.LSeek(0, 0, 0, out errno));
            Assert.Equal(Errno.EBADF, errno);
            Assert.Equal(0UL, wasi.LSeek(1, 0, 0, out errno));
            Assert.Equal(Errno.EBADF, errno);
            Assert.Equal(0UL, wasi.LSeek(2, 0, 0, out errno));
            Assert.Equal(Errno.EBADF, errno);
            Assert.Equal(0UL, wasi.LSeek(3, 0, 0, out errno));
            Assert.Equal(Errno.EBADF, errno);
        }

        [Fact]
        public void LSeekEinval()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));
            int errno;

            Assert.Equal(0UL, wasi.LSeek(stream.fd, 0, 3, out errno));
            Assert.Equal(Errno.EINVAL, errno);
        }

        [Fact]
        public void WriteFromByteArray()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            byte[] buffer = Encoding.UTF8.GetBytes("a");
            Assert.Equal(1, wasi.Write(stream.fd, buffer));
            Assert.Equal("a123456789", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(1UL, stream.position);

            Assert.Equal(1, wasi.Write(stream.fd, buffer));
            Assert.Equal("aa23456789", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(2UL, stream.position);

            buffer = Encoding.UTF8.GetBytes("bcdefghijk");
            Assert.Equal(10, wasi.Write(stream.fd, buffer));
            Assert.Equal("aabcdefghijk", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(12UL, stream.position);
        }

        [Fact]
        public void WriteFromByteArrayEbadf()
        {
            Assert.Equal(-Errno.EBADF, wasi.Write(0, new byte[1]));
            Assert.Equal(-Errno.EBADF, wasi.Write(2, new byte[1]));
            Assert.Equal(-Errno.EBADF, wasi.Write(3, new byte[1]));
        }

        [Fact]
        public void WriteFromByteArrayEfbig()
        {
            Stream stream = wasi.CreateStream("test.txt", new byte[0]);

            wasi.Write(stream.fd, new byte[10]);

            byte[] buffer = new byte[EmscriptenWasi.MAX_ARRAY_LENGTH - 5];
            Assert.Equal(-Errno.EFBIG, wasi.Write(stream.fd, buffer));
        }

        [Fact]
        public void WriteFromHeap()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));
            env.WriteUTF8StringToMem(new Ptr<byte>(0), "abcdefghijk");

            Assert.Equal(1, wasi.Write(stream.fd, 0, 1));
            Assert.Equal("a123456789", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(1UL, stream.position);

            Assert.Equal(1, wasi.Write(stream.fd, 0, 1));
            Assert.Equal("aa23456789", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(2UL, stream.position);

            Assert.Equal(10, wasi.Write(stream.fd, 1, 10));
            Assert.Equal("aabcdefghijk", Encoding.UTF8.GetString(stream.content));
            Assert.Equal(12UL, stream.position);
        }

        [Fact]
        public void WriteFromHeapEbadf()
        {
            Assert.Equal(-Errno.EBADF, wasi.Write(0, 0, 1));
            Assert.Equal(-Errno.EBADF, wasi.Write(2, 0, 1));
            Assert.Equal(-Errno.EBADF, wasi.Write(3, 0, 1));
        }

        [Fact]
        public void WriteFromHeapEfbig()
        {
            Stream stream = wasi.CreateStream(
                "test.txt",
                new byte[EmscriptenWasi.MAX_ARRAY_LENGTH - 5]
            );
            wasi.LSeek(stream.fd, 0, 2, out int _);

            Assert.Equal(-Errno.EFBIG, wasi.Write(stream.fd, 0, 10));
        }

        [Fact]
        public void WriteFromHeapEfault()
        {
            Stream stream = wasi.CreateStream("test.txt", Encoding.UTF8.GetBytes("0123456789"));

            Assert.Equal(-Errno.EFAULT, wasi.Write(stream.fd, machine.Heap.Length, 1));
            Assert.Equal(0, wasi.Write(stream.fd, machine.Heap.Length, 0));
        }

        [Fact]
        public void WriteToStdoutFromByteArray()
        {
            string output = "";
            env.outputWriter = (string str) => output += str;

            Assert.Equal(6, wasi.Write(EmscriptenWasi.FD_STDOUT, Encoding.UTF8.GetBytes("012345")));
            Assert.Equal("012345", output);

            output = "";
            byte[] buffer = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34 };
            Assert.Equal(5, wasi.Write(EmscriptenWasi.FD_STDOUT, buffer));
            Assert.Equal("01234", output);
        }

        [Fact]
        public void WriteToStdoutFromHeap()
        {
            string output = "";
            env.outputWriter = (string str) => output += str;

            env.WriteUTF8StringToMem(new Ptr<byte>(0), "012345");

            Assert.Equal(6, wasi.Write(EmscriptenWasi.FD_STDOUT, 0, 6));
            Assert.Equal("012345", output);
        }
    }
}
