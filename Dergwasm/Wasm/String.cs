using System.Runtime.InteropServices;

namespace Dergwasm.Wasm
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
