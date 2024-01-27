using System;
using System.Threading.Tasks;
using Elements.Core; // For UniLog
using FrooxEngine;

namespace Derg
{
    public class BinaryAssetLoader
    {
        public ISlot BinarySlot;
        public ISlot ByteDisplay;
        public IWorldServices worldServices;

        public BinaryAssetLoader(IWorldServices worldServices, ISlot binarySlot, ISlot byteDisplay)
        {
            BinarySlot = binarySlot;
            ByteDisplay = byteDisplay;
            this.worldServices = worldServices;
        }

        // Code adapted from BinaryExportable.Export.
        public async Task<string> Load()
        {
            BinaryAssetLoader binaryAssetLoader = this;
            StaticBinary binary = BinarySlot.GetComponent<StaticBinary>();
            if (binary == null)
            {
                DergwasmMachine.Msg(
                    $"Couldn't access WASM StaticBinary component in world {worldServices.GetName()}"
                );
                return null;
            }
            FileMetadata metadata = BinarySlot.GetComponent<FileMetadata>();
            if (metadata != null)
                metadata.IsProcessing.Value = true;
            Uri url = binary.URL.Value;

            worldServices.ToBackground();
            string filename = await worldServices.GatherAssetFile(url, 100f);
            UniLog.Log($"[Dergwasm] Gathered binary asset file {filename}");
            worldServices.ToWorld();

            if (metadata != null)
                metadata.IsProcessing.Value = false;
            if (ByteDisplay != null)
                ByteDisplay.GetComponent<TextRenderer>().Text.Value = $"Loaded to {filename}";

            DergwasmMachine.Init(worldServices, filename);
            return filename;
        }
    }
}
