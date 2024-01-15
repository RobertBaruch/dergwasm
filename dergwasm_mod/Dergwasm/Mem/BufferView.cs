using System;
using System.Collections;
using System.Collections.Generic;

namespace Derg.Mem
{
    public unsafe readonly struct BufferView<T> : IEnumerable<T> where T : unmanaged
    {
        private readonly int _length;
        private readonly Memory<byte> _memory;

        public int Length => _length;

        public unsafe BufferView(Wasm.Buffer<T> buffer, Memory mem)
        {
            var memAcc = mem.AsMemory();

            var start = buffer.Ptr;
            var length = sizeof(T) * buffer.Length;
            _memory = memAcc.Slice(start, length);
            _length = buffer.Length;
        }

        public unsafe T this[int i]
        {
            get
            {
                fixed (byte* mem = _memory.Span)
                {
                    var span = new Span<T>((T*)mem, _length);
                    return span[i];
                }
            }
            set
            {
                fixed (byte* mem = _memory.Span)
                {
                    var span = new Span<T>((T*)mem, _length);
                    span[i] = value;
                }
            }
        }

        public void CopyFrom(ReadOnlySpan<T> source)
        {
            fixed (byte* mem = _memory.Span)
            {
                var dest = new Span<T>((T*)mem, _length);
                source.CopyTo(dest);
            }
        }

        public void CopyTo(Span<T> dest)
        {
            fixed (byte* mem = _memory.Span)
            {
                var source = new Span<T>((T*)mem, _length);
                source.CopyTo(dest);
            }
        }

        public MemoryViewEnumerator GetEnumerator() => new MemoryViewEnumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct MemoryViewEnumerator : IEnumerator<T>
        {
            private readonly BufferView<T> _view;
            private int _current;

            public MemoryViewEnumerator(BufferView<T> view)
            {
                _current = -1;
                _view = view;
            }

            public T Current => _view[_current];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _current++;
                return _current < _view.Length;
            }

            public void Reset()
            {
                _current = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}