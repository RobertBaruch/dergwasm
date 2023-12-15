using System;
using System.Collections.Generic;
using System.Linq;

namespace Derg
{
    public class Machine : IMachine
    {
        bool debug = false;
        public int stepBudget = -1;
        public string mainModuleName;
        public Dictionary<string, HostFunc> hostFuncs = new Dictionary<string, HostFunc>();
        public Stack<Frame> frameStack = new Stack<Frame>();
        public List<FuncType> funcTypes = new List<FuncType>(); // is this even used?
        public List<Func> funcs = new List<Func>();
        public List<Table> tables = new List<Table>();
        public List<ElementSegment> elementSegments = new List<ElementSegment>();
        public List<Value> Globals = new List<Value>();
        public List<Memory> memories = new List<Memory>();
        public List<byte[]> dataSegments = new List<byte[]>();

        public string MainModuleName
        {
            get => mainModuleName;
            set => mainModuleName = value;
        }

        public bool Debug
        {
            get => debug;
            set => debug = value;
        }

        public Frame Frame
        {
            get => frameStack.Peek();
            set => frameStack.Push(value);
        }

        public void PopFrame()
        {
            Frame last_frame = frameStack.Pop();
            if (frameStack.Count == 0)
            {
                return;
            }
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

        public void Push<R>(R ret)
        {
            switch (ret)
            {
                case int r:
                    Push(r);
                    break;

                case uint r:
                    Push(r);
                    break;

                case long r:
                    Push(r);
                    break;

                case ulong r:
                    Push(r);
                    break;

                case float r:
                    Push(r);
                    break;

                case double r:
                    Push(r);
                    break;

                case bool r:
                    Push(r);
                    break;

                default:
                    throw new Trap($"Invalid push type {ret.GetType()}");
            }
        }

        public Value[] Locals => Frame.Locals;

        public int AddGlobal(Value global)
        {
            Globals.Add(global);
            return Globals.Count - 1;
        }

        public int GetGlobalAddrForIndex(int idx) => Frame.Module.GlobalsMap[idx];

        List<Value> IMachine.Globals => Globals;

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

        public bool HasLabel() => Frame.label_stack.Count > 0;

        public int AddMemory(Memory memory)
        {
            memories.Add(memory);
            return memories.Count - 1;
        }

        public Memory GetMemory(int addr) => memories[addr];

        public Memory GetMemoryFromIndex(int idx)
        {
            if (idx != 0)
            {
                throw new Trap($"Nonzero memory {idx} accessed.");
            }
            return memories[0];
        }

        public byte[] Memory0 => memories[0].Data;

        // Span accepts ints, but converts them internally to uints.
        public Span<byte> Span0(uint offset, uint sz) =>
            new Span<byte>(memories[0].Data, (int)offset, (int)sz);

        public FuncType GetFuncTypeFromIndex(int idx) => Frame.Module.FuncTypes[idx];

        public void InvokeFuncFromIndex(int idx) => InvokeFunc(GetFuncAddrFromIndex(idx));

        public void InvokeFunc(int addr) => InvokeFunc(funcs[addr]);

        void InvokeHostFunc(HostFunc f)
        {
            Console.WriteLine($"Invoking host func {f.ModuleName}.{f.Name}");

            Frame next_frame_host = new HostFrame(f, Frame.Module);
            int numArgs = f.Proxy.NumArgs();

            // Remove args from stack and place in new frame's locals.
            Frame.value_stack.CopyTo(
                Frame.value_stack.Count - numArgs,
                next_frame_host.Locals,
                0,
                numArgs
            );
            Frame.value_stack.RemoveRange(Frame.value_stack.Count - numArgs, numArgs);

            Frame = next_frame_host;
            // For consistency, we also stick a label in.
            Label = new Label(f.Proxy.Arity(), 0);

            f.Proxy.Invoke(this);

            PopFrame();
        }

        void InvokeModuleFunc(ModuleFunc f)
        {
            int arity = f.Signature.returns.Length;
            int args = f.Signature.args.Length;

            Frame next_frame = new Frame(f, Frame.Module);

            // Remove args from stack and place in new frame's locals.
            Frame.value_stack.CopyTo(Frame.value_stack.Count - args, next_frame.Locals, 0, args);
            Frame.value_stack.RemoveRange(Frame.value_stack.Count - args, args);

            Frame = next_frame;
            PC = -1; // So that incrementing PC goes to beginning.
            Label = new Label(arity, f.Code.Count);
        }

        public void InvokeFunc(Func f)
        {
            if (f is HostFunc host_func)
            {
                InvokeHostFunc(host_func);
                return;
            }

            if (f is ModuleFunc module_func)
            {
                InvokeModuleFunc(module_func);
                return;
            }

            throw new Trap($"Attempted to invoke a non-module func of type {f.GetType()}.");
        }

        public void InvokeExpr(ModuleFunc func)
        {
            // This frame collects any return values.
            Frame = new Frame(null, func.Module);

            Frame = new Frame(func, func.Module);
            PC = 0;
            Label = new Label(0, func.Code.Count);

            while (HasLabel())
            {
                Step();
            }
        }

        public int AddFunc(Func func)
        {
            funcs.Add(func);
            return funcs.Count - 1;
        }

        public int NumFuncs => funcs.Count;

        public Func GetFunc(int addr) => funcs[addr];

        public Func GetFunc(string moduleName, string name)
        {
            // O(N) for now
            foreach (var f in funcs)
            {
                if (f.ModuleName == moduleName && f.Name == name)
                {
                    return f;
                }
            }
            return null;
        }

        public int GetFuncAddrFromIndex(int idx) => Frame.Module.FuncsMap[idx];

        public int AddTable(Table table)
        {
            tables.Add(table);
            return tables.Count - 1;
        }

        public Table GetTable(int addr) => tables[addr];

        public Table GetTable(string moduleName, string name)
        {
            // O(N) for now
            foreach (var t in tables)
            {
                if (t.ModuleName == moduleName && t.Name == name)
                {
                    return t;
                }
            }
            return null;
        }

        public Table GetTableFromIndex(int idx) => tables[Frame.Module.TablesMap[idx]];

        public int AddElementSegment(ElementSegment elementSegment)
        {
            elementSegments.Add(elementSegment);
            return elementSegments.Count - 1;
        }

        public void DropElementSegment(int addr) => elementSegments[addr] = null;

        public ElementSegment GetElementSegment(int addr) => elementSegments[addr];

        public ElementSegment GetElementSegmentFromIndex(int idx) =>
            elementSegments[Frame.Module.ElementSegmentsMap[idx]];

        public void DropElementSegmentFromIndex(int idx) =>
            elementSegments[Frame.Module.ElementSegmentsMap[idx]] = null;

        public int AddDataSegment(byte[] dataSegment)
        {
            dataSegments.Add(dataSegment);
            return dataSegments.Count - 1;
        }

        public void DropDataSegment(int addr) => dataSegments[addr] = null;

        public int GetDataSegmentAddrFromIndex(int idx) => Frame.Module.DataSegmentsMap[idx];

        public byte[] GetDataSegmentFromIndex(int idx) =>
            dataSegments[GetDataSegmentAddrFromIndex(idx)];

        public void DropDataSegmentFromIndex(int idx) =>
            dataSegments[GetDataSegmentAddrFromIndex(idx)] = null;

        public void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = Frame.Code[PC];
                InstructionEvaluation.Execute(insn, this, Frame);
                if (stepBudget > 0)
                {
                    stepBudget--;
                    if (stepBudget == 0)
                    {
                        throw new Trap("Step budget exceeded");
                    }
                }
            }
        }

        public void RegisterHostFunc(
            string moduleName,
            string name,
            FuncType signature,
            HostProxy proxy
        )
        {
            hostFuncs.Add($"{moduleName}.{name}", new HostFunc(moduleName, name, signature, proxy));
        }

        public int ResolveHostFunc(string moduleName, string name)
        {
            string key = $"{moduleName}.{name}";
            if (!hostFuncs.ContainsKey(key))
            {
                throw new Trap($"Could not find host function {key}");
            }
            funcs.Add(hostFuncs[key]);
            return funcs.Count - 1;
        }
    }
}
