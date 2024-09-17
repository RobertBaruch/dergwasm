using System;
using System.Collections.Generic;
using System.Linq;
using Derg.Instructions;
using Derg.Resonite;
using Derg.Wasm;
using Dergwasm.Runtime;
using FrooxEngine;

namespace Derg.Runtime
{
    // Represents the state of the machine, in the context of executing a function.
    //
    // Frames have their own label and value stacks.
    //
    // Frames are also not skippable like blocks. That means you can't exit a function and continue to
    // anything other than the function in the previous frame. This is in contrast to blocks,
    // where you can break out of multiple levels of blocks.
    public class Frame
    {
        public int stepBudget = -1;

        // The function currently executing.
        public ModuleFunc Func;

        // The function's locals. This includes its arguments, which come first.
        public Value[] Locals;

        // The module instance this frame is executing in.
        public ModuleInstance Module;

        // The current program counter.
        public int PC;

        // The label stack. Labels never apply across function boundaries.
        public Stack<Label> label_stack;

        // The value stack. Values never apply across function boundaires. Return values
        // are handled explicitly by copying from stack to stack. Args are locals copied
        // from the caller's stack.
        public Stack<Value> value_stack;

        public Frame prev_frame;

        public Frame(ModuleFunc func, ModuleInstance module, Frame prev_frame)
        {
            if (func != null)
            {
                Locals = new Value[func.Signature.args.Length + func.Locals.Length];
            }
            Module = module;
            PC = 0;
            label_stack = new Stack<Label>();
            value_stack = new Stack<Value>();
            Func = func;
            this.prev_frame = prev_frame;
        }

        // Steps the machine by n steps. Note that call instructions count as one step.
        public void Step(Machine machine, int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Instruction insn = Code[PC];
                InstructionEvaluation.Execute(insn, machine, this);
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

        // Continually steps the machine until the current frame is done. This is detected
        // when there are no labels on the label stack. Thus, before entering this function,
        // the frame should have a label on the stack. The first label is typically a label
        // pointing to the end of the function, and containing the function's arity.
        public void Execute(Machine machine)
        {
            while (HasLabel())
            {
                Step(machine);
            }
        }

        public virtual int Arity
        {
            get => Func.Signature.returns.Length;
        }

        public List<Instruction> Code
        {
            get => Func.Code;
        }

        public Value TopOfStack => value_stack.First();

        public Value Pop() => value_stack.Pop();

        public T Pop<T>() => Pop().As<T>();

        public void Push<T>(in T value) => value_stack.Push(Value.From(value));

        public void Push(Value val) => value_stack.Push(val);

        public int StackLevel() => value_stack.Count;

        public Label PopLabel() => label_stack.Pop();

        public Label Label
        {
            get => label_stack.Peek();
            set => label_stack.Push(value);
        }

        public bool HasLabel() => label_stack.Count > 0;

        public int GetGlobalAddrForIndex(int idx) => Module.GlobalsMap[idx];

        public int GetTableAddrForIndex(int idx) => Module.TablesMap[idx];

        public int GetElementSegmentAddrForIndex(int idx) => Module.ElementSegmentsMap[idx];

        public int GetDataSegmentAddrForIndex(int idx) => Module.DataSegmentsMap[idx];

        public int GetFuncAddrForIndex(int idx) => Module.FuncsMap[idx];

        public FuncType GetFuncTypeForIndex(int idx) => Module.FuncTypes[idx];

        public void InvokeFuncFromIndex(Machine machine, int idx) =>
            InvokeFunc(machine, GetFuncAddrForIndex(idx));

        public void InvokeFunc(Machine machine, int addr) =>
            InvokeFunc(machine, machine.funcs[addr]);

        // Executes a host function call. This sets up a new frame, pops the args off the current frame and
        // places them in the new frame's locals, and then invokes the host function. After the invokation,
        // any return values are popped off the new frame and placed on the current frame's stack.
        void InvokeHostFunc(Machine machine, HostFunc f)
        {
            if (machine.Debug)
                Console.WriteLine($"Invoking host func {f.ModuleName}.{f.Name}");

            f.Proxy.Invoke(machine, this);
        }

        // Executes a module function call. This sets up a new frame, pops the args off the current frame and
        // places them in the new frame's locals, and then invokes the host function. After the invokation,
        // any return values are popped off the new frame and placed on the current frame's stack.
        void InvokeModuleFunc(Machine machine, ModuleFunc f)
        {
            if (machine.Debug)
            {
                Console.WriteLine($"Invoking module func {f.ModuleName}.{f.Name}");
            }

            int arity = f.Signature.returns.Length;
            int args = f.Signature.args.Length;

            Frame next_frame = new Frame(f, Module, this);

            // The first value pushed is the first local.
            for (int i = args - 1; i >= 0; --i)
            {
                next_frame.Locals[i] = value_stack.Pop();
            }

            if (machine.Debug)
            {
                for (int i = 0; i < args; i++)
                {
                    Console.WriteLine($"  arg {i}: {next_frame.Locals[i]}");
                }
            }

            next_frame.Label = new Label(arity, f.Code.Count);
            next_frame.Execute(machine);
            next_frame.EndFrame();
        }

        // Executes a function call. This sets up a new frame, pops the args off the current frame and
        // places them in the new frame's locals, and then invokes the host function. After the invokation,
        // any return values are popped off the new frame and placed on the current frame's stack.
        public void InvokeFunc(Machine machine, Func f)
        {
            if (f is HostFunc host_func)
            {
                InvokeHostFunc(machine, host_func);
                return;
            }

            if (f is ModuleFunc module_func)
            {
                InvokeModuleFunc(machine, module_func);
                return;
            }

            throw new Trap(
                $"Attempted to invoke a non-module non-host func of type {f.GetType()}."
            );
        }

        // Loads the return values from this frame into the previous frame, if there is one.
        public void EndFrame()
        {
            if (prev_frame == null)
            {
                return;
            }
            Value[] retvals = new Value[Arity];
            for (int i = 0; i < Arity; ++i)
            {
                retvals[i] = value_stack.Pop();
            }
            // retvals[0] needs to end up as the top of the stack,
            // so we push them in reverse order.
            for (int i = Arity - 1; i >= 0; --i)
            {
                prev_frame.value_stack.Push(retvals[i]);
            }
        }
    }

    // A call frame for when we're calling a host function.
    public class HostFrame : Frame
    {
        // The function currently executing.
        public HostFunc HostFunc;

        public HostFrame(HostFunc func, ModuleInstance module, Frame prev_frame)
            : base(null, module, prev_frame)
        {
            if (func != null)
            {
                Locals = new Value[func.Signature.args.Length];
            }
            HostFunc = func;
        }

        public override int Arity
        {
            get => HostFunc.Signature.returns.Length;
        }
    }
}
