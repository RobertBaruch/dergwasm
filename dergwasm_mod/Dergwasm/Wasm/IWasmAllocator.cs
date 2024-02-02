using Derg.Runtime;

namespace Derg.Wasm
{
    public interface IWasmAllocator
    {
        Ptr Malloc(Frame frame, int size);
        void Free(Frame frame, Ptr buffer);
    }
}
