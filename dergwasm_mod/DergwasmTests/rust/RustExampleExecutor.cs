using System;
using System.Collections.Generic;
using Derg;
using DergwasmTests.testing;
using FrooxEngine;
using Xunit;

namespace DergwasmTests.micropython
{
    public class RustExampleExecutor
    {
        [Fact(
            Skip = "Run `cargo build --example slot_dumper --release` in the rust module before this can run."
        )]
        public void RunSlotDumper()
        {
            FakeWorldServices worldServices = new FakeWorldServices();

            ResonitePatches.Apply();

            var dergwasmSlot = worldServices.GetRootSlot().AddSlot("Dergwasm") as FakeSlot;
            dergwasmSlot.Tag = "_dergwasm";
            var wasmBinarySlot = dergwasmSlot.AddSlot("firmware.wasm") as FakeSlot;
            wasmBinarySlot.Tag = "_dergwasm_wasm_file";
            var binary = wasmBinarySlot.AttachComponent<StaticBinary>();
            binary.URL.Value = new Uri("file:///firmware.wasm");
            worldServices.GetRootSlot().AddSlot("Example");

            worldServices.AddAssetFile(
                new Uri("file:///firmware.wasm"),
                "../../../../../dergwasm_lib/rust/target/wasm32-wasi/release/examples/slot_dumper.wasm"
            );

            DergwasmMachine.InitStage0(worldServices);

            string text = "";

            DergwasmMachine.machine.Debug = true;
            DergwasmMachine.emscriptenEnv.outputWriter = s => text += s;

            DergwasmMachine.resoniteEnv.InvokeWasmFunction("_start", new List<Value>());
        }
    }
}
