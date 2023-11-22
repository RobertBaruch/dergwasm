using System;
using System.Collections.Generic;
using System.Linq;
using Derg;

namespace DergwasmTests
{
    // A Machine for testing. Implements a real frame stack, but all other runtime structures
    // are just dictionaries.
    //
    // In the test machine, the addresses for functions, tables, element segments, and globals are offset
    // from their indices. This is to ensure that the address is not equal to the index, which
    // helps in testing.
    //
    // The offsets are:
    //
    // Global addr = index - 10
    // Function addr = index + 10
    // Table addr = index + 30
    // Element segment addr = index + 40
    // Data segment addr = index + 50
    //
    // You can add functions, tables, element segments, and globals to the machine using the Add* methods,
    // specifying the address to put them in.
    public class TestMachine : IMachine
    {
        public const int VoidType = 0;
        public const int OneArgType = 1;
        public const int OneReturnType = 2;
        public const int TwoArgTwoReturnType = 3;

        public Stack<Frame> frame_stack = new Stack<Frame>();
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
        };
        public Dictionary<int, ModuleFunc> funcs = new Dictionary<int, ModuleFunc>();
        public Dictionary<int, Table> tables = new Dictionary<int, Table>();
        public Dictionary<int, ElementSegment> elementSegments =
            new Dictionary<int, ElementSegment>();
        public Value[] Globals = new Value[2];
        public Memory Memory = new Memory(new Limits(1));
        public Dictionary<int, byte[]> dataSegments = new Dictionary<int, byte[]>();

        public int GetGlobalAddrForIndex(int idx) => idx - 10;

        public Frame Frame
        {
            get => frame_stack.Peek();
            set => frame_stack.Push(value);
        }

        public void PopFrame()
        {
            Frame last_frame = frame_stack.Pop();
            Frame.value_stack.AddRange(
                last_frame.value_stack.GetRange(
                    last_frame.value_stack.Count - last_frame.Arity,
                    last_frame.Arity
                )
            );
        }

        public int PC
        {
            get => Frame.pc;
            set => Frame.pc = value;
        }

        public Value TopOfStack => Frame.value_stack.Last();

        public Value Pop()
        {
            Value top = Frame.value_stack.Last();
            Frame.value_stack.RemoveAt(Frame.value_stack.Count - 1);
            return top;
        }

        public unsafe T Pop<T>()
            where T : unmanaged
        {
            Value top = Pop();
            return *(T*)&top.value_lo;
        }

        public void Push(Value val) => Frame.value_stack.Add(val);

        public void Push(int val) => Push(new Value(val));

        public void Push(uint val) => Push(new Value(val));

        public void Push(long val) => Push(new Value(val));

        public void Push(ulong val) => Push(new Value(val));

        public void Push(float val) => Push(new Value(val));

        public void Push(double val) => Push(new Value(val));

        public void Push(bool val) => Push(new Value(val));

        public Value[] Locals => Frame.Locals;

        Value[] IMachine.Globals => Globals;

        public int StackLevel() => Frame.value_stack.Count;

        public void RemoveStack(int from_level, int arity)
        {
            Frame.value_stack.RemoveRange(from_level, Frame.value_stack.Count - from_level - arity);
        }

        public Label PopLabel() => Frame.label_stack.Pop();

        public Label Label
        {
            get => Frame.label_stack.Peek();
            set => Frame.label_stack.Push(value);
        }

        public Memory GetMemoryFromIndex(int idx)
        {
            if (idx != 0)
            {
                throw new Trap($"Nonzero memory {idx} accessed.");
            }
            return Memory;
        }

        public byte[] Memory0 => Memory.Data;

        public Span<byte> Span0(int offset, int sz) => new Span<byte>(Memory.Data, offset, sz);

        public FuncType GetFuncTypeFromIndex(int index)
        {
            return funcTypes.ContainsKey(index) ? funcTypes[index] : funcTypes[index / 100];
        }

        public void InvokeFuncFromIndex(int idx) => InvokeFunc(GetFuncAddrFromIndex(idx));

        public void InvokeFunc(int addr)
        {
            ModuleFunc func = funcs[addr];
            int arity = func.Signature.returns.Length;
            int args = func.Signature.args.Length;

            Frame next_frame = new Frame(func, Frame.Module);

            // Remove args from stack and place in new frame's locals.
            Frame.value_stack.CopyTo(0, next_frame.Locals, Frame.value_stack.Count - args, args);
            Frame.value_stack.RemoveRange(Frame.value_stack.Count - args, args);

            // Here we would initialize the other locals. But we assume they're I32, so they're
            // already defaulted to zero.

            Frame = next_frame;
            PC = -1; // So that incrementing PC goes to beginning.
            Label = new Label(arity, func.Code.Count);
        }

        public Func GetFunc(int addr) => funcs[addr];

        public int GetFuncAddrFromIndex(int idx) => idx + 10; // Ensure addr != idx.

        // Sets the program up for execution, with a signature given by the signature_idx (see GetFuncTypeFromIndex).
        // The program always has two I32 locals.
        public void SetProgram(int signature_idx, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<UnflattenedInstruction>(instructions).Flatten(0);
            ModuleFunc func = new ModuleFunc(
                GetFuncTypeFromIndex(signature_idx),
                new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                program
            );
            Frame = new Frame(func, null);
            Label = new Label(Frame.Arity, program.Count);
        }

        // Adds a function at the given addr. The index is also used to determine the function's
        // signature (see GetFuncTypeFromIndex). The function has two I32 locals.
        public void AddFunction(int addr, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<UnflattenedInstruction>(instructions).Flatten(0);
            funcs[addr] = new ModuleFunc(
                GetFuncTypeFromIndex(addr - 10),
                new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                program
            );
        }

        public void AddTable(int addr, Table table) => tables[addr] = table;

        public Table GetTableFromIndex(int idx) => tables[idx + 30];

        public void AddElementSegment(int addr, ElementSegment segment) =>
            elementSegments[addr] = segment;

        public ElementSegment GetElementSegmentFromIndex(int idx) => elementSegments[idx + 40];

        public void DropElementSegmentFromIndex(int idx) => elementSegments.Remove(idx + 40);

        public int GetDataSegmentAddrFromIndex(int idx) => idx + 50;

        public byte[] GetDataSegmentFromIndex(int idx) => dataSegments[idx + 50];

        public void AddDataSegment(int addr, byte[] data) => dataSegments[addr] = data;

        public void DropDataSegmentFromIndex(int idx) => dataSegments.Remove(idx + 50);

        public void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = Frame.Code[PC];
                InstructionEvaluation.Execute(insn, this);
            }
        }

        public UnflattenedInstruction Insn(InstructionType type, params Value[] operands)
        {
            return new UnflattenedInstruction(
                type,
                (from operand in operands select new UnflattenedOperand(operand)).ToArray()
            );
        }
    }
}
