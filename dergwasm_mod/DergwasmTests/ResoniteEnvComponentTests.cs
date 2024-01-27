using Derg;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvComponentTests : TestMachine
    {
        FakeWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;
        TestComponent testComponent;

        public ResoniteEnvComponentTests()
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
        public void GetMemberTest()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                0,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
            Assert.Equal(
                ResoniteEnv.ResoniteType.ValueInt,
                (ResoniteEnv.ResoniteType)HeapGet<int>(outTypePtr)
            );
            Assert.Equal(testComponent.IntField.ReferenceID, HeapGet<ulong>(outRefIdPtr));
        }

        [Fact]
        public void GetMemberFailsOnNonexistentRefID()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    0xFFFFFFFFFFFFFFFFUL,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNoncomponentRefID()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.IntField.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullNamePtr()
        {
            int namePtr = 0;
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullTypePtr()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = 0;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullRefIdPtr()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = 0;

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNonexistentField()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "CatFace");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.IntField.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }
    }
}
