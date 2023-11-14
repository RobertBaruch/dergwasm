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
        // Pushes the given value onto the stack.
        void Push(Value val);

        // Pops the top value off the stack.
        Value Pop();

        // Peeks at the top value on the stack.
        Value Peek();

        // Gets the number of values on the stack.
        int StackLevel();

        // Remove stack values from the given level (where the bottom of the stack is 0)
        // to the top of the stack minus the arity. Thus, after this operation, there
        // will be from_level + arity values on the stack.
        void RemoveStack(int from_level, int arity);

        // Gets the current frame.
        Frame CurrentFrame();

        // Pushes a frame onto the frame stack.
        void PushFrame(Frame frame);

        // Gets the current program counter from the current frame.
        int CurrentPC();

        // Increments the program counter on the current frame.
        void IncrementPC();

        // Sets the program counter on the current frame.
        void SetPC(int pc);

        // Creates and pushes a label onto the current frame. The args is the number of
        // arguments the block consumes off the value stack. The arity is the number
        // of values expected to be returned by the block. The target is the program
        // counter to jump to for a BR 0 instruction.
        void PushLabel(int args, int arity, int target);

        // Pops a label off the current frame. 
        Label PopLabel();

        // Gets the FuncType for the given index using the current frame's module
        // to map the index to the machine's type address.
        FuncType GetFuncTypeFromIndex(int index);
    }
}
