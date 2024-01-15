using System.Runtime.InteropServices;
using Derg.Mem;

namespace Derg.Wasm
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Buffer
    {
        public readonly int Ptr;
        public readonly int Length;

        public Buffer(int ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public Pointer ToPointer() => new Pointer(Ptr);

        public Buffer<T> Reinterpret<T>() where T : unmanaged => new Buffer<T>(Ptr, Length);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Buffer<T> where T : unmanaged
    {
        public readonly int Ptr;
        public readonly int Length;

        public Buffer(int ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public Pointer<T> ToPointer() => new Pointer<T>(Ptr);

        public BufferView<T> ToView(Memory mem) => new BufferView<T>(this, mem);
    }
}
