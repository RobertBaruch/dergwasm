using Derg;
using Xunit;

namespace DergwasmTests
{
    public class ValueTests
    {
        [Fact]
        public void TestI32_U()
        {
            Value v = new Value { u32 = 1u };

            Assert.Equal(1u, v.u32);
            Assert.Equal(0u, v.value_hi);
        }

        [Fact]
        public void TestI32_S()
        {
            Value v = new Value { s32 = -1 };

            Assert.Equal(-1, v.s32);
            Assert.Equal(0u, v.value_hi);
        }

        [Fact]
        public void TestI64_U()
        {
            Value v = new Value { u64 = 1ul };

            Assert.Equal(1ul, v.u64);
            Assert.Equal(0u, v.value_hi);
        }

        [Fact]
        public void TestI64_S()
        {
            Value v = new Value { s64 = -1L };

            Assert.Equal(-1L, v.s64);
            Assert.Equal(0u, v.value_hi);
        }

        [Fact]
        public void TestF32()
        {
            Value v = new Value { f32 = 1.0f };

            Assert.Equal(1.0f, v.f32);
            Assert.Equal(0u, v.value_hi);
        }

        [Fact]
        public void TestF64()
        {
            Value v = new Value { f64 = 1.0 };

            Assert.Equal(1.0, v.f64);
            Assert.Equal(0u, v.value_hi);
        }
    }
}
