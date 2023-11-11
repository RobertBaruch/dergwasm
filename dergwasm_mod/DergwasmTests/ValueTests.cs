using Xunit;

namespace Derg
{
    public class ValueTests
    {
        [Fact]
        public void TestI32_U()
        {
            Value v = new Value(1u);

            Assert.Equal(1u, v.AsI32_U());
        }

        [Fact]
        public void TestI32_S()
        {
            Value v = new Value(-1);

            Assert.Equal(-1, v.AsI32_S());
        }

        [Fact]
        public void TestI64_U()
        {
            Value v = new Value(1ul);

            Assert.Equal(1ul, v.AsI64_U());
        }

        [Fact]
        public void TestI64_S()
        {
            Value v = new Value(-1L);

            Assert.Equal(-1L, v.AsI64_S());
        }

        [Fact]
        public void TestF32()
        {
            Value v = new Value(1.0f);

            Assert.Equal(1.0f, v.AsF32());
        }

        [Fact]
        public void TestF64()
        {
            Value v = new Value(1.0);

            Assert.Equal(1.0, v.AsF64());
        }
    }
}
