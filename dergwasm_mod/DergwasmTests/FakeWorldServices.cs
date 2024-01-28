using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Derg;
using Derg.Wasm;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace DergwasmTests
{
    public class FakeWorldServices : IWorldServices
    {
        Dictionary<RefID, IWorldElement> objects = new Dictionary<RefID, IWorldElement>();
        Dictionary<Uri, string> assetFiles = new Dictionary<Uri, string>();
        FakeSlot root;
        ulong nextRefID = 1;

        public FakeWorldServices()
        {
            root = new FakeSlot(this, "Root");
        }

        public ulong GetNextRefID() => nextRefID++;

        public void AddRefID(IWorldElement obj, ulong i)
        {
            objects.Add(new RefID(i), obj);
        }

        public void AddRefID(IWorldElement obj, RefID i)
        {
            objects.Add(i, obj);
        }

        public void AddAssetFile(Uri uri, string file)
        {
            assetFiles.Add(uri, file);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        ) => assetFiles[assetURL];
#pragma warning restore CS1998

        public override string GetName() => "TestWorld";

        public override IWorldElement GetObjectOrNull(RefID refID) =>
            objects.TryGetValue(refID, out IWorldElement obj) ? obj : null;

        public override T GetObjectOrNull<T>(WasmRefID<T> wasmRefID) => GetObjectOrNull(wasmRefID);

        public override ISlot GetRootSlot() => root;

        public override Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null)
        {
            Task<T> runningTask = Task.Run(task);
            runningTask.Wait();
            return runningTask;
        }

        public override void ToBackground() { }

        public override void ToWorld() { }
    }
}
