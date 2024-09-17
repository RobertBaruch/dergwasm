using System.Threading.Tasks;
using FrooxEngine;

namespace Derg
{
    public interface IDergwasmSlots
    {
        Slot DergwasmRoot { get; }
        Slot WasmBinarySlot { get; }

        Slot ConsoleSlot { get; }
        Slot FilesystemSlot { get; }

        bool Ready { get; }

        Task<string> GatherWasmBinary(IWorldServices worldServices);
    }
}
