using Derg;
using Xunit;

namespace DergwasmTests.micropython
{
    public class LoadingTests : MicropythonTestFramework
    {
        [Fact(Skip = "Recompile Micropython with correct host funcs before this test can pass")]
        public void DergwasmMachineInitTest()
        {
            Assert.True(DergwasmMachine.initialized);
        }
    }
}
