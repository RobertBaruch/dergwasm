using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;

namespace Derg.Wasm
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
            Id = (ulong)obj.ReferenceID;
        }

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

        public static implicit operator RefID(WasmRefID<T> p) => new RefID(p.Id);

        public static implicit operator WasmRefID(WasmRefID<T> p) => new WasmRefID(p.Id);
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
