using System;

namespace Derg.Mem
{
    public interface IMarshaller<T>
    {
        int Length(in T obj);
        void ToMem(in T obj, Span<byte> memory, in MemoryContext ctx);
        T FromMem(ReadOnlySpan<byte> memory, in MemoryContext ctx);
    }
}
