using Derg;
using Derg.Wasm;
using DergwasmTests.testing;
using Elements.Core;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvSlotTests : TestMachine
    {
        FakeWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;
        TestComponent testComponent;
        FakeSlot testSlot;
        FakeSlot rootSlot;

        public ResoniteEnvSlotTests()
        {
            ResonitePatches.Apply();

            worldServices = new FakeWorldServices();
            emscriptenEnv = new TestEmscriptenEnv();
            env = new ResoniteEnv(this, worldServices, emscriptenEnv);
            SimpleSerialization.Initialize(env);
            frame = emscriptenEnv.EmptyFrame(null);

            testComponent = new TestComponent(worldServices);
            testComponent.Initialize();

            rootSlot = worldServices.GetRootSlot() as FakeSlot;
            testSlot = new FakeSlot(worldServices, "name", rootSlot);
        }

        [Fact]
        public void RootSlotTest()
        {
            Assert.Equal(rootSlot.ReferenceID, env.slot__root_slot(frame));
        }

        [Fact]
        public void GetParentTest()
        {
            Assert.Equal(
                rootSlot.ReferenceID,
                env.slot__get_parent(frame, new WasmRefID<ISlot>(testSlot))
            );
        }

        [Fact]
        public void GetParentFailsOnNonexistentRefID()
        {
            Assert.Equal(
                new RefID(0),
                env.slot__get_parent(frame, new WasmRefID<ISlot>(0xFFFFFFFFFFFFFFFFUL))
            );
        }

        [Fact]
        public void GetNameTest()
        {
            Ptr<byte> dataPtr = env.slot__get_name(frame, new WasmRefID<ISlot>(testSlot));
            Assert.Equal(testSlot.Name, emscriptenEnv.GetUTF8StringFromMem(dataPtr));
        }

        [Fact]
        public void SetNameTest()
        {
            Buff<byte> buff = emscriptenEnv.AllocateUTF8StringInMem(frame, "new name");
            env.slot__set_name(frame, new WasmRefID<ISlot>(testSlot), buff.Ptr);
            Assert.Equal("new name", testSlot.Name);
        }
    }
}
