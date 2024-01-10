using System;

namespace Derg
{
    // An environment providing various utility functions that don't really fit
    // in other environments.
    public class UtilEnv
    {
        public Machine machine;
        public EmscriptenEnv emscriptenEnv;

        public UtilEnv(Machine machine, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.emscriptenEnv = emscriptenEnv;
        }

        public void RegisterHostFuncs()
        {
            machine.RegisterReturningHostFunc<int, int, uint, int, int>(
                "env",
                "fd_write_buf",
                fd_write_buf
            );
        }

        // Writes a buffer to a file descriptor.
        //
        // If the bufPtr is 0, returns -EFAULT.
        //
        // If fd == 1, writes to stdout. In this case, though, the buffer must be
        // decodable as a UTF-8 string. If the buffer couldn't be decoded, then
        // -EINVAL is returned.
        //
        // If fd != 1, -EBADF is returned.
        //
        // Writes the number of bytes written in the memory pointed to by nwrittenPtr,
        // unless nwrittenPtr is 0.
        public int fd_write_buf(Frame frame, int fd, int bufPtr, uint bufLen, int nwrittenPtr)
        {
            if (bufPtr == 0)
            {
                return -Errno.EFAULT;
            }
            if (fd != 1)
            {
                return -Errno.EBADF;
            }
            try
            {
                string str = emscriptenEnv.GetUTF8StringFromMem(bufPtr, bufLen);
                if (emscriptenEnv.outputWriter != null)
                {
                    emscriptenEnv.outputWriter(str);
                }
                else
                {
                    Console.Write(str);
                }
                if (nwrittenPtr != 0)
                {
                    machine.MemSet(nwrittenPtr, bufLen);
                }
                return 0;
            }
            catch (Exception)
            {
                return -Errno.EINVAL;
            }
        }
    }
}
