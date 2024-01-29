using Derg;
using Derg.Wasm;
using DergwasmTests.testing;
using FrooxEngine;
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
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                0,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
            Assert.Equal(
                ResoniteEnv.ResoniteType.ValueInt,
                (ResoniteEnv.ResoniteType)HeapGet(outTypePtr)
            );
            Assert.Equal(testComponent.IntField.ReferenceID, HeapGet(outRefIdPtr));
        }

        [Fact]
        public void GetMemberFailsOnNonexistentRefID()
        {
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(0xFFFFFFFFFFFFFFFFUL),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNoncomponentRefID()
        {
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent.IntField.ReferenceID),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullNamePtr()
        {
            NullTerminatedString namePtr = new NullTerminatedString(0);
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullTypePtr()
        {
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<int> outTypePtr = new Ptr<int>(0);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullRefIdPtr()
        {
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(0);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNonexistentField()
        {
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "CatFace")
            );
            Ptr<int> outTypePtr = new Ptr<int>(namePtr.Data.Addr + 100);
            Ptr<ulong> outRefIdPtr = new Ptr<ulong>(outTypePtr.Addr + sizeof(int));

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    new WasmRefID<Component>(testComponent.IntField.ReferenceID),
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }
    }
}
