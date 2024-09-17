using System.Threading.Tasks;

namespace Derg
{
    public class BinaryAssetLoader
    {
        public IWorldServices worldServices;
        public IDergwasmSlots dergwasmSlots;

        public BinaryAssetLoader(IWorldServices worldServices, IDergwasmSlots dergwasmSlots)
        {
            this.worldServices = worldServices;
            this.dergwasmSlots = dergwasmSlots;
        }

        public async Task<string> Load()
        {
            string filename = await dergwasmSlots.GatherWasmBinary(worldServices);
            DergwasmMachine.Init(worldServices, filename);
            return filename;
        }
    }
}
