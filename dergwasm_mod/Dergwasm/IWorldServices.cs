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
        public abstract string GetName();
        public abstract ISlot GetRootSlot();

        public abstract IWorldElement GetObjectOrNull(RefID refID);

        public abstract ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        );

        // Equivalent to Worker.StartGlobalTask
        public abstract Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null);
    }
}
