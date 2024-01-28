using System;
using System.Text;
using Derg.Wasm;

namespace Derg.Mem
{
    public readonly struct TerminatedUtf8Marshaller : IMarshaller<string>
    {
        private readonly BlitMarshaller<Pointer> PtrMarshaller;

        public int Length(in string obj) => PtrMarshaller.Length(default);

        public Buffer<byte> PutString(string obj, in MemoryContext ctx)
        {
            // Allocate the string.
            var buffer = ctx.Allocate(Encoding.UTF8.GetByteCount(obj) + 1);
            var bytes = Encoding.UTF8.GetBytes(obj);
            var bufferMem = ctx.View(buffer);
            bufferMem.CopyFrom(bytes);
            bytes[bytes.Length - 1] = 0; // Null terminator
            return buffer;
        }

        public unsafe string GetString(Pointer<byte> ptr, in MemoryContext ctx)
        {
            var buff = ctx.RemainingMemory(ptr);
            fixed (byte* p = buff.Span)
            {
                return Encoding.UTF8.GetString(p, buff.Length);
            }
        }

        public void ToMem(in string obj, Span<byte> memory, in MemoryContext ctx)
        {
            var buffer = PutString(obj, in ctx);

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

        public Value ToValue(in string obj, in MemoryContext ctx)
        {
            var buffer = PutString(obj, in ctx);
            // Store the ptr only, as this is null terminated.
            return Value.From(buffer.Ptr);
        }

        public string FromValue(in Value value, in MemoryContext ctx)
        {
            var ptr = new Pointer<byte>(value.As<int>());

            return GetString(ptr, in ctx);
        }
    }
}
