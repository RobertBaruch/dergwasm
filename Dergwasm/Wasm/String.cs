using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Derg.Wasm
{
    // Represents a pointer to bytes to be interpreted as a NUL-terminated UTF8-encoded
    // string.
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct NullTerminatedString
    {
        public readonly Ptr<byte> Data;

        public NullTerminatedString(Ptr<byte> data)
        {
            Data = data;
        }

        public NullTerminatedString(Buff<byte> buffer)
        {
            Data = buffer.Ptr;
        }

        public NullTerminatedString(int addr)
        {
            Data = new Ptr<byte>(addr);
        }
    }
}
