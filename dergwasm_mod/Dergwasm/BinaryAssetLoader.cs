using System;
using System.Threading.Tasks;
using Elements.Core; // For UniLog
using FrooxEngine;

namespace Derg
{
    public class BinaryAssetLoader
    {
        public StaticBinary Binary;
        public ISlot ByteDisplay;
        public IWorldServices worldServices;

        public BinaryAssetLoader(
            IWorldServices worldServices,
            StaticBinary binary,
            ISlot byteDisplay
        )
        {
            Binary = binary;
            ByteDisplay = byteDisplay;
            this.worldServices = worldServices;
        }

        // Code adapted from BinaryExportable.Export.
        public async Task<string> Load()
        {
            BinaryAssetLoader binaryAssetLoader = this;
            //if (exportType < 0 || exportType >= 1)
            //    throw new ArgumentOutOfRangeException(nameof(exportType));
            FileMetadata metadata = Binary?.Slot.GetComponent<FileMetadata>();
            if (metadata != null)
                metadata.IsProcessing.Value = true;
            Uri url = Binary.URL.Value;
            await new ToBackground();
            string file = await worldServices.GatherAssetFile(url, 100f).ConfigureAwait(false);
            UniLog.Log($"[Dergwasm] Gathered binary asset file {file}");
            //if (file != null)
            //{
            //    string str = Path.Combine(folder, name);
            //    File.Copy(file, str, true);
            //    File.SetAttributes(str, FileAttributes.Normal);
            //}
            await new ToWorld();
            if (metadata != null)
                metadata.IsProcessing.Value = false;
            if (ByteDisplay != null)
                ByteDisplay.GetComponent<TextRenderer>().Text.Value = $"Loaded to {file}";

            DergwasmMachine.Init(worldServices, file);
            return file;
        }
    }
}
