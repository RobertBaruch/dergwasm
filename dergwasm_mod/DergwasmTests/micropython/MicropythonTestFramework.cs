using System;
using Derg;
using DergwasmTests.testing;
using FrooxEngine;

namespace DergwasmTests.micropython
{
    public class MicropythonTestFramework
    {
        public FakeWorldServices worldServices = new FakeWorldServices();
        public FakeSlot dergwasmSlot;
        public FakeSlot wasmBinarySlot;
        public StaticBinary binary;

        public MicropythonTestFramework()
        {
            ResonitePatches.Apply();

            dergwasmSlot = worldServices.GetRootSlot().AddSlot("Dergwasm") as FakeSlot;
            dergwasmSlot.Tag = "_dergwasm";
            wasmBinarySlot = dergwasmSlot.AddSlot("firmware.wasm") as FakeSlot;
            wasmBinarySlot.Tag = "_dergwasm_wasm_file";
            binary = wasmBinarySlot.AttachComponent<StaticBinary>();
            binary.URL.Value = new Uri("file:///firmware.wasm");

            worldServices.AddAssetFile(
                new Uri("file:///firmware.wasm"),
                "../../../../../firmware.wasm"
            );

            DergwasmMachine.InitStage0(worldServices);
        }
    }
}
