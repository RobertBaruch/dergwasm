using System.Runtime.InteropServices;

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

        public unsafe int ByteLength() => Length * sizeof(T);

        public Buffer(int ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public Pointer<T> ToPointer() => new Pointer<T>(Ptr);

        public static implicit operator Buffer(Buffer<T> p) => new Buffer(p.Ptr, p.Length);
    }
}
