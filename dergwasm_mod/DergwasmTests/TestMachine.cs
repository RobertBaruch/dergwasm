using System.Collections.Generic;
using System.Linq;
using Derg;

namespace DergwasmTests
{
    // A Machine for testing. Implements a real frame stack, but all other runtime structures
    // are just dictionaries.
    public class TestMachine : IMachine
    {
        public Stack<Frame> frame_stack = new Stack<Frame>();
        public Dictionary<int, FuncType> funcTypes = new Dictionary<int, FuncType>()
        {
            { 0, new FuncType(new ValueType[] { }, new ValueType[] { }) },
            { 1, new FuncType(new ValueType[] { ValueType.I32 }, new ValueType[] { }) },
            { 2, new FuncType(new ValueType[] { }, new ValueType[] { ValueType.I32 }) },
            {
                3,
                new FuncType(
                    new ValueType[] { ValueType.I32, ValueType.I32 },
                    new ValueType[] { ValueType.I32, ValueType.I32 }
                )
            },
        };
        public Dictionary<int, ModuleFunc> module_funcs = new Dictionary<int, ModuleFunc>();
        public Dictionary<int, Table> tables = new Dictionary<int, Table>();

        public Frame Frame
        {
            get => frame_stack.Peek();
            set => frame_stack.Push(value);
        }

        public void PopFrame()
        {
            Frame last_frame = frame_stack.Pop();
            Frame
                .value_stack
                .AddRange(
                    last_frame
                        .value_stack
                        .GetRange(last_frame.value_stack.Count - last_frame.Arity, last_frame.Arity)
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

        public void Push(Value val) => Frame.value_stack.Add(val);

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

        public FuncType GetFuncTypeFromIndex(int index)
        {
            return funcTypes.ContainsKey(index) ? funcTypes[index] : funcTypes[index / 100];
        }

        public void InvokeFuncFromIndex(int index)
        {
            ModuleFunc func = module_funcs[index];
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

        public Table GetTableFromIndex(int index) => tables[index];

        public Func GetFunc(int addr) => module_funcs[addr];

        public void InvokeFunc(int addr) => InvokeFuncFromIndex(addr);

        public int GetFuncAddrFromIndex(int idx) => idx + 10; // Ensure addr != idx.

        // Sets the program up for execution, with a signature given by the idx (see GetFuncTypeFromIndex).
        // The program always has two I32 locals.
        public void SetProgram(int idx, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<UnflattenedInstruction>(instructions).Flatten(0);
            ModuleFunc func = new ModuleFunc(
                GetFuncTypeFromIndex(idx),
                new ValueType[] { ValueType.I32, ValueType.I32 },
                program
            );
            FuncType signature = GetFuncTypeFromIndex(idx);
            Frame = new Frame(func, null);
            Label = new Label(Frame.Arity, program.Count);
        }

        // Adds a function at the given index. The index is also used to determine the function's
        // signature (see GetFuncTypeFromIndex). The function has two I32 locals.
        public void AddFunction(int idx, params UnflattenedInstruction[] instructions)
        {
            List<Instruction> program = new List<UnflattenedInstruction>(instructions).Flatten(0);
            module_funcs[idx] = new ModuleFunc(
                GetFuncTypeFromIndex(idx),
                new ValueType[] { ValueType.I32, ValueType.I32 },
                program
            );
        }

        public void AddTable(int idx, Table table) => tables[idx] = table;

        public void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = Frame.Code[PC];
                InstructionEvaluation.Execute(insn, this);
            }
        }
    }
}
