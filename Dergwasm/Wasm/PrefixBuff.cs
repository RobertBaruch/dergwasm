using System.Runtime.InteropServices;
using Dergwasm.Runtime;

namespace Dergwasm.Wasm
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PrefixBuff
    {
        public readonly Ptr Ptr;

        public Ptr<int> Length => Ptr.Reinterpret<int>();

        public Ptr BufferStart => Ptr.Offset(4);

        public PrefixBuff(Ptr ptr)
        {
            Ptr = ptr;
        }

        public Buff ToBuff(Machine machine) => new Buff(BufferStart, machine.HeapGet(Length));

        public PrefixBuff<T> Reinterpret<T>()
            where T : struct => new PrefixBuff<T>(this);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PrefixBuff<T>
        where T : struct
    {
        public readonly PrefixBuff Buff;

        public Ptr Ptr => Buff.Ptr;

        public Ptr<int> Length => Buff.Length;

        public Ptr<T> BufferStart => Buff.BufferStart.Reinterpret<T>();

        public PrefixBuff(PrefixBuff buff)
        {
            Buff = buff;
        }

        public PrefixBuff(Ptr addr)
        {
            Buff = new PrefixBuff(addr);
        }

        public Buff<T> ToBuff(Machine machine) => new Buff<T>(BufferStart, machine.HeapGet(Length));

        public static implicit operator PrefixBuff(PrefixBuff<T> p) => p.Buff;
    }
}
