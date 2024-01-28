using System.Collections.Generic;
using System.Linq;
using Derg;

namespace DergwasmTests
{
    // A Machine for testing. Implements a real frame stack, but all other runtime structures
    // are just dictionaries.
    //
    // You can add functions, tables, element segments, and globals to the machine using the Add* methods,
    // specifying the address to put them in.
    public class TestMachine : Machine
    {
        public const int VoidType = 0;
        public const int OneArgType = 1;
        public const int OneReturnType = 2;
        public const int TwoArgTwoReturnType = 3;
        public FakeModuleInstance FakeModuleInstance = new FakeModuleInstance();
        public Frame programFrame;

        // Allocates enough space for 2 globals, 500 funcs, tables, element segments, and data segments,
        // and one memory (with one page).
        public TestMachine()
        {
            Globals.Add(new Value { s32 = 0 });
            Globals.Add(new Value { s32 = 0 });

            funcs = new List<Func>(new Func[500]);
            tables = new List<Table>(new Table[500]);
            elementSegments = new List<ElementSegment>(new ElementSegment[500]);
            memories = new List<Memory> { new Memory(new Limits(1, 1)) };
            dataSegments = new List<byte[]>(new byte[500][]);
        }

        public Frame CreateFrame()
        {
            return new Frame(null, FakeModuleInstance, null);
        }

        public void Step(int n = 1)
        {
            programFrame.Step(this, n);
        }

        public Frame Frame => programFrame;

        // Sets the program up for execution, with a signature given by the signature_idx (see GetFuncTypeFromIndex).
        // The program always has two I32 locals.
        public void SetProgram(int signature_idx, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<Instruction>();
            new List<UnflattenedInstruction>(instructions).Flatten(program);

            programFrame = CreateFrame();
            ModuleFunc func = new ModuleFunc(
                "test",
                "$0",
                programFrame.GetFuncTypeForIndex(signature_idx)
            );
            func.Locals = new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 };
            func.Code = program;
            programFrame.Func = func;
            programFrame.Locals = new Value[func.Signature.args.Length + func.Locals.Length];
            programFrame.Label = new Label(programFrame.Arity, program.Count);
        }

        // Sets the function at the given addr. The index is also used to determine the function's
        // signature (see GetFuncTypeFromIndex). The function has two I32 locals.
        public void SetFuncAt(int addr, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<Instruction>();
            new List<UnflattenedInstruction>(instructions).Flatten(program);
            ModuleFunc func = new ModuleFunc(
                "test",
                $"${addr - 10}",
                programFrame.GetFuncTypeForIndex(addr - 10)
            );
            func.Locals = new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 };
            func.Code = program;
            funcs[addr] = func;
        }

        // Sets a host function at the given addr. The index is also used to determine the function's
        // signature (see GetFuncTypeFromIndex).
        public void SetHostFuncAt(int addr, HostProxy proxy)
        {
            funcs[addr] = new HostFunc(
                "test",
                $"${addr - 10}",
                programFrame.GetFuncTypeForIndex(addr - 10),
                proxy
            );
        }

        public UnflattenedInstruction Insn(InstructionType type, params Value[] operands)
        {
            return new UnflattenedInstruction(
                type,
                (from operand in operands select new UnflattenedOperand(operand)).ToArray()
            );
        }

        public void SetTableAt(int addr, Table table)
        {
            tables[addr] = table;
        }

        public void SetElementSegmentAt(int addr, ElementSegment elementSegment)
        {
            elementSegments[addr] = elementSegment;
        }

        public void SetDataSegmentAt(int addr, byte[] dataSegment)
        {
            dataSegments[addr] = dataSegment;
        }
    }

    // A fake ModuleInstance for use in tests.
    //
    // FuncTypes are mapped from 0 - 499, where 0 - 3 are the canned types below, and the rest
    // are computed by dividing the index by 100 (modulo 4) to index into the canned types.
    //
    // The addresses for functions, tables, element segments, and globals are offset from their
    // indices. This is to ensure that the address is not equal to the index, which helps in testing.
    //
    // The offsets are:
    //
    // Global addr = index - 10 (the only valid indices are >= 10)
    // Function addr = index + 10
    // Table addr = index + 30
    // Element segment addr = index + 40
    // Data segment addr = index + 50
    public class FakeModuleInstance : ModuleInstance
    {
        public FakeModuleInstance()
            : base("test")
        {
            for (int i = 0; i < 500; i++)
            {
                FuncTypes.Add(funcTypes.ContainsKey(i) ? funcTypes[i] : funcTypes[(i / 100) % 4]);
            }

            for (int i = 0; i < 500; i++)
            {
                GlobalsMap.Add(i - 10);
                FuncsMap.Add(i + 10);
                TablesMap.Add(i + 30);
                ElementSegmentsMap.Add(i + 40);
                DataSegmentsMap.Add(i + 50);
            }
        }

        // A canned map of index to function type.
        //
        // 0: () -> ()
        // 1: (i32) -> ()
        // 2: () -> (i32)
        // 3: (i32, i32) -> (i32, i32)
        // 4: (i32, i32) -> (i32)
        // 5: (i32) -> (i32)
        public Dictionary<int, FuncType> funcTypes = new Dictionary<int, FuncType>()
        {
            { 0, new FuncType(new Derg.ValueType[] { }, new Derg.ValueType[] { }) },
            {
                1,
                new FuncType(new Derg.ValueType[] { Derg.ValueType.I32 }, new Derg.ValueType[] { })
            },
            {
                2,
                new FuncType(new Derg.ValueType[] { }, new Derg.ValueType[] { Derg.ValueType.I32 })
            },
            {
                3,
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 }
                )
            },
            {
                4,
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                )
            },
            {
                5,
                new FuncType(
                    new Derg.ValueType[] { Derg.ValueType.I32 },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                )
            },
        };
    }
}
