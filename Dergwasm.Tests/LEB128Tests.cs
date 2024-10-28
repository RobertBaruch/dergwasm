using System.IO;
using Dergwasm.Instructions;
using Xunit;

namespace DergwasmTests
{
    public class LEB128Tests
    {
        [Theory]
        [InlineData(0UL, (byte)0x00)]
        [InlineData(1UL, (byte)0x01)]
        [InlineData(127UL, (byte)0x7f)]
        [InlineData(128UL, (byte)0x80, (byte)0x01)]
        [InlineData(129UL, (byte)0x81, (byte)0x01)]
        [InlineData(255UL, (byte)0xff, (byte)0x01)]
        [InlineData(256UL, (byte)0x80, (byte)0x02)]
        [InlineData(257UL, (byte)0x81, (byte)0x02)]
        [InlineData(383UL, (byte)0xff, (byte)0x02)]
        [InlineData(384UL, (byte)0x80, (byte)0x03)]
        [InlineData(511UL, (byte)0xff, (byte)0x03)]
        [InlineData(512UL, (byte)0x80, (byte)0x04)]
        [InlineData(624485, (byte)0xe5, (byte)0x8e, (byte)0x26)] // test value from wikipedia :)
        [InlineData(0x7fffffffUL, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0x07)]
        [InlineData(
            ulong.MaxValue,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0x01
        )]
        public void TestLEB128Unsigned(ulong expected, params byte[] bytes)
        {
            var ms = new MemoryStream();
            ms.WriteLEB128Unsigned(expected);
            ms.Position = 0;
            Assert.Equal(expected, ms.ReadLEB128Unsigned());
            AssertStreamBytesEqual(ms, bytes);
        }

        [Theory]
        [InlineData(0L, (byte)0x00)]
        [InlineData(1L, (byte)0x01)]
        [InlineData(2L, (byte)0x02)]
        [InlineData(62L, (byte)0x3e)]
        [InlineData(63L, (byte)0x3f)]
        [InlineData(64L, (byte)0xc0, (byte)0x00)]
        [InlineData(65L, (byte)0xc1, (byte)0x00)]
        [InlineData(66L, (byte)0xc2, (byte)0x00)]
        [InlineData(127L, (byte)0xff, (byte)0x00)]
        [InlineData(128L, (byte)0x80, (byte)0x01)]
        [InlineData(129L, (byte)0x81, (byte)0x01)]
        [InlineData(-1L, (byte)0x7f)]
        [InlineData(-2L, (byte)0x7e)]
        [InlineData(-62L, (byte)0x42)]
        [InlineData(-63L, (byte)0x41)]
        [InlineData(-64L, (byte)0x40)]
        [InlineData(-65L, (byte)0xbf, (byte)0x7f)]
        [InlineData(-66L, (byte)0xbe, (byte)0x7f)]
        [InlineData(-127L, (byte)0x81, (byte)0x7f)]
        [InlineData(-128L, (byte)0x80, (byte)0x7f)]
        [InlineData(-129L, (byte)0xff, (byte)0x7e)]
        [InlineData(-123456L, (byte)0xc0, (byte)0xbb, (byte)0x78)] // test value from wikipedia :)
        [InlineData(
            long.MinValue,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x80,
            (byte)0x7f
        )]
        [InlineData(
            long.MaxValue,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0xff,
            (byte)0x00
        )]
        public void TestLEB128Signed(long value, params byte[] bytes)
        {
            var ms = new MemoryStream();
            ms.WriteLEB128Signed(value);
            ms.Position = 0;
            Assert.Equal(value, ms.ReadLEB128Signed());
            AssertStreamBytesEqual(ms, bytes);
        }

        [Fact]
        public void TestRoundTripThroughBinaryWriter()
        {
            var ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            writer.WriteLEB128Unsigned(4UL);
            ms.Position = 0;
            Assert.Equal(4UL, ms.ReadLEB128Unsigned());
        }

        [Fact]
        public void TestRoundTripThroughBinaryReader()
        {
            var ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            writer.WriteLEB128Unsigned(4UL);
            ms.Position = 0;
            BinaryReader reader = new BinaryReader(ms);
            Assert.Equal(4UL, reader.ReadLEB128Unsigned());
        }

        private void AssertStreamBytesEqual(MemoryStream ms, params byte[] values)
        {
            ms.Position = 0;
            var buf = ms.ToArray();
            Assert.Equal(buf, values);
        }
    }
}
