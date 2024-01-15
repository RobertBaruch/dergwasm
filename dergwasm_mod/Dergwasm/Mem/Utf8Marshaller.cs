using System;
using System.Text;
using Derg.Wasm;

namespace Derg.Mem
{
    public readonly struct Utf8Marshaller : IMarshaller<string>
    {
        private readonly BlitMarshaller<Pointer> PtrMarshaller;

        public int Length(in string obj) => PtrMarshaller.Length(default);

        public void ToMem(in string obj, Span<byte> memory, in MemoryContext ctx)
        {
            // Allocate the string.
            var buffer = ctx.Allocate(Encoding.UTF8.GetByteCount(obj) + 1);
            var bytes = Encoding.UTF8.GetBytes(obj);
            var bufferMem = ctx.Get(buffer);
            bufferMem.CopyFrom(bytes);
            bytes[bytes.Length - 1] = 0; // Null terminator

            PtrMarshaller.ToMem(buffer.ToPointer(), memory, in ctx);
        }

        public unsafe string FromMem(ReadOnlySpan<byte> memory, in MemoryContext ctx)
        {
            var ptr = PtrMarshaller.FromMem(memory, in ctx);
            var buffPtr = ptr.Reinterpret<byte>();

            var buff = ctx.RemainingMemory(buffPtr);
            fixed (byte* p = buff.Span)
            {
                return Encoding.UTF8.GetString(p, buff.Length);
            }
        }
    }
}
