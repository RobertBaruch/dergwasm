using System;
using System.Threading.Tasks;
using Dergwasm.Wasm;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace Dergwasm.Resonite
{
    // Interface for calls involving World, for testability.
    public interface IWorld
    {
        // Gets the world name.
        string GetName();

        // Gets the world root slot.
        Slot GetRootSlot();

        // Gets the IWorldElement for the refID, or null if it doesn't exist.
        IWorldElement GetObjectOrNull(RefID refID);

        T GetObjectOrNull<T>(WasmRefID<T> wasmRefID)
            where T : class, IWorldElement;

        // Gets an asset from the assetURL, puts it in a file, and returns the filename.
        ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        );

        // Equivalent to Worker.StartGlobalTask
        Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null);

        // Moves the current async context to the background thread.
        void ToBackground();

        // Moves the current async context to the main thread.
        void ToWorld();
    }
}
