using System;
using Derg.Wasm;

namespace Derg.Mem
{
    public readonly struct MemoryContext
    {
        private readonly Machine _machine;
        private readonly Frame _frame;

        private IWasmAllocator Allocator => _machine.Allocator;
        private Memory Memory => _machine.memories[0];

        public MemoryContext(Machine machine, Frame frame)
        {
            _machine = machine;
            _frame = frame;
        }

        public unsafe Pointer<T> Allocate<T>() where T : unmanaged
        {
            return Allocator.Malloc(_frame, sizeof(T)).Reinterpret<T>();
        }

        public unsafe Buffer<byte> Allocate(int size)
        {
            return Allocator.Malloc(_frame, size).Reinterpret<byte>(size);
        }

        public unsafe BufferView<T> BufferView<T>(Buffer<T> buffer) where T : unmanaged
        {
            return buffer.ToView(Memory);
        }

        /// <summary>
        /// Returns a span from the pointer, to the end of memory.
        /// 
        /// This is highly unsafe, and should only be used in reading null terminated strings.
        /// </summary>
        /// <param name="start">A pointer to the start of the block of memory.</param>
        /// <returns>A span from start to the end of memory.</returns>
        public unsafe Memory<byte> RemainingMemory(Pointer<byte> start)
        {
            return Memory.AsMemory().Slice(start.Ptr);
        }

        public T TryRead<T>(Pointer<T> ptr) where T : unmanaged
        {
            return ptr.ToBuffer().ToView(Memory)[0];
        }
    }
}
