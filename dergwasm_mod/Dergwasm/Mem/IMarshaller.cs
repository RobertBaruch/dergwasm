using System;

namespace Derg.Mem
{
    public interface IMarshaller<T>
    {
        /// <summary>
        /// The length of the struct in bytes, this is the size reserved in the stack or heap for the value.
        /// </summary>
        int Length(in T obj);

        void ToMem(in T obj, Span<byte> memory, in MemoryContext ctx);
        T FromMem(ReadOnlySpan<byte> memory, in MemoryContext ctx);

        /// <summary>
        /// Converts an object into a single value object.
        /// </summary>
        Value ToValue(in T obj, in MemoryContext ctx);
        /// <summary>
        /// Converts a single value object into this object.
        /// </summary>
        T FromValue(in Value value, in MemoryContext ctx);
    }
}
