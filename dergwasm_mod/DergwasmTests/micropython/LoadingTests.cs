using Derg;
using Xunit;

namespace DergwasmTests.micropython
{
    public class LoadingTests : MicropythonTestFramework
    {
        [Fact]
        public void DergwasmMachineInitTest()
        {
            Assert.True(DergwasmMachine.initialized);
        }
    }
}
