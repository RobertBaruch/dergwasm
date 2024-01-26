using System;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace Derg
{
    public class WorldServices : IWorldServices
    {
        World world;

        public WorldServices(World world)
        {
            this.world = world;
        }

        public override string GetName() => world.Name;

        public override IWorldElement GetObjectOrNull(RefID refID) =>
            world.ReferenceController.GetObjectOrNull(refID);

        public override ISlot GetRootSlot() => new SlotProxy(world.RootSlot);

        public override async ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        ) => await world.Engine.AssetManager.GatherAssetFile(assetURL, priority, overrideEndpoint);

        public override Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null) =>
            world.Coroutines.StartTask<T>(task, updatable);
    }
}
