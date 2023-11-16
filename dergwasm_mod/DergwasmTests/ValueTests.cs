using Derg;
using Xunit;

namespace DergwasmTests
{
    public class ValueTests
    {
        [Fact]
        public void TestI32_U()
        {
            Value v = new Value(1u);

            Assert.Equal(1u, v.U32);
        }

        [Fact]
        public void TestI32_S()
        {
            Value v = new Value(-1);

            Assert.Equal(-1, v.S32);
        }

        [Fact]
        public void TestI64_U()
        {
            Value v = new Value(1ul);

            Assert.Equal(1ul, v.U64);
        }

        [Fact]
        public void TestI64_S()
        {
            Value v = new Value(-1L);

            Assert.Equal(-1L, v.S64);
        }

        [Fact]
        public void TestF32()
        {
            Value v = new Value(1.0f);

            Assert.Equal(1.0f, v.F32);
        }

        [Fact]
        public void TestF64()
        {
            Value v = new Value(1.0);

            Assert.Equal(1.0, v.F64);
        }
    }
}
