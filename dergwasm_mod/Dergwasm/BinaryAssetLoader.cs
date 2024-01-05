using Elements.Core; // For UniLog
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Derg;

namespace Derg
{
    public class BinaryAssetLoader
    {
        public StaticBinary Binary;
        public Engine Engine;
        public Slot ByteDisplay;
        public World world;

        public BinaryAssetLoader(World world, StaticBinary binary, Slot byteDisplay)
        {
            Engine = world.Engine;
            Binary = binary;
            ByteDisplay = byteDisplay;
            this.world = world;
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
            string file = await binaryAssetLoader.Engine.AssetManager
                .GatherAssetFile(url, 100f)
                .ConfigureAwait(false);
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
            ByteDisplay.GetComponent<TextRenderer>().Text.Value = $"Loaded to {file}";

            DergwasmMachine.Init(world, file);
            return file;
        }
    }
}
