using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;

namespace Derg.Wasm
{
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

        public static implicit operator RefID(WasmRefID<T> p) => new RefID(p.Id);
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
