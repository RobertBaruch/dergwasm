using System;
using Xunit;

namespace Derg.Modules
{
    public class TestModule {
        [ModFn]
        public void TestFunc(Frame frame, int num) {

        }
    }

    public class ValueTests
    {
        [Fact]
        public void PopulateFunc()
        {
            var module = new TestModule();
            var reflected = new ReflectedModule<TestModule>(module);
        }
    }
}
