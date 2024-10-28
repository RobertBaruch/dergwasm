using System;
using System.Threading.Tasks;
using Dergwasm;
using FrooxEngine;

namespace DergwasmTests.testing
{
    public class FakeDergwasmSlots : IDergwasmSlots
    {
        public Slot DergwasmRoot => null;

        public Slot WasmBinarySlot => null;

        public Slot ConsoleSlot => null;

        public Slot FilesystemSlot => null;

        public bool Ready => true;

        public async Task<string> GatherWasmBinary(IWorldServices worldServices)
        {
            return await worldServices.GatherAssetFile(new Uri("file:///firmware.wasm"), 100f);
        }
    }
}
