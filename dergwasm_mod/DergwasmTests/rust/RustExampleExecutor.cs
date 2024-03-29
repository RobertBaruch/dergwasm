using System;
using System.Collections.Generic;
using Derg;
using Derg.Runtime;
using DergwasmTests.testing;
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
            FakeDergwasmSlots dergwasmSlots = new FakeDergwasmSlots();

            ResonitePatches.Apply();

            worldServices.AddAssetFile(
                new Uri("file:///firmware.wasm"),
                "../../../../../firmware.wasm"
            );

            DergwasmMachine.InitStage0(worldServices, dergwasmSlots);

            string text = "";

            DergwasmMachine.machine.Debug = true;
            DergwasmMachine.emscriptenEnv.outputWriter = s => text += s;

            DergwasmMachine.resoniteEnv.InvokeWasmFunction("_start", new List<Value>());
        }
    }
}
