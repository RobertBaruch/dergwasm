namespace Derg.Wasm
{
    public interface IWasmAllocator
    {
        Pointer Malloc(Frame frame, int size);
        void Free(Frame frame, Pointer buffer);
    }
}
