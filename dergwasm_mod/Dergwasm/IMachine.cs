using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // The interface to a WASM machine.
    public interface IMachine
    {
        // Gets the value at the top of the current frame's label stack.
        Value TopOfStack { get; }

        // The current frame. Getting peeks at the current frame while setting will push a frame.
        Frame Frame { get; set; }

        // The label at the top of the current frame's label stack. Getting peeks at the current label,
        // while setting will push a label.
        Label Label { get; set; }

        // The current program counter from the current frame.
        int PC { get; set; }

        // Pushes the given value onto the stack.
        void Push(Value val);

        // Pops the top value off the stack.
        Value Pop();

        // Gets the locals for the current frame.
        Value[] Locals { get; }

        // Gets the global address for the current module's index.
        int GetGlobalAddrForIndex(int idx);

        // Gets the machine's globals.
        Value[] Globals { get; }

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
        // to map the index to the machine's type address.
        FuncType GetFuncTypeFromIndex(int index);

        // Gets the Table for the given index, using the current frame's module
        // to map the index to the machine's table address.
        Table GetTableFromIndex(int index);

        Func GetFunc(int addr);

        int GetFuncAddrFromIndex(int index);

        // Invokes the function at the given index, using the current frame's module
        // to map the index to the machine's function address. Note that you can only
        // invoke a function in the current module or on the host using this. If you
        // need to invoke a function outside the module, use InvokeExternalFunc().
        void InvokeFuncFromIndex(int index);

        void InvokeFunc(int addr);
    }
}
