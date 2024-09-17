using System.Collections.Generic;
using Derg;
using Derg.Wasm;
using DergwasmTests.testing;
using Elements.Core;
using Xunit;

namespace DergwasmTests
{
    public class SimpleSerializationTests
    {
        TestEmscriptenEnv env;
        ResoniteEnv resoniteEnv;

        public SimpleSerializationTests()
        {
            env = new TestEmscriptenEnv();
            resoniteEnv = new ResoniteEnv(env.machine, null, env);
            SimpleSerialization.Initialize(resoniteEnv);
        }

        [Fact]
        public void TestBoolSerializesCorrectly()
        {
            bool value = true;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            Assert.Equal(4, ptr);
            Assert.Equal(SimpleSerialization.SimpleType.Bool, env.machine.HeapGet(new Ptr<int>(4)));
            Assert.Equal(1, env.machine.HeapGet(new Ptr<int>(8)));
        }

        [Fact]
        public void TestNullSerializesCorrectly()
        {
            string value = null;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            Assert.Equal(4, ptr);
            Assert.Equal(
                SimpleSerialization.SimpleType.Null,
                env.machine.HeapGet<int>(new Ptr<int>(4))
            );
        }

        [Fact]
        public void TestStringSerializesCorrectly()
        {
            string value = "1234";
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            Assert.Equal(4, ptr);
            Assert.Equal(
                SimpleSerialization.SimpleType.String,
                env.machine.HeapGet(new Ptr<int>(4))
            );
            var stringPtr = env.machine.HeapGet(new Ptr<PrefixBuff>(8));
            Assert.Equal(4, env.machine.HeapGet(stringPtr.Length));
            Assert.Equal(
                0x34333231u,
                env.machine.HeapGet(stringPtr.BufferStart.Reinterpret<uint>())
            );
        }

        [Fact]
        public void TestBoolDeserializesCorrectly()
        {
            bool value = true;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<bool>(deserialized);
            Assert.True((bool)deserialized);
        }

        [Fact]
        public void TestNullDeserializesCorrectly()
        {
            string value = null;
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.Null(deserialized);
        }

        [Fact]
        public void TestStringDeserializesCorrectly()
        {
            string value = "1234";
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<string>(deserialized);
            Assert.Equal("1234", (string)deserialized);
        }

        [Fact]
        public void TestRefIDListDeserializesCorrectly()
        {
            List<RefID> value = new List<RefID> { new RefID(100), new RefID(102), new RefID(90) };
            int ptr = SimpleSerialization.Serialize(env.machine, resoniteEnv, null, value);

            object deserialized = SimpleSerialization.Deserialize(env.machine, resoniteEnv, ptr);

            Assert.IsAssignableFrom<List<RefID>>(deserialized);
            Assert.Equal(value, (List<RefID>)deserialized);
        }
    }
}
