using System.Linq;
using DergwasmTests.testing;
using Xunit;

namespace Derg.Modules
{
    [Mod("test")]
    public class TestModule
    {
        public int Got { get; private set; }
        public int Got2 { get; private set; }

        [ModFn("fn_1")]
        public void TestFunc(Frame frame, int num, int num2)
        {
            Got = num;
            Got2 = num2;
        }
    }

    public class ValueTests
    {
        [Fact]
        public void PopulateFunc()
        {
            var module = new TestModule();
            var reflected = new ReflectedModule<TestModule>(module);
            var method = reflected.Functions.First();
            Assert.Equal("test", method.ModuleName);
            Assert.Equal("fn_1", method.Name);

            var machine = new Machine();
            var frame = new Frame(null, new FakeModuleInstance(), null);
            frame.Push(5);
            frame.Push(34);
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.Got);
            Assert.Equal(34, module.Got2);
        }
    }
}
