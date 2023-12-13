using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // Exception thrown when longjmp is called in a C program. The WASM code and the EmscriptenEnv
    // work together to implement setjmp/longjmp functionality.
    public class LongjmpException : Exception
    {
        public LongjmpException()
            : base() { }
    }

    // Host environment expected by Emscripten.
    public class EmscriptenEnv
    {
        public Machine machine;

        public EmscriptenEnv(Machine machine)
        {
            this.machine = machine;
        }

        // Returns a funcref.
        public Value GetWasmTableEntry(int index)
        {
            Table wasmTable = __indirect_function_table();
            if (index < 0 || index >= wasmTable.Elements.Length)
            {
                throw new Trap($"__indirect_function_table index out of bounds: {index}");
            }
            return wasmTable.Elements[index];
        }

        ModuleFunc SetUpExportedFuncCall(string name)
        {
            Frame returning_frame = new Frame(null, machine.Frame.Module);
            machine.Frame = returning_frame;
            return machine.GetFunc(machine.MainModuleName, name) as ModuleFunc;
        }

        void ExecuteFunc(ModuleFunc f)
        {
            machine.InvokeExpr(f);
            machine.PC = 0;

            while (machine.HasLabel())
            {
                machine.Step();
            }
            machine.PopFrame();
        }

        R ExecuteFunc<R>(ModuleFunc f)
            where R : unmanaged
        {
            machine.InvokeExpr(f);
            machine.PC = 0;

            while (machine.HasLabel())
            {
                machine.Step();
            }
            R retval = machine.Pop<R>();
            machine.PopFrame();
            return retval;
        }

        void SetUpFuncCall()
        {
            Frame returning_frame = new Frame(null, machine.Frame.Module);
            machine.Frame = returning_frame;
        }

        public void CallFunc(ModuleFunc f)
        {
            SetUpFuncCall();
            ExecuteFunc(f);
        }

        public void CallFunc<T1>(ModuleFunc f, T1 arg1)
            where T1 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg1);
            ExecuteFunc(f);
        }

        public void CallFunc<T1, T2>(ModuleFunc f, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg2);
            machine.Push(arg1);
            ExecuteFunc(f);
        }

        public void CallFunc<T1, T2, T3>(ModuleFunc f, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            ExecuteFunc(f);
        }

        public void CallFunc<T1, T2, T3, T4>(ModuleFunc f, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg4);
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            ExecuteFunc(f);
        }

        public void CallFunc<T1, T2, T3, T4, T5>(
            ModuleFunc f,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg5);
            machine.Push(arg4);
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            ExecuteFunc(f);
        }

        public R CallFunc<R>(ModuleFunc f)
            where R : unmanaged
        {
            SetUpFuncCall();
            return ExecuteFunc<R>(f);
        }

        public R CallFunc<R, T1>(ModuleFunc f, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg1);
            return ExecuteFunc<R>(f);
        }

        public R CallFunc<R, T1, T2>(ModuleFunc f, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg2);
            machine.Push(arg1);
            return ExecuteFunc<R>(f);
        }

        public R CallFunc<R, T1, T2, T3>(ModuleFunc f, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            return ExecuteFunc<R>(f);
        }

        public R CallFunc<R, T1, T2, T3, T4>(ModuleFunc f, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg4);
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            return ExecuteFunc<R>(f);
        }

        public R CallFunc<R, T1, T2, T3, T4, T5>(
            ModuleFunc f,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            SetUpFuncCall();
            machine.Push(arg5);
            machine.Push(arg4);
            machine.Push(arg3);
            machine.Push(arg2);
            machine.Push(arg1);
            return ExecuteFunc<R>(f);
        }

        public void CallExportedFunc(string name) =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc);

        public void CallExportedFunc<T1>(string name, T1 arg1)
            where T1 : unmanaged =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, arg1);

        public void CallExportedFunc<T1, T2>(string name, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, arg1, arg2);

        public void CallExportedFunc<T1, T2, T3>(string name, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, arg1, arg2, arg3);

        public void CallExportedFunc<T1, T2, T3, T4>(
            string name,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged =>
            CallFunc(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public void CallExportedFunc<T1, T2, T3, T4, T5>(
            string name,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged =>
            CallFunc(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public R CallExportedFunc<R>(string name)
            where R : unmanaged =>
            CallFunc<R>(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc);

        public R CallExportedFunc<R, T1>(string name, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged =>
            CallFunc<R, T1>(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, arg1);

        public R CallExportedFunc<R, T1, T2>(string name, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc<R, T1, T2>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2
            );

        public R CallExportedFunc<R, T1, T2, T3>(string name, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc<R, T1, T2, T3>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2,
                arg3
            );

        public R CallExportedFunc<R, T1, T2, T3, T4>(
            string name,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged =>
            CallFunc<R, T1, T2, T3, T4>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public R CallExportedFunc<R, T1, T2, T3, T4, T5>(
            string name,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged =>
            CallFunc<R, T1, T2, T3, T4, T5>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public void CallIndirectFunc(int index) =>
            CallFunc(machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc);

        public void CallIndirectFunc<T1>(int index, T1 arg1)
            where T1 : unmanaged =>
            CallFunc(machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc, arg1);

        public void CallIndirectFunc<T1, T2>(int index, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc(machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc, arg1, arg2);

        public void CallIndirectFunc<T1, T2, T3>(int index, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3
            );

        public void CallIndirectFunc<T1, T2, T3, T4>(int index, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public void CallIndirectFunc<T1, T2, T3, T4, T5>(
            int index,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public R CallIndirectFunc<R>(int index)
            where R : unmanaged =>
            CallFunc<R>(machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc);

        public R CallIndirectFunc<R, T1>(int index, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged =>
            CallFunc<R, T1>(machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc, arg1);

        public R CallIndirectFunc<R, T1, T2>(int index, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc<R, T1, T2>(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2
            );

        public R CallIndirectFunc<R, T1, T2, T3>(int index, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc<R, T1, T2, T3>(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3
            );

        public R CallIndirectFunc<R, T1, T2, T3, T4>(int index, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged =>
            CallFunc<R, T1, T2, T3, T4>(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public R CallIndirectFunc<R, T1, T2, T3, T4, T5>(
            int index,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged =>
            CallFunc<R, T1, T2, T3, T4, T5>(
                machine.GetFunc(GetWasmTableEntry(index).RefAddr) as ModuleFunc,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        //
        // Exports from WASM
        //

        public Table __indirect_function_table()
        {
            return machine.GetTable(machine.MainModuleName, "__indirect_function_table");
        }

        // Called for a C++ program. Runs the main function after calling setup stuff.
        public void _start() => CallExportedFunc("_start");

        // Called for a C program, before calling main.
        public void __wasm_call_ctors()
        {
            Func f = machine.GetFunc(machine.MainModuleName, "__wasm_call_ctors");
            machine.InvokeExpr(f as ModuleFunc);
        }

        // Called for a C program. Runs the main function.
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
        public int main(int argc, int argvPtr) =>
            CallExportedFunc<int, int, int>("main", argc, argvPtr);

        // Returns the location in the heap for the global errno variable.
        public int __errno_location() => CallExportedFunc<int>("__errno_location");

        public int fflush(int fd) => CallExportedFunc<int, int>("fflush", fd);

        public void free(int ptr) => CallExportedFunc<int>("free", ptr);

        public void setThrew(int a, int b) => CallExportedFunc<int, int>("setThrew", a, b);

        public void setTempRet0(int a) => CallExportedFunc<int>("setTempRet0", a);

        public void emscripten_stack_init() => CallExportedFunc("emscripten_stack_init");

        public int emscripten_stack_get_free() =>
            CallExportedFunc<int>("emscripten_stack_get_free");

        public int emscripten_stack_get_base() =>
            CallExportedFunc<int>("emscripten_stack_get_base");

        public int emscripten_stack_get_end() => CallExportedFunc<int>("emscripten_stack_get_end");

        public int emscripten_stack_get_current() =>
            CallExportedFunc<int>("emscripten_stack_get_current");

        public int stackSave() => CallExportedFunc<int>("stackSave");

        public void stackRestore(int ptr) => CallExportedFunc<int>("stackRestore", ptr);

        public int stackAlloc(int size) => CallExportedFunc<int, int>("stackAlloc", size);

        public void __cxa_free_exception(int excPtr) =>
            CallExportedFunc<int>("__cxa_free_exception", excPtr);

        public void __cxa_increment_exception_refcount(int excPtr) =>
            CallExportedFunc<int>("__cxa_increment_exception_refcount", excPtr);

        public void __cxa_decrement_exception_refcount(int excPtr) =>
            CallExportedFunc<int>("__cxa_decrement_exception_refcount", excPtr);

        public void __get_exception_message(int excPtr, int typePtrPtr, int msgPtrPtr) =>
            CallExportedFunc<int, int, int>(
                "__get_exception_message",
                excPtr,
                typePtrPtr,
                msgPtrPtr
            );

        public int __cxa_can_catch(int caughtType, int thrownType, int adjusted_ptrPtr) =>
            CallExportedFunc<int, int, int, int>(
                "__cxa_can_catch",
                caughtType,
                thrownType,
                adjusted_ptrPtr
            );

        public int __cxa_is_pointer_type(int type) =>
            CallExportedFunc<int, int>("__cxa_is_pointer_type", type);

        // This was present in hello_world.c. It returns a long, but the actual return value
        // is just the low 32 bits. The upper 32 bits get stored in $global1 (although we don't
        // yet support exported globals).
        public int dynCall_jiji(int index, int a, int b_lo, int b_hi, int d) =>
            CallExportedFunc<int, int, int, int, int, int>("dynCall_jiji", index, a, b_lo, b_hi, d);

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
        public void invoke_v(int index)
        {
            int sp = stackSave();
            try
            {
                CallIndirectFunc(index);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
            }
        }

        public void invoke_vi(int index, int a0)
        {
            int sp = stackSave();
            try
            {
                CallIndirectFunc<int>(index, a0);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
            }
        }

        public void invoke_vii(int index, int a0, int a1)
        {
            int sp = stackSave();
            try
            {
                CallIndirectFunc<int, int>(index, a0, a1);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
            }
        }

        public int invoke_i(int index)
        {
            int sp = stackSave();
            try
            {
                return CallIndirectFunc<int>(index);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
                // In the JavaScript version, the function doesn't have an explicit return, which
                // means it returns undefined.
                return 0; // ????
            }
        }

        public int invoke_ii(int index, int a0)
        {
            int sp = stackSave();
            try
            {
                return CallIndirectFunc<int, int>(index, a0);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
                return 0; // ????
            }
        }

        public int invoke_iii(int index, int a0, int a1)
        {
            int sp = stackSave();
            try
            {
                return CallIndirectFunc<int, int, int>(index, a0, a1);
            }
            catch (LongjmpException)
            {
                stackRestore(sp);
                setThrew(1, 0);
                return 0; // ????
            }
        }
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
