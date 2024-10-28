using Dergwasm.Runtime;
using Dergwasm.Environments;

namespace DergwasmTests.testing
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
            int result = ptr;
            ptr += size;
            return result;
        }
    }
}
