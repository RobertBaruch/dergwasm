using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Derg;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace DergwasmTests
{
    public class TestWorldServices : IWorldServices
    {
        Dictionary<RefID, IWorldElement> objects = new Dictionary<RefID, IWorldElement>();
        Dictionary<Uri, string> assetFiles = new Dictionary<Uri, string>();

        public void AddRefID(IWorldElement obj, ulong i)
        {
            objects.Add(new RefID(i), obj);
        }

        public void AddAssetFile(Uri uri, string file)
        {
            assetFiles.Add(uri, file);
        }

        public override async ValueTask<string> GatherAssetFile(
            Uri assetURL,
            float priority,
            DB_Endpoint? overrideEndpoint = null
        )
        {
            Func<string> callback = () => assetFiles[assetURL];
            return await new Task<string>(callback);
        }

        public override string GetName() => "TestWorld";

        public override IWorldElement GetObjectOrNull(RefID refID) =>
            objects.TryGetValue(refID, out IWorldElement obj) ? obj : null;

        public override ISlot GetRootSlot()
        {
            throw new NotImplementedException();
        }

        public override Task<T> StartTask<T>(Func<Task<T>> task, IUpdatable updatable = null) =>
            task();
    }
}
