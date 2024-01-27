using System.Runtime.InteropServices;

namespace Derg.Wasm
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

        public Ptr<T> Reinterpret<T>() where T : unmanaged => new Ptr<T>(Addr);

        public Buff<T> Reinterpret<T>(int length) where T : unmanaged => new Buff<T>(Addr, length);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Ptr<T> where T : unmanaged
    {
        public readonly int Addr;

        public bool IsNull => Addr == 0;

        public static Ptr<T> Null => new Ptr<T>(0);

        public Ptr(int ptr)
        {
            Addr = ptr;
        }

        public Buff<T> ToBuffer(int length = 1) => new Buff<T>(Addr, length);

        public Ptr<TNew> Reinterpret<TNew>() where TNew : unmanaged => new Ptr<TNew>(Addr);

        public static implicit operator Ptr(Ptr<T> p) => new Ptr(p.Addr);
    }
}
