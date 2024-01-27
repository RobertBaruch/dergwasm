using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;

namespace Derg.Wasm
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct WasmRefID<T> where T : class, IWorldElement
    {
        public readonly ulong Id;

        public WasmRefID(RefID id)
        {
            Id = (ulong)id;
        }

        public T Get(IWorldServices worldServices)
        {
            return worldServices.GetObjectOrNull(new RefID(Id)) as T;
        }

        public static implicit operator RefID(WasmRefID<T> p) => new RefID(p.Id);
    }

    public static class WRefIdExtensions
    {
        public static WasmRefID<T> GetWasmRef<T>(this T val) where T : class, IWorldElement
        {
            return new WasmRefID<T>(val.ReferenceID);
        }
    }
}