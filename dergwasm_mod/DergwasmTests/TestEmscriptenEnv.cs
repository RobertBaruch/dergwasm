using Derg;

namespace DergwasmTests
{
    public class TestEmscriptenEnv : EmscriptenEnv
    {
        int ptr = 4;

        public TestEmscriptenEnv()
            : base(new TestMachine())
        {
            machine.Allocator = this;
        }

        public void ResetMalloc()
        {
            ptr = 4;
        }

        public override int Malloc(Frame frame, int size)
        {
            // C malloc: "If the size of the space requested is zero, the behavior
            // is implementation-defined: either a null pointer is returned, or the
            // behavior is as if the size were some nonzero value, except that the
            // returned pointer shall not be used to access an object."
            if (size == 0)
            {
                return 0;
            }
            int result = ptr;
            ptr += size;
            return result;
        }

        public override void Free(Frame frame, int ptr) { }
    }
}
