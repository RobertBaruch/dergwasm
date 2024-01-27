using System;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace Derg
{
    // Interface for calls involving World, for testability.
    public abstract class IWorldServices
    {
        // Gets the world name.
        public abstract string GetName();

        // Gets the world root slot.
        public abstract ISlot GetRootSlot();

        // Gets the IWorldElement for the refID, or null if it doesn't exist.
        public abstract IWorldElement GetObjectOrNull(RefID refID);

        // Gets an asset from the assetURL, puts it in a file, and returns the filename.
        public abstract ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        );

        // Equivalent to Worker.StartGlobalTask
        public abstract Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null);

        // Moves the current async context to the background thread.
        public abstract void ToBackground();

        // Moves the current async context to the main thread.
        public abstract void ToWorld();
    }
}
