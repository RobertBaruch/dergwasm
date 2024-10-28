using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dergwasm.Wasm
{
    /// <summary>
    /// A utility type to use in blit marshalling where C# < 7.3 does not allow unmanaged generic types as bitable types.
    /// In most cases the hard type version of this should be used, except when passed into <see cref="BlitMarshaller{Pointer}"/>, where it will be auto downcast.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Ptr
    {
        public readonly int Addr;

        public bool Valid => Addr != 0;

        public static Ptr Null => new Ptr(0);

        public Ptr(int ptr)
        {
            Addr = ptr;
        }

        public Ptr Offset(int offset) => new Ptr(Addr + offset);

        public Ptr<T> Reinterpret<T>()
            where T : struct => new Ptr<T>(Addr);

        public Buff<T> Reinterpret<T>(int length)
            where T : struct => new Buff<T>(Addr, length);

        public static Ptr operator ++(Ptr p) => new Ptr(p.Addr + 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Ptr<T>
        where T : struct
    {
        public readonly int Addr;

        public bool IsNull => Addr == 0;

        public static Ptr<T> Null => new Ptr<T>(0);

        public Ptr(int ptr)
        {
            Addr = ptr;
        }

        public Buff<T> ToBuffer(int length = 1) => new Buff<T>(Addr, length);

        public Ptr<TNew> Reinterpret<TNew>()
            where TNew : struct => new Ptr<TNew>(Addr);

        public static implicit operator Ptr(Ptr<T> p) => new Ptr(p.Addr);

        public static Ptr<T> operator ++(Ptr<T> p) => new Ptr<T>(p.Addr + Unsafe.SizeOf<T>());
    }

    // A pointer that is explicitly an output parameter.
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Output<T>
        where T : struct
    {
        public readonly Ptr<T> Ptr;

        public bool IsNull => Ptr.Addr == 0;

        public Output(Ptr<T> ptr)
        {
            Ptr = ptr;
        }

        public Output(int addr)
        {
            Ptr = new Ptr<T>(addr);
        }

        public static implicit operator Output<T>(Ptr<T> p) => new Output<T>(p);
    }
}
