using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;

namespace Derg.Wasm
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WRefId
    {
        public readonly ulong Id;

        public WRefId(RefID id)
        {
            Id = (ulong)id;
        }

        public WRefId<T> Reinterpret<T>() where T : class, IWorldElement
        {
            return new WRefId<T>(Id);
        }

        public IWorldElement Get(IWorldServices worldServices)
        {
            return worldServices.GetObjectOrNull(new RefID(Id));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WRefId<T> where T : class, IWorldElement
    {
        public readonly ulong Id;

        public WRefId(RefID id)
        {
            Id = (ulong)id;
        }

        public T Get(IWorldServices worldServices)
        {
            return worldServices.GetObjectOrNull(new RefID(Id)) as T;
        }

        public static implicit operator WRefId(WRefId<T> p) => new WRefId(p.Id);
    }

    public static class WRefIdExtensions
    {
        public static WRefId<T> GetWasmRef<T>(this T val) where T : class, IWorldElement
        {
            return new WRefId<T>(val.ReferenceID);
        }
    }
}