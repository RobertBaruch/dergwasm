using System.Runtime.InteropServices;

namespace Dergwasm.Wasm
{
    // Represents a pointer to a bunch of T's. The length must be stored separately.
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WasmArray<T>
        where T : struct
    {
        public readonly Ptr<T> Data;

        public WasmArray(Ptr<T> data)
        {
            Data = data;
        }

        public WasmArray(Buff<T> buffer)
        {
            Data = buffer.Ptr;
        }

        public WasmArray(int addr)
        {
            Data = new Ptr<T>(addr);
        }
    }
}
