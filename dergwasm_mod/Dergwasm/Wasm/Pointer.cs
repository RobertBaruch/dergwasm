using Derg.Mem;
using System.Runtime.InteropServices;

namespace Derg.Wasm
{
    /// <summary>
    /// A utility type to use in blit marshalling where C# < 7.3 does not allow unmanaged generic types as bitable types.
    /// In most cases the hard type version of this should be used, except when passed into <see cref="BlitMarshaller{Pointer}"/>, where it will be auto downcast. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Pointer
    {
        public readonly int Ptr;

        public bool Valid => Ptr != 0;

        public static Pointer Null => new Pointer(0);

        public Pointer(int ptr)
        {
            Ptr = ptr;
        }

        public Pointer<T> Reinterpret<T>() where T : unmanaged => new Pointer<T>(Ptr);

        public Buffer<T> Reinterpret<T>(int length) where T : unmanaged => new Buffer<T>(Ptr, length);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Pointer<T> where T : unmanaged
    {
        public readonly int Ptr;

        public bool Valid => Ptr != 0;

        public static Pointer<T> Null => new Pointer<T>(0);

        public Pointer(int ptr)
        {
            Ptr = ptr;
        }

        public Buffer<T> ToBuffer(int length = 1) => new Buffer<T>(Ptr, length);

        public Pointer<TNew> Reinterpret<TNew>() where TNew : unmanaged => new Pointer<TNew>(Ptr);

        public static implicit operator Pointer(Pointer<T> p) => new Pointer(p.Ptr);
    }
}
