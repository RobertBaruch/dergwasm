using System;
using System.Threading.Tasks;
using Dergwasm.Wasm;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace Dergwasm.Resonite
{
    public class World : IWorld
    {
        FrooxEngine.World world;

        public World(FrooxEngine.World world)
        {
            this.world = world;
        }

        public string GetName() => world.Name;

        public IWorldElement GetObjectOrNull(RefID refID) =>
            world.ReferenceController.GetObjectOrNull(refID);

        public T GetObjectOrNull<T>(WasmRefID<T> wasmRefID)
            where T : class, IWorldElement =>
            world.ReferenceController.GetObjectOrNull(wasmRefID) as T;

        public Slot GetRootSlot() => world.RootSlot;

        public async ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        ) => await world.Engine.AssetManager.GatherAssetFile(assetURL, priority, overrideEndpoint);

        public Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null) =>
            world.Coroutines.StartTask<T>(task, updatable);

        public async void ToBackground() => await new ToBackground();

        public async void ToWorld() => await new ToWorld();
    }
}
