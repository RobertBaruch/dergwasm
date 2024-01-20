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

        // This is called in a reflection context.
        public MemoryContext(Machine machine, Frame frame)
        {
            _machine = machine;
            _frame = frame;
        }

        public unsafe Pointer<T> Allocate<T>() where T : unmanaged
        {
            return Allocator.Malloc(_frame, sizeof(T)).Reinterpret<T>();
        }

        public unsafe Buffer<T> Allocate<T>(int length) where T : unmanaged
        {
            return Allocator.Malloc(_frame, sizeof(T) * length).Reinterpret<T>().ToBuffer(length);
        }

        public Buffer<byte> Allocate(int size)
        {
            return Allocator.Malloc(_frame, size).Reinterpret<byte>(size);
        }

        /// <summary>
        /// Returns a span from the pointer, to the end of memory.
        /// 
        /// This is highly unsafe, and should only be used in reading null terminated strings.
        /// </summary>
        /// <param name="start">A pointer to the start of the block of memory.</param>
        /// <returns>A span from start to the end of memory.</returns>
        public Memory<byte> RemainingMemory(Pointer<byte> start)
        {
            return Memory.AsMemory().Slice(start.Ptr);
        }
    }
}
