using System.Linq;
using Dergwasm.Wasm;
using Dergwasm.Runtime;
using DergwasmTests.testing;
using FrooxEngine;
using Xunit;

namespace Dergwasm.Modules
{
    [Mod("test")]
    public class TestModule : ReflectedModule
    {
        public int Got { get; private set; }
        public int Got2 { get; private set; }
        public WasmRefID<Slot> SlotArg { get; private set; }
        public Ptr<byte> BytePtrArg { get; private set; }
        public bool ReceivedBoolArg { get; private set; }
        public Buff<byte> ReceivedBuffArg { get; private set; }

        public NullTerminatedString StringArg { get; private set; }

        [ModFn("fn_1")]
        public void TestFunc(Frame frame, int num, int num2)
        {
            Got = num;
            Got2 = num2;
        }

        [ModFn("refid_arg")]
        public void RefIdArg(Frame frame, WasmRefID<Slot> slot)
        {
            SlotArg = slot;
        }

        [ModFn("ptr_arg")]
        public void PtrArg(Frame frame, Ptr<byte> ptr)
        {
            BytePtrArg = ptr;
        }

        [ModFn("buff_arg")]
        public void BuffArg(Frame frame, Buff<byte> buff)
        {
            ReceivedBuffArg = buff;
        }

        [ModFn("string_arg")]
        public void NullTerminatedStringArg(Frame frame, NullTerminatedString s)
        {
            StringArg = s;
        }

        [ModFn("bool_arg")]
        public void BoolArg(Frame frame, bool b)
        {
            ReceivedBoolArg = b;
        }

        [ModFn("refid_return")]
        public WasmRefID<Slot> RefIdReturn(Frame frame)
        {
            return new WasmRefID<Slot>(5UL);
        }

        [ModFn("ptr_return")]
        public Ptr<byte> PtrReturn(Frame frame)
        {
            return new Ptr<byte>(5);
        }

        [ModFn("string_return")]
        public NullTerminatedString StringReturn(Frame frame)
        {
            return new NullTerminatedString(5);
        }

        [ModFn("bool_return")]
        public bool BoolReturn(Frame frame)
        {
            return true;
        }
    }

    public class ValueTests
    {
        TestModule module;
        Machine machine;
        Frame frame;

        public ValueTests()
        {
            module = new TestModule();
            machine = new Machine();
            frame = new Frame(null, new FakeModuleInstance(), null);
        }

        [Fact]
        public void PopulateFunc()
        {
            var method = module.Functions.First();
            Assert.Equal("test", method.ModuleName);
            Assert.Equal("fn_1", method.Name);

            var apiData = module.ApiData.First();
            Assert.Equal("test", apiData.Module);
            Assert.Equal("fn_1", apiData.Name);
            Assert.Collection(
                apiData.Parameters,
                p => Assert.Equal("num", p.Name),
                p => Assert.Equal("num2", p.Name)
            );
            Assert.Collection(
                apiData.Parameters,
                p => Assert.Equal(new ValueType[] { ValueType.I32 }, p.Types),
                p => Assert.Equal(new ValueType[] { ValueType.I32 }, p.Types)
            );
            Assert.Collection(
                apiData.Parameters,
                p => Assert.Equal("int", p.CSType),
                p => Assert.Equal("int", p.CSType)
            );
            Assert.Empty(apiData.Returns);

            frame.Push(5);
            frame.Push(34);
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.Got);
            Assert.Equal(34, module.Got2);
        }

