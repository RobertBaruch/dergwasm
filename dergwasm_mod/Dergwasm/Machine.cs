using Elements.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public class Machine : IMachine
    {
        public Stack<Frame> frameStack = new Stack<Frame>();
        public List<FuncType> funcTypes = new List<FuncType>();
        public List<ModuleFunc> funcs = new List<ModuleFunc>();
        public List<Table> tables = new List<Table>();
        public List<ElementSegment> elementSegments = new List<ElementSegment>();
        public Value[] Globals;
        public Memory Memory = new Memory(new Limits(1));
        public List<byte[]> dataSegments = new List<byte[]>();

        public Frame Frame
        {
            get => frameStack.Peek();
            set => frameStack.Push(value);
        }

        public void PopFrame()
        {
            Frame last_frame = frameStack.Pop();
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

        public int GetGlobalAddrForIndex(int idx) => Frame.Module.GlobalsMap[idx];

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

        public FuncType GetFuncTypeFromIndex(int idx) => funcTypes[Frame.Module.FuncTypesMap[idx]];

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

        public int GetFuncAddrFromIndex(int idx) => Frame.Module.FuncsMap[idx];

        public Table GetTableFromIndex(int idx) => tables[Frame.Module.TablesMap[idx]];

        public ElementSegment GetElementSegmentFromIndex(int idx) =>
            elementSegments[Frame.Module.ElementSegmentsMap[idx]];

        public void DropElementSegmentFromIndex(int idx) =>
            elementSegments.RemoveAt(Frame.Module.ElementSegmentsMap[idx]);

        public int GetDataSegmentAddrFromIndex(int idx) => Frame.Module.DataSegmentsMap[idx];

        public byte[] GetDataSegmentFromIndex(int idx) =>
            dataSegments[GetDataSegmentAddrFromIndex(idx)];

        public void DropDataSegmentFromIndex(int idx) =>
            dataSegments.RemoveAt(GetDataSegmentAddrFromIndex(idx));

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
