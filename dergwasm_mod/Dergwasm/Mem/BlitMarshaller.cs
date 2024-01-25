using System;

namespace Derg.Mem
{
    public struct BlitMarshaller<T> : IMarshaller<T> where T : unmanaged
    {
        public unsafe int Length(in T len) => sizeof(T);

        public unsafe T FromMem(ReadOnlySpan<byte> memory, Machine machine, Frame frame)
        {
            fixed (byte* p = memory)
            {
                return *(T*)p;
            }
        }

        public unsafe void ToMem(in T obj, Span<byte> memory, Machine machine, Frame frame)
        {
            fixed (byte* p = memory)
            {
                *(T*)p = obj;
            }
        }

        public Value ToValue(in T obj, Machine machine, Frame frame)
        {
            return Value.From(obj);
        }

        public T FromValue(in Value value, Machine machine, Frame frame)
        {
            return value.As<T>();
        }
    }
}
