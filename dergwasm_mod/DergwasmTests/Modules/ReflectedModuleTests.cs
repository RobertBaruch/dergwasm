using System.Linq;
using Derg.Wasm;
using DergwasmTests.testing;
using Xunit;

namespace Derg.Modules
{
    [Mod("test")]
    public class TestModule
    {
        public int Got { get; private set; }
        public int Got2 { get; private set; }
        public WasmRefID<ISlot> SlotArg { get; private set; }
        public Ptr<byte> BytePtrArg { get; private set; }

        [ModFn("fn_1")]
        public void TestFunc(Frame frame, int num, int num2)
        {
            Got = num;
            Got2 = num2;
        }

        [ModFn("refid_arg")]
        public void RefIdArg(Frame frame, WasmRefID<ISlot> slot)
        {
            SlotArg = slot;
        }

        [ModFn("ptr_arg")]
        public void PtrArg(Frame frame, Ptr<byte> ptr)
        {
            BytePtrArg = ptr;
        }

        [ModFn("refid_return")]
        public WasmRefID<ISlot> RefIdReturn(Frame frame)
        {
            return new WasmRefID<ISlot>(5UL);
        }

        [ModFn("ptr_return")]
        public Ptr<byte> PtrReturn(Frame frame)
        {
            return new Ptr<byte>(5);
        }
    }

    public class ValueTests
    {
        TestModule module;
        ReflectedModule<TestModule> reflected;
        Machine machine;
        Frame frame;

        public ValueTests()
        {
            module = new TestModule();
            reflected = new ReflectedModule<TestModule>(module);
            machine = new Machine();
            frame = new Frame(null, new FakeModuleInstance(), null);
        }

        [Fact]
        public void PopulateFunc()
        {
            var method = reflected.Functions.First();
            Assert.Equal("test", method.ModuleName);
            Assert.Equal("fn_1", method.Name);

            frame.Push(5);
            frame.Push(34);
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.Got);
            Assert.Equal(34, module.Got2);
        }

        [Fact]
        public void RefIdArgPassedCorrectly()
        {
            var method = reflected["refid_arg"];
            frame.Push(5UL);
            frame.InvokeFunc(machine, method);
            Assert.Equal(5UL, module.SlotArg.Id);
        }

        [Fact]
        public void PtrArgPassedCorrectly()
        {
            var method = reflected["ptr_arg"];
            frame.Push(5);
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.BytePtrArg.Addr);
        }

        [Fact]
        public void RefIdReturnPassedCorrectly()
        {
            var method = reflected["refid_return"];
            frame.InvokeFunc(machine, method);
            Assert.Equal(5UL, frame.Pop<WasmRefID<ISlot>>().Id);
        }

        [Fact]
        public void PtrReturnPassedCorrectly()
        {
            var method = reflected["ptr_return"];
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, frame.Pop<Ptr<byte>>().Addr);
        }
    }
}
