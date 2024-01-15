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

        public Pointer(int ptr)
        {
            Ptr = ptr;
        }

        public Pointer<T> Reinterpret<T>() where T : unmanaged => new Pointer<T>(Ptr);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Pointer<T> where T : unmanaged
    {
        public readonly int Ptr;

        public Pointer(int ptr)
        {
            Ptr = ptr;
        }

        public Buffer<T> ToBuffer() => new Buffer<T>(Ptr, 1);

        public Pointer<TNew> Reinterpret<TNew>() where TNew : unmanaged => new Pointer<TNew>(Ptr);

        public static implicit operator Pointer(Pointer<T> p) => new Pointer(p.Ptr);
    }
}
