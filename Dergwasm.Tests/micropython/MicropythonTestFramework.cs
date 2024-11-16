using System;
using Dergwasm;
using DergwasmTests.testing;
using FrooxEngine;

namespace DergwasmTests.micropython
{
    public class MicropythonTestFramework
    {
        public FakeWorld world = new FakeWorld();
        public FakeDergwasmSlots dergwasmSlots = new FakeDergwasmSlots();

        public MicropythonTestFramework()
        {
            ResonitePatches.Apply();

            world.AddAssetFile(new Uri("file:///firmware.wasm"), "firmware.wasm");

            DergwasmMachine.InitStage0(world, dergwasmSlots);
        }
    }
}
