using System.Reflection;
using Derg;
using Derg.Wasm;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvValueGetSetTests : TestMachine
    {
        FakeWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;
        TestComponent testComponent;

        public ResoniteEnvValueGetSetTests()
        {
            ResonitePatches.Apply();
            worldServices = new FakeWorldServices();
            emscriptenEnv = new TestEmscriptenEnv();
            env = new ResoniteEnv(this, worldServices, emscriptenEnv);
            SimpleSerialization.Initialize(env);
            frame = emscriptenEnv.EmptyFrame(null);

            testComponent = new TestComponent(worldServices);
            testComponent.Initialize();
        }

        [Fact]
        public void GetValueUnsetIntIsDefaultedTest()
        {
            int dataPtr = 4;

            Assert.Equal(
                0,
                env.value__get<int>(frame, (ulong)testComponent.IntField.ReferenceID, dataPtr)
            );
            Assert.Equal(0, HeapGet(new Ptr<int>(dataPtr)));
        }

        [Fact]
        public void GetValueIntTest()
        {
            testComponent.IntField.Value = 1;
            int dataPtr = 4;

            Assert.Equal(
                0,
                env.value__get<int>(frame, (ulong)testComponent.IntField.ReferenceID, dataPtr)
            );
            Assert.Equal(1, HeapGet(new Ptr<int>(dataPtr)));
        }

        [Fact]
        public void GetValueFailsOnNonexistentRefID()
        {
            Assert.Equal(-1, env.value__get<int>(frame, 0xFFFFFFFFFFFFFFFFUL, 4));
        }

        [Fact]
        public void GetValueFailsOnWrongType()
        {
            Assert.Equal(
                -1,
                env.value__get<double>(frame, (ulong)testComponent.IntField.ReferenceID, 4)
            );
        }

        [Fact]
        public void GetValueFailsOnNullDataPtr()
        {
            Assert.Equal(
                -1,
                env.value__get<int>(frame, (ulong)testComponent.IntField.ReferenceID, 0)
            );
        }

        [Fact]
        public void GetValueFloatTest()
        {
            testComponent.FloatField.Value = 1;
            int dataPtr = 4;

            Assert.Equal(
                0,
                env.value__get<float>(frame, (ulong)testComponent.FloatField.ReferenceID, dataPtr)
            );
            Assert.Equal(1, HeapGet(new Ptr<float>(dataPtr)));
        }

        [Fact]
        public void GetValueDoubleTest()
        {
            testComponent.DoubleField.Value = 1;
            int dataPtr = 4;

            Assert.Equal(
                0,
                env.value__get<double>(frame, (ulong)testComponent.DoubleField.ReferenceID, dataPtr)
            );
            Assert.Equal(1, HeapGet(new Ptr<double>(dataPtr)));
        }

        [Fact]
        public void SetValueTest()
        {
            int dataPtr = 4;
            HeapSet(new Ptr<int>(dataPtr), 12);
            Assert.Equal(
                0,
                env.value__set<int>(frame, (ulong)testComponent.IntField.ReferenceID, dataPtr)
            );
            Assert.Equal(12, testComponent.IntField.Value);
        }

        [Fact]
        public void SetValueFailsOnNonexistentRefID()
        {
            Assert.Equal(-1, env.value__set<int>(frame, 0xFFFFFFFFFFFFFFFFUL, 4));
        }

        [Fact]
        public void SetValueFailsOnWrongType()
        {
            Assert.Equal(
                -1,
                env.value__set<double>(frame, (ulong)testComponent.IntField.ReferenceID, 4)
            );
        }

        [Fact]
        public void SetValueFailsOnNullDataPtr()
        {
            Assert.Equal(
                -1,
                env.value__set<int>(frame, (ulong)testComponent.IntField.ReferenceID, 0)
            );
        }
    }
}
