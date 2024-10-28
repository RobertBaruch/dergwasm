using System;
using Dergwasm;
using DergwasmTests.testing;
using FrooxEngine;

namespace DergwasmTests.micropython
{
    public class MicropythonTestFramework
    {
        public FakeWorldServices worldServices = new FakeWorldServices();
        public FakeDergwasmSlots dergwasmSlots = new FakeDergwasmSlots();

        public MicropythonTestFramework()
        {
            ResonitePatches.Apply();

            worldServices.AddAssetFile(new Uri("file:///firmware.wasm"), "firmware.wasm");

            DergwasmMachine.InitStage0(worldServices, dergwasmSlots);
        }
    }
}
