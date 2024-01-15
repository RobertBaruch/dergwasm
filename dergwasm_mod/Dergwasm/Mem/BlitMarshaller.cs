using System;

namespace Derg.Mem
{
    public struct BlitMarshaller<T> : IMarshaller<T> where T : unmanaged
    {
        public unsafe int Length(in T len) => sizeof(T);

        public unsafe T FromMem(ReadOnlySpan<byte> memory, in MemoryContext ctx)
        {
            fixed (byte* p = memory)
            {
                return *(T*)p;
            }
        }

        public unsafe void ToMem(in T obj, Span<byte> memory, in MemoryContext ctx)
        {
            fixed (byte* p = memory)
            {
                *(T*)p = obj;
            }
        }
    }
}
