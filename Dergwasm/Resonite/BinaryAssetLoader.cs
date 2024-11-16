using System.Threading.Tasks;

namespace Dergwasm.Resonite
{
    public class BinaryAssetLoader
    {
        public IWorld world;
        public IDergwasmSlots dergwasmSlots;

        public BinaryAssetLoader(IWorld world, IDergwasmSlots dergwasmSlots)
        {
            this.world = world;
            this.dergwasmSlots = dergwasmSlots;
        }

        public async Task<string> Load()
        {
            string filename = await dergwasmSlots.GatherWasmBinary(world);
            DergwasmMachine.Init(world, filename);
            return filename;
        }
    }
}
