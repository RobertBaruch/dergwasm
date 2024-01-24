using System.Collections.Generic;
using Derg;
using Elements.Core;
using Xunit;

namespace DergwasmTests
{
    public class SimpleSerializationTests
    {
        [Fact]
        public void TestBoolSerializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            bool value = true;
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            Assert.Equal(4, ptr);
            Assert.Equal(8, len);
            Assert.Equal(SimpleSerialization.SimpleType.Bool, env.machine.HeapGet<int>(4));
            Assert.Equal(1, env.machine.HeapGet<int>(8));
        }

        [Fact]
        public void TestNullSerializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            string value = null;
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            Assert.Equal(4, ptr);
            Assert.Equal(4, len);
            Assert.Equal(SimpleSerialization.SimpleType.Null, env.machine.HeapGet<int>(4));
        }

        [Fact]
        public void TestStringSerializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            string value = "1234";
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            Assert.Equal(4, ptr);
            Assert.Equal(12, len);
            Assert.Equal(SimpleSerialization.SimpleType.String, env.machine.HeapGet<int>(4));
            Assert.Equal(4, env.machine.HeapGet<int>(8));
            Assert.Equal(0x34333231u, env.machine.HeapGet<uint>(12));
        }

        [Fact]
        public void TestBoolDeserializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            bool value = true;
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<bool>(deserialized);
            Assert.True((bool)deserialized);
        }

        [Fact]
        public void TestNullDeserializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            string value = null;
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.Null(deserialized);
        }

        [Fact]
        public void TestStringDeserializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            string value = "1234";
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<string>(deserialized);
            Assert.Equal("1234", (string)deserialized);
        }

        [Fact]
        public void TestRefIDListDeserializesCorrectly()
        {
            TestEmscriptenEnv env = new TestEmscriptenEnv();
            ResoniteEnv resoniteEnv = new ResoniteEnv(env.machine, null, env);
            List<RefID> value = new List<RefID> { new RefID(100), new RefID(102), new RefID(90) };
            int len;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value, out len);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<List<RefID>>(deserialized);
            Assert.Equal(value, (List<RefID>)deserialized);
        }
    }
}
