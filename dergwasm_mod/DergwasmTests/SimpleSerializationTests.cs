using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Derg;
using Xunit;

namespace DergwasmTests
{
    public class SimpleSerializationTests
    {
        class TestEmscriptenEnv : EmscriptenEnv
        {
            public TestEmscriptenEnv()
                : base(new TestMachine()) { }

            public override int Malloc(Frame frame, int size)
            {
                return 4;
            }
        }

        [Fact]
        public void TestBoolSerializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            bool value = true;
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, env, null, value, out len);

            Assert.Equal(4, ptr);
            Assert.Equal(8, len);
            Assert.Equal(SimpleSerialization.SimpleType.Bool, env.machine.MemGet<int>(4));
            Assert.Equal(1, env.machine.MemGet<int>(8));
        }

        [Fact]
        public void TestStringSerializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            string value = "1234";
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, env, null, value, out len);

            Assert.Equal(4, ptr);
            Assert.Equal(9, len);
            Assert.Equal(SimpleSerialization.SimpleType.String, env.machine.MemGet<int>(4));
            Assert.Equal(0x34333231u, env.machine.MemGet<uint>(8));
            Assert.Equal(0, env.machine.Memory0[12]);
        }
    }
}
