using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // Host environment expected by Emscripten.
    public class EmscriptenEnv
    {
        public Machine machine;

        public EmscriptenEnv(Machine machine)
        {
            this.machine = machine;
        }

        public void GetWasmTableEntry(int index) { }

        //
        // Exports from WASM
        //

        public void __indirect_function_table()
        {
            machine.GetTable(moduleInstance.ModuleName, "__indirect_function_table");
        }

        public void __wasm_call_ctors() => throw new NotImplementedException();

        // Runs the main function.
        //
        // Args:
        //  argc: Non-negative value representing the number of arguments passed to the program
        //    from the environment in which the program is run.
        //  argvPtr: Pointer to the first element of an array of argc + 1 pointers, of which the
        //    last one is null and the previous ones, if any, point to null-terminated multibyte
        //    strings that represent the arguments passed to the program from the execution
        //    environment. If argv[0] is not a null pointer (or, equivalently, if argc > 0), it
        //    points to a string that represents the name used to invoke the program, or to an
        //    empty string.
        //
        // Returns:
        //   The exit code of the program. Typically 0 means no error.
        public int main(int argc, int argvPtr) => throw new NotImplementedException();

        // Returns the location in the heap for the global errno variable.
        public int __errno_location() => throw new NotImplementedException();

        public int fflush(int fd) => throw new NotImplementedException();

        public void free(int ptr) => throw new NotImplementedException();

        public void setThrew(int a, int b) => throw new NotImplementedException();

        public void setTempRet0(int a) => throw new NotImplementedException();

        public void emscripten_stack_init() => throw new NotImplementedException();

        public int emscripten_stack_get_free() => throw new NotImplementedException();

        public int emscripten_stack_get_base() => throw new NotImplementedException();

        public int emscripten_stack_get_end() => throw new NotImplementedException();

        public int emscripten_stack_get_current() => throw new NotImplementedException();

        public int stackSave() => throw new NotImplementedException();

        public void stackRestore(int ptr) => throw new NotImplementedException();

        public int stackAlloc(int size) => throw new NotImplementedException();

        public void __cxa_free_exception(int excPtr) => throw new NotImplementedException();

        public void __cxa_increment_exception_refcount(int excPtr) =>
            throw new NotImplementedException();

        public void __cxa_decrement_exception_refcount(int excPtr) =>
            throw new NotImplementedException();

        public void __get_exception_message(int excPtr, int typePtrPtr, int msgPtrPtr) =>
            throw new NotImplementedException();

        public int __cxa_can_catch(int caughtType, int thrownType, int adjusted_ptrPtr) =>
            throw new NotImplementedException();

        public int __cxa_is_pointer_type(int type) => throw new NotImplementedException();

        public int dynCall_jiji(int a, int b, int c, int d, int e) =>
            throw new NotImplementedException();

        // The various dyncall_* functions for setjmp/longjmp.
        // The first character is the return type, and the rest are the arg types.
        //
        // v = void
        // i = i32
        // j = i64
        // f = f32
        // d = f64
        // e = externref
        // p = i32 (a pointer)
        public void dynCall_v(int index) => throw new NotImplementedException();

        public void dynCall_vi(int index, int a0) => throw new NotImplementedException();

        public void dynCall_vii(int index, int a0, int a1) => throw new NotImplementedException();

        public int dynCall_i(int index) => throw new NotImplementedException();

        public int dynCall_ii(int index, int a0) => throw new NotImplementedException();

        public int dynCall_iii(int index, int a0, int a1) => throw new NotImplementedException();

        //
        // Imports to WASM
        //

        public void __assert_fail(
            int conditionStrPtr,
            int filenameStrPtr,
            int line,
            int funcStrPtr
        ) => throw new NotImplementedException();

        public void abort() => throw new NotImplementedException();

        public void emscripten_memcpy_js(int dest, int src, int len)
        {
            Console.WriteLine($"emscripten_memcpy_js({dest}, {src}, {len})");
            byte[] mem = machine.Memory0;
            try
            {
                Array.Copy(mem, src, mem, dest, len);
            }
            catch (Exception)
            {
                throw new Trap(
                    "emscripten_memcpy_js: Access out of bounds: source offset "
                        + $"0x{src:X8}, destination offset 0x{dest:X8}, length 0x{len:X8} bytes"
                );
            }
        }

        public void emscripten_resize_heap(int requestedSize) =>
            throw new NotImplementedException();

        // Implementation of exceptions when not supported in WASM.
        public int __cxa_begin_catch(int excPtr) => throw new NotImplementedException();

        public void __cxa_end_catch() => throw new NotImplementedException();

        public int __cxa_find_matching_catch_2() => throw new NotImplementedException();

        public int __cxa_find_matching_catch_3(int arg0) => throw new NotImplementedException();

        public void __cxa_throw(int excPtr, int type, int destructor) =>
            throw new NotImplementedException();

        public void __resumeException(int excPtr) => throw new NotImplementedException();

        // The various invoke_* functions for setjmp/longjmp.
        public void invoke_v(int index) => throw new NotImplementedException();

        public void invoke_vi(int index, int a0) => throw new NotImplementedException();

        public void invoke_vii(int index, int a0, int a1) => throw new NotImplementedException();

        public int invoke_i(int index)
        {
            int sp = stackSave();
            try
            {
                return dynCall_i(index);
            }
            catch (Exception)
            {
                stackRestore(sp);
                setThrew(1, 0);
                return 0; // ????
            }
        }

        public int invoke_ii(int index, int a0) => throw new NotImplementedException();

        public int invoke_iii(int index, int a0, int a1) => throw new NotImplementedException();
    }

    // Ported from the Emscripted JavaScript output.
    public class EmscriptenExceptionInfo
    {
        EmscriptenEnv env;
        Heap heap;
        int excPtr;
        int ptr;

        public EmscriptenExceptionInfo(EmscriptenEnv env, int excPtr)
        {
            this.env = env;
            this.heap = new Heap(env.machine);
            this.excPtr = excPtr;
            this.ptr = excPtr - 24;
        }

        // Initializes native structure fields. Should be called once after allocated.
        public void Init(int type, int destructor)
        {
            AdjustedPtr = 0;
            Type = type;
            Destructor = destructor;
        }

        // Get pointer which is expected to be received by catch clause in C++ code. It may be adjusted
        // when the pointer is casted to some of the exception object base classes (e.g. when virtual
        // inheritance is used). When a pointer is thrown this method should return the thrown pointer
        // itself.
        public int GetExceptionPtr()
        {
            // Work around a fastcomp bug, this code is still included for some reason in a build without
            // exceptions support.
            if (env.__cxa_is_pointer_type(Type) != 0)
            {
                return heap.IntAt(ptr);
            }
            return (AdjustedPtr != 0) ? AdjustedPtr : excPtr;
        }

        public int Type
        {
            get => heap.IntAt(ptr + 4);
            set => heap.SetIntAt(ptr + 4, value);
        }

        public int Destructor
        {
            get => heap.IntAt(ptr + 8);
            set => heap.SetIntAt(ptr + 8, value);
        }

        public bool Caught
        {
            get => heap.ByteAt(ptr + 12) != 0;
            set => heap.SetByteAt(ptr + 12, (byte)(value ? 1 : 0));
        }

        public bool Rethrown
        {
            get => heap.ByteAt(ptr + 13) != 0;
            set => heap.SetByteAt(ptr + 13, (byte)(value ? 1 : 0));
        }

        public int AdjustedPtr
        {
            get => heap.IntAt(ptr + 16);
            set => heap.SetIntAt(ptr + 16, value);
        }
    }
}
