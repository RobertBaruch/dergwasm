using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Derg;
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
        ulong nextRefID = 2;

        public FakeWorldServices()
        {
            root = new FakeSlot(this, "Root", new RefID(1));
            AddRefID(root, root.ReferenceID);
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

        public override async ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        ) => assetFiles[assetURL];

        public override string GetName() => "TestWorld";

        public override IWorldElement GetObjectOrNull(RefID refID) =>
            objects.TryGetValue(refID, out IWorldElement obj) ? obj : null;

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
