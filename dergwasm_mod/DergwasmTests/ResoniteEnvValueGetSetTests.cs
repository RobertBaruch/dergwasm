using System.Reflection;
using Derg;
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
            Initialize(testComponent);
        }

        void Initialize(Component component)
        {
            component
                .GetType()
                .GetMethod("InitializeSyncMembers", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
            component
                .GetType()
                .GetMethod("OnAwake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
        }

        [Fact]
        public void GetValueUnsetIntIsDefaultedTest()
        {
            int dataPtr = 4;

            Assert.Equal(
                0,
                env.value__get<int>(frame, (ulong)testComponent.IntField.ReferenceID, dataPtr)
            );
            Assert.Equal(0, HeapGet<int>(dataPtr));
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
            Assert.Equal(1, HeapGet<int>(dataPtr));
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
            Assert.Equal(1, HeapGet<float>(dataPtr));
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
            Assert.Equal(1, HeapGet<double>(dataPtr));
        }

        [Fact]
        public void SetValueTest()
        {
            int dataPtr = 4;
            HeapSet<int>(dataPtr, 12);
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
