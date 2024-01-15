using System;
using Derg.Wasm;

namespace Derg.Mem
{
    public readonly struct MemoryContext
    {
        private readonly EmscriptenEnv _emscripten;
        private readonly Frame _frame;
        private readonly Memory _memory;

        public unsafe Pointer<T> Allocate<T>() where T : unmanaged
        {
            return new Pointer<T>(_emscripten.Malloc(_frame, sizeof(T)));
        }

        public unsafe Buffer<byte> Allocate(int size)
        {
            return new Buffer<byte>(_emscripten.Malloc(_frame, size), size);
        }

        public unsafe BufferView<T> Get<T>(Buffer<T> buffer) where T : unmanaged {
            return buffer.ToView(_memory);
        }

        /// <summary>
        /// Returns a span from the pointer, to the end of memory.
        /// 
        /// This is highly unsafe, and should only be used in reading null terminated strings.
        /// </summary>
        /// <param name="start">A pointer to the start of the block of memory.</param>
        /// <returns>A span from start to the end of memory.</returns>
        public unsafe Memory<byte> RemainingMemory(Pointer<byte> start) {
            return _memory.AsMemory().Slice(start.Ptr);
        }

        public T TryRead<T>(Pointer<T> ptr) where T : unmanaged {
            return ptr.ToBuffer().ToView(_memory)[0];
        }
    }
}