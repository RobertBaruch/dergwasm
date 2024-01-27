using System.Runtime.InteropServices;

namespace Derg.Wasm
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Buff
    {
        public readonly Ptr Ptr;
        public readonly int Length;

        public Buff(int ptr, int length)
        {
            Ptr = new Ptr(ptr);
            Length = length;
        }

        public Buff(Ptr ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public Ptr ToPointer() => Ptr;

        public Buff<T> Reinterpret<T>() where T : unmanaged => new Buff<T>(Ptr.Reinterpret<T>(), Length);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Buff<T> where T : unmanaged
    {
        public readonly Ptr<T> Ptr;
        public readonly int Length;

        public unsafe int ByteLength() => Length * sizeof(T);

        public Buff(Ptr<T> ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public Buff(int ptr, int length)
        {
            Ptr = new Ptr<T>(ptr);
            Length = length;
        }

        public Ptr<T> ToPointer() => Ptr;

        public static implicit operator Buff(Buff<T> p) => new Buff(p.Ptr, p.Length);
    }
}
