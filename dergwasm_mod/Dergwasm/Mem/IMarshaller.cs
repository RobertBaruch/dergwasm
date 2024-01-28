using System;

namespace Derg.Mem
{
    public interface IMarshaller<T>
    {
        /// <summary>
        /// The length of the struct in bytes, this is the size reserved in the stack or heap for the value.
        /// </summary>
        int Length(in T obj);

        void ToMem(in T obj, Span<byte> memory, Machine machine, Frame frame);
        T FromMem(ReadOnlySpan<byte> memory, Machine machine, Frame frame);

        /// <summary>
        /// Converts an object into a single value object.
        /// </summary>
        Value ToValue(in T obj, Machine machine, Frame frame);
        /// <summary>
        /// Converts a single value object into this object.
        /// </summary>
        T FromValue(in Value value, Machine machine, Frame frame);
    }
}
