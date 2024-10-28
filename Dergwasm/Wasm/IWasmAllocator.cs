using Dergwasm.Runtime;

namespace Dergwasm.Wasm
{
    public interface IWasmAllocator
    {
        Ptr Malloc(Frame frame, int size);
        void Free(Frame frame, Ptr buffer);
    }
}