        [Fact]
        public void RefIdArgApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("refid_arg");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("refid_arg", apiData.Name);
            Assert.Equal("WasmRefID<Slot>", Assert.Single(apiData.Parameters).CSType);
            Assert.Equal(
                new ValueType[] { ValueType.I64 },
                Assert.Single(apiData.Parameters).Types
            );
            Assert.Empty(apiData.Returns);
        }

        [Fact]
        public void RefIdArgPassedCorrectly()
        {
            var method = module.GetHostFunc("refid_arg");
            frame.Push(new WasmRefID<Slot>(5UL));
            frame.InvokeFunc(machine, method);
            Assert.Equal(5UL, module.SlotArg.Id);
        }

        [Fact]
        public void PtrArgApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("ptr_arg");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("ptr_arg", apiData.Name);
            Assert.Equal("Ptr<byte>", Assert.Single(apiData.Parameters).CSType);
            Assert.Equal(
                new ValueType[] { ValueType.I32 },
                Assert.Single(apiData.Parameters).Types
            );
            Assert.Empty(apiData.Returns);
        }

        [Fact]
        public void PtrArgPassedCorrectly()
        {
            var method = module.GetHostFunc("ptr_arg");
            frame.Push(new Ptr<byte>(5));
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.BytePtrArg.Addr);
        }

        [Fact]
        public void BuffArgApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("buff_arg");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("buff_arg", apiData.Name);
            Assert.Equal("Buff<byte>", Assert.Single(apiData.Parameters).CSType);
            Assert.Equal(
                new ValueType[] { ValueType.I32, ValueType.I32 },
                Assert.Single(apiData.Parameters).Types
            );
            Assert.Empty(apiData.Returns);
        }

        [Fact]
        public void BuffArgPassedCorrectly()
        {
            var method = module.GetHostFunc("buff_arg");
            new BuffMarshaller<byte>().To(frame, machine, new Buff<byte>(new Ptr<byte>(5), 10));
            frame.InvokeFunc(machine, method);
            Assert.Equal(10, module.ReceivedBuffArg.Length);
            Assert.Equal(5, module.ReceivedBuffArg.Ptr.Addr);
        }

        [Fact]
        public void StringArgApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("string_arg");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("string_arg", apiData.Name);
            Assert.Equal("NullTerminatedString", Assert.Single(apiData.Parameters).CSType);
            Assert.Equal(
                new ValueType[] { ValueType.I32 },
                Assert.Single(apiData.Parameters).Types
            );
            Assert.Empty(apiData.Returns);
        }

        [Fact]
        public void StringArgPassedCorrectly()
        {
            var method = module.GetHostFunc("string_arg");
            frame.Push(new NullTerminatedString(5));
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, module.StringArg.Data.Addr);
        }

        [Fact]
        public void BoolArgApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("bool_arg");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("bool_arg", apiData.Name);
            Assert.Equal("bool", Assert.Single(apiData.Parameters).CSType);
            Assert.Equal(
                new ValueType[] { ValueType.I32 },
                Assert.Single(apiData.Parameters).Types
            );
            Assert.Empty(apiData.Returns);
        }

        [Fact]
        public void BoolArgPassedCorrectly()
        {
            var method = module.GetHostFunc("bool_arg");
            frame.Push(true);
            frame.InvokeFunc(machine, method);
            Assert.True(module.ReceivedBoolArg);
        }

        [Fact]
        public void RefIdReturnApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("refid_return");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("refid_return", apiData.Name);
            Assert.Empty(apiData.Parameters);
            Assert.Equal("WasmRefID<Slot>", Assert.Single(apiData.Returns).CSType);
            Assert.Equal(new ValueType[] { ValueType.I64 }, Assert.Single(apiData.Returns).Types);
        }

        [Fact]
        public void RefIdReturnPassedCorrectly()
        {
            var method = module.GetHostFunc("refid_return");
            frame.InvokeFunc(machine, method);
            Assert.Equal(5UL, frame.Pop<WasmRefID<Slot>>().Id);
        }

        [Fact]
        public void PtrReturnApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("ptr_return");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("ptr_return", apiData.Name);
            Assert.Empty(apiData.Parameters);
            Assert.Equal("Ptr<byte>", Assert.Single(apiData.Returns).CSType);
            Assert.Equal(new ValueType[] { ValueType.I32 }, Assert.Single(apiData.Returns).Types);
        }

        [Fact]
        public void PtrReturnPassedCorrectly()
        {
            var method = module.GetHostFunc("ptr_return");
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, frame.Pop<Ptr<byte>>().Addr);
        }

        [Fact]
        public void StringReturnApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("string_return");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("string_return", apiData.Name);
            Assert.Empty(apiData.Parameters);
            Assert.Equal("NullTerminatedString", Assert.Single(apiData.Returns).CSType);
            Assert.Equal(new ValueType[] { ValueType.I32 }, Assert.Single(apiData.Returns).Types);
        }

        [Fact]
        public void StringReturnPassedCorrectly()
        {
            var method = module.GetHostFunc("string_return");
            frame.InvokeFunc(machine, method);
            Assert.Equal(5, frame.Pop<NullTerminatedString>().Data.Addr);
        }

        [Fact]
        public void BoolReturnApiDataIsCorrect()
        {
            var apiData = module.GetApiFunc("bool_return");
            Assert.Equal("test", apiData.Module);
            Assert.Equal("bool_return", apiData.Name);
            Assert.Empty(apiData.Parameters);
            Assert.Equal("bool", Assert.Single(apiData.Returns).CSType);
            Assert.Equal(new ValueType[] { ValueType.I32 }, Assert.Single(apiData.Returns).Types);
        }

        [Fact]
        public void BoolReturnPassedCorrectly()
        {
            var method = module.GetHostFunc("bool_return");
            frame.InvokeFunc(machine, method);
            Assert.True(frame.Pop<bool>());
        }
    }
}
