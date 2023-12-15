using System;
using System.Collections.Generic;

namespace Derg
{
    // The interface to a WASM machine.
    public interface IMachine
    {
        string MainModuleName { get; set; }

        bool Debug { get; set; }

        // Gets the value at the top of the current frame's label stack.
        Value TopOfStack { get; }

        // The current frame. Getting peeks at the current frame while setting will push a frame.
        Frame Frame { get; set; }

        // The label at the top of the current frame's label stack. Getting peeks at the current label,
        // while setting will push a label.
        Label Label { get; set; }

        // Whether there is at least one label on the current frame's label stack.
        bool HasLabel();

        // The current program counter from the current frame.
        int PC { get; set; }

        // Pushes the given value onto the stack.
        void Push(Value val);

        // We can't make these generic because we're not on .NET 7 or above yet. We are currently
        // working with .NET Framework 4.7.2.
        void Push(bool val);
        void Push(int val);
        void Push(uint val);
        void Push(long val);
        void Push(ulong val);
        void Push(float val);
        void Push(double val);

        // Pops the top value off the stack.
        Value Pop();

        unsafe T Pop<T>()
            where T : unmanaged;

        // Pushes the given value onto the stack. Use only when you absolutely do not know
        // the type, since this performs type checks at runtime.
        void Push<R>(R val);

        // Adds the given global to the machine, returning its address.
        int AddGlobal(Value global);

        // Gets the global address for the current module's index. May only be used
        // when the machine is actively running code.
        int GetGlobalAddrForIndex(int idx);

        // Gets the machine's globals.
        List<Value> Globals { get; }

        // Gets the number of values on the stack.
        int StackLevel();

        // Remove stack values from the given level (where the bottom of the stack is 0)
        // to the top of the stack minus the arity. Thus, after this operation, there
        // will be from_level + arity values on the stack.
        void RemoveStack(int from_level, int arity);

        // Pop a frame. This effectively returns from the current function.
        void PopFrame();

        // Pops a label off the current frame.
        Label PopLabel();

        // Gets the FuncType for the given index, using the current frame's module
        // to map the index to the machine's type address. May only be used
        // when the machine is actively running code.
        FuncType GetFuncTypeFromIndex(int idx);

        // Adds the given table to the machine, returning its address.
        int AddTable(Table table);

        // Gets the Table for the given address.
        Table GetTable(int addr);

        // Gets the Table for the given index, using the current frame's module
        // to map the index to the machine's table address. May only be used
        // when the machine is actively running code.
        Table GetTableFromIndex(int idx);

        // Adds the given element segment to the machine, returning its address.
        int AddElementSegment(ElementSegment elementSegment);

        // Nulls out the ElementSegment for the given address.
        void DropElementSegment(int addr);

        // Gets the ElementSegment for the given address.
        ElementSegment GetElementSegment(int addr);

        // Gets the ElementSegment for the given index, using the current frame's module
        // to map the index to the machine's element segment address. May only be used
        // when the machine is actively running code.
        ElementSegment GetElementSegmentFromIndex(int idx);

        // Nulls out the ElementSegment for the given index, using the current frame's module
        // to map the index to the machine's element segment address. May only be used
        // when the machine is actively running code.
        void DropElementSegmentFromIndex(int idx);

        // Adds the given memory to the machine, returning its address.
        int AddMemory(Memory memory);

        // Gets the Memory for the given address.
        Memory GetMemory(int addr);

        // Gets the Memory for the given index, using the current frame's module
        // to map the index to the machine's memory address.
        Memory GetMemoryFromIndex(int idx);

        // A shortcut to get to the data in Memory 0.
        byte[] Memory0 { get; }

        // Gets a span of bytes from Memory 0.
        Span<byte> Span0(uint offset, uint sz);

        // Adds the given data segment to the machine, returning its address.
        int AddDataSegment(byte[] data);

        // Nulls out the DataSegment for the given address.
        void DropDataSegment(int addr);

        // Gets the DataSegment for the given index, using the current frame's module
        // to map the index to the machine's data segment address. May only be used
        // when the machine is actively running code.
        byte[] GetDataSegmentFromIndex(int idx);

        // Gets the address of the data segment for the given index, using the current frame's module
        // to map the index to the machine's data segment address. May only be used
        // when the machine is actively running code.
        int GetDataSegmentAddrFromIndex(int idx);

        // Nulls out the DataSegment for the given index, using the current frame's module
        // to map the index to the machine's data segment address. May only be used
        // when the machine is actively running code.
        void DropDataSegmentFromIndex(int idx);

        // Adds the given function to the machine, returning its address.
        int AddFunc(Func func);

        // The number of funcs in the machine.
        int NumFuncs { get; }

        // Gets the function at the given address.
        Func GetFunc(int addr);

        // Gets the function with the given module name and function name.
        Func GetFunc(string moduleName, string name);

        // Gets the function address for the given index, using the current frame's module
        // to map the index to the machine's function address. May only be used
        // when the machine is actively running code.
        int GetFuncAddrFromIndex(int idx);

        // Invokes the function at the given index, using the current frame's module
        // to map the index to the machine's function address. Note that you can only
        // invoke a function in the current module or on the host using this. If you
        // need to invoke a function outside the module, use InvokeFunc().
        //
        // Used only by the machine when executing the CALL or CALL_INDIRECT instructions.
        // This method relies on a machine that is already running.
        void InvokeFuncFromIndex(int idx);

        // Invokes the function at the given address. Used only by the machine when
        // executing the CALL or CALL_INDIRECT instructions. This method relies on
        // a machine that is already running.
        void InvokeFunc(int addr);

        // Invokes a ModuleFunc. The function is expected to have zero arguments.
        // Only the host should use this, and only when the machine isn't running.
        // A frame will be left on the machine's frame stack which contains any return
        // values. The caller is responsible for popping this frame off the stack when
        // the return values have been consumed.
        void InvokeExpr(ModuleFunc func);

        // Steps the machine by n steps (default 1).
        void Step(int n = 1);

        // Registers a host function with the given name, signature, and proxy.
        void RegisterHostFunc(string moduleName, string name, FuncType signature, HostProxy proxy);

        // Finds the host function with the given name and signature, adds it to the machine's
        // functions, and returns its address. If the function is not found, throws a Trap.
        // This should only be called during extern func resolution.
        int ResolveHostFunc(string moduleName, string name);
    }
}
