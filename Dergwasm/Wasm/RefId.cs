using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dergwasm.Runtime;
using Elements.Core;
using FrooxEngine;

namespace Dergwasm.Wasm
{
    /// <summary>
    /// A utility type to use in blit marshalling where C# < 7.3 does not allow
    /// unmanaged generic types as blittable types. In most cases the hard type
    /// version of this should be used, except when passed into
    /// <see cref="BlitMarshaller{WasmRefID}"/>, where it will be auto downcast.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WasmRefID
    {
        public readonly ulong Id;

        public WasmRefID(RefID id)
        {
            Id = (ulong)id;
        }

        public WasmRefID(IWorldElement obj)
        {
            Id = obj == null ? 0UL : (ulong)obj.ReferenceID;
        }

        public bool IsNullRef => Id == 0;

        public WasmRefID<T> Reinterpret<T>()
            where T : class, IWorldElement => new WasmRefID<T>(new RefID(Id));

        public static implicit operator RefID(WasmRefID p) => new RefID(p.Id);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WasmRefID<T>
        where T : class, IWorldElement
    {
        public readonly ulong Id;

        public WasmRefID(RefID id)
        {
            Id = (ulong)id;
        }

        public WasmRefID(T obj)
        {
            Id = (ulong)obj.ReferenceID;
        }

        public bool IsNullRef => Id == 0;

        public static implicit operator RefID(WasmRefID<T> p) => new RefID(p.Id);

        public static implicit operator WasmRefID(WasmRefID<T> p) => new WasmRefID(p.Id);
    }

    // A list of WasmRefID<T>. This is represented as a buffer, where the buffer's count
    // is the number of elements.
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WasmRefIDList<T>
        where T : class, IWorldElement
    {
        public readonly Buff<WasmRefID<T>> buff;

        public WasmRefIDList(Machine machine, Frame frame, List<WasmRefID<T>> refIdList)
        {
            buff = machine.HeapAlloc<WasmRefID<T>>(frame, refIdList.Count);
            Ptr<WasmRefID<T>> ptr = buff.Ptr;
            foreach (WasmRefID<T> refId in refIdList)
            {
                machine.HeapSet(ptr, refId);
                ptr++;
            }
        }

        public static WasmRefIDList<U> Make<U>(Machine machine, Frame frame, IEnumerable<U> objList)
            where U : class, IWorldElement
        {
            return new WasmRefIDList<U>(
                machine,
                frame,
                objList.Select(obj => obj.GetWasmRef()).ToList()
            );
        }

        public static WasmRefIDList<U> Make<U>(
            Machine machine,
            Frame frame,
            IEnumerable<WasmRefID<U>> refList
        )
            where U : class, IWorldElement
        {
            return new WasmRefIDList<U>(machine, frame, refList.ToList());
        }
    }

    public static class WasmRefIdExtensions
    {
        public static WasmRefID<T> GetWasmRef<T>(this T val)
            where T : class, IWorldElement
        {
            return new WasmRefID<T>(val);
        }
    }
}
