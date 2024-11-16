using System;
using System.Threading.Tasks;
using Elements.Core; // For UniLog
using FrooxEngine;

namespace Dergwasm.Resonite
{
    public class DergwasmSlots : IDergwasmSlots
    {
        public Slot DergwasmRoot { get; }
        public Slot WasmBinarySlot { get; }

        public Slot ConsoleSlot { get; }
        public Slot FilesystemSlot { get; }

        public bool Ready => DergwasmRoot != null && WasmBinarySlot != null;

        public DergwasmSlots(IWorld world)
        {
            WasmBinarySlot = null;
            ConsoleSlot = null;
            FilesystemSlot = null;
            DergwasmRoot = world.GetRootSlot().FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
            if (DergwasmRoot == null)
            {
                UniLog.Log(
                    $"[Dergwasm] Couldn't find dergwasm slot with tag _dergwasm in world {world.GetName()}"
                );
                return;
            }

            // We expect a slot with the tag _dergwasm_wasm_file to exist, and to have
            // a StaticBinary component.
            WasmBinarySlot = DergwasmRoot.FindChild(
                s => s.Tag == "_dergwasm_wasm_file",
                maxDepth: 0
            );

            ConsoleSlot = DergwasmRoot.FindChild(s => s.Tag == "_dergwasm_console_content");
            FilesystemSlot = DergwasmRoot.FindChild(s => s.Tag == "_dergwasm_fs_root");
        }

        // Code adapted from BinaryExportable.Export.
        public async Task<string> GatherWasmBinary(IWorld world)
        {
            if (!Ready)
            {
                UniLog.Log(
                    $"[Dergwasm] World {world.GetName()} slots are not set up to gather WASM binary"
                );
                return null;
            }

            StaticBinary binary = WasmBinarySlot.GetComponent<StaticBinary>();
            if (binary == null)
            {
                UniLog.Log(
                    $"[Dergwasm] Couldn't access WASM StaticBinary component in world {world.GetName()}"
                );
                return null;
            }

            FileMetadata metadata = WasmBinarySlot.GetComponent<FileMetadata>();
            if (metadata != null)
                metadata.IsProcessing.Value = true;
            Uri url = binary.URL.Value;

            world.ToBackground();
            string filename = await world.GatherAssetFile(url, 100f);
            UniLog.Log($"[Dergwasm] Gathered binary asset file {filename}");
            world.ToWorld();

            if (metadata != null)
                metadata.IsProcessing.Value = false;
            return filename;
        }
    }
}
