using System.Runtime.InteropServices;

namespace Derg.Wasm
{
    /// <summary>
    /// Represents a buffer that encapsulates a pointer and a length. This structure
    /// is used to manage a block of memory, providing two constructors for initializing
    /// the buffer either with an integer representing the memory address or with a Ptr
    /// object. It offers a method to retrieve the underlying pointer and a generic method
    /// to reinterpret the buffer as a different unmanaged type.
    ///
    /// Note: Untyped buffers are 64-bit values in Wasm, encoded as a 32-bit pointer
    /// and a 32-bit byte length. When passed from C# to Wasm, the 32-bit pointer is
    /// generally a pointer to allocated memory in the Wasm heap, which means that the
    /// receiver is responsible for freeing that memory.
    /// </summary>
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

        public Buff<T> Reinterpret<T>()
            where T : unmanaged => new Buff<T>(Ptr.Reinterpret<T>(), Length);
    }

    /// <summary>
    /// Represents a generic buffer for unmanaged types, encapsulating a typed pointer
    /// and a length. This structure is designed to handle memory blocks in a type-safe
    /// manner. It provides constructors for initializing the buffer with either a typed
    /// pointer or an integer representing the memory address. The buffer includes a method
    /// to calculate the byte length of the memory block it represents, and a method to
    /// retrieve the underlying typed pointer. Additionally, it defines an implicit
    /// conversion operator to convert a Buff<T> to a non-generic Buff. The structure
    /// is constrained to unmanaged types, ensuring type safety for pointer operations.
    ///
    /// Note: Typed buffers are 64-bit values in Wasm, encoded as a 32-bit pointer
    /// and a 32-bit number of elements. When passed from C# to Wasm, the 32-bit pointer is
    /// generally a pointer to allocated memory in the Wasm heap, which means that the
    /// receiver is responsible for freeing that memory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Buff<T>
        where T : unmanaged
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
