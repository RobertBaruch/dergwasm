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
        public Value GetWasmTableEntry(int index, Frame frame)
        {
            Table wasmTable = __indirect_function_table(frame);
            if (index < 0 || index >= wasmTable.Elements.Length)
            {
                throw new Trap($"__indirect_function_table index out of bounds: {index}");
            }
            return wasmTable.Elements[index];
        }

        public void CallFunc(ModuleFunc f, Frame frame)
        {
            frame.InvokeFunc(machine, f);
        }

        public void CallFunc<T1>(ModuleFunc f, Frame frame, T1 arg1)
            where T1 : unmanaged
        {
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public void CallFunc<T1, T2>(ModuleFunc f, Frame frame, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public void CallFunc<T1, T2, T3>(ModuleFunc f, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public void CallFunc<T1, T2, T3, T4>(
            ModuleFunc f,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            frame.Push(arg4);
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public void CallFunc<T1, T2, T3, T4, T5>(
            ModuleFunc f,
            Frame frame,
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
            frame.Push(arg5);
            frame.Push(arg4);
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public R CallFunc<R>(ModuleFunc f, Frame frame)
            where R : unmanaged
        {
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public R CallFunc<R, T1>(ModuleFunc f, Frame frame, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged
        {
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public R CallFunc<R, T1, T2>(ModuleFunc f, Frame frame, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
        {
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public R CallFunc<R, T1, T2, T3>(ModuleFunc f, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public R CallFunc<R, T1, T2, T3, T4>(
            ModuleFunc f,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            frame.Push(arg4);
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public R CallFunc<R, T1, T2, T3, T4, T5>(
            ModuleFunc f,
            Frame frame,
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
            frame.Push(arg5);
            frame.Push(arg4);
            frame.Push(arg3);
            frame.Push(arg2);
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public void CallExportedFunc(string name, Frame frame) =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, frame);

        public void CallExportedFunc<T1>(string name, Frame frame, T1 arg1)
            where T1 : unmanaged =>
            CallFunc(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, frame, arg1);

        public void CallExportedFunc<T1, T2>(string name, Frame frame, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                frame,
                arg1,
                arg2
            );

        public void CallExportedFunc<T1, T2, T3>(
            string name,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3
            );

        public void CallExportedFunc<T1, T2, T3, T4>(
            string name,
            Frame frame,
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
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public void CallExportedFunc<T1, T2, T3, T4, T5>(
            string name,
            Frame frame,
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
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public R CallExportedFunc<R>(string name, Frame frame)
            where R : unmanaged =>
            CallFunc<R>(machine.GetFunc(machine.MainModuleName, name) as ModuleFunc, frame);

        public R CallExportedFunc<R, T1>(string name, Frame frame, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged =>
            CallFunc<R, T1>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                frame,
                arg1
            );

        public R CallExportedFunc<R, T1, T2>(string name, Frame frame, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc<R, T1, T2>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                frame,
                arg1,
                arg2
            );

        public R CallExportedFunc<R, T1, T2, T3>(
            string name,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc<R, T1, T2, T3>(
                machine.GetFunc(machine.MainModuleName, name) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3
            );

        public R CallExportedFunc<R, T1, T2, T3, T4>(
            string name,
            Frame frame,
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
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public R CallExportedFunc<R, T1, T2, T3, T4, T5>(
            string name,
            Frame frame,
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
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public void CallIndirectFunc(int index, Frame frame) =>
            CallFunc(machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc, frame);

        public void CallIndirectFunc<T1>(int index, Frame frame, T1 arg1)
            where T1 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1
            );

        public void CallIndirectFunc<T1, T2>(int index, Frame frame, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2
            );

        public void CallIndirectFunc<T1, T2, T3>(int index, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3
            );

        public void CallIndirectFunc<T1, T2, T3, T4>(
            int index,
            Frame frame,
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
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public void CallIndirectFunc<T1, T2, T3, T4, T5>(
            int index,
            Frame frame,
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
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public R CallIndirectFunc<R>(int index, Frame frame)
            where R : unmanaged =>
            CallFunc<R>(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame
            );

        public R CallIndirectFunc<R, T1>(int index, Frame frame, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged =>
            CallFunc<R, T1>(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1
            );

        public R CallIndirectFunc<R, T1, T2>(int index, Frame frame, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            CallFunc<R, T1, T2>(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2
            );

        public R CallIndirectFunc<R, T1, T2, T3>(int index, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            CallFunc<R, T1, T2, T3>(
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3
            );

        public R CallIndirectFunc<R, T1, T2, T3, T4>(
            int index,
            Frame frame,
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
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public R CallIndirectFunc<R, T1, T2, T3, T4, T5>(
            int index,
            Frame frame,
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
                machine.GetFunc(GetWasmTableEntry(index, frame).RefAddr) as ModuleFunc,
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        //
        // Exports from WASM
        //

        public Table __indirect_function_table(Frame frame)
        {
            return machine.GetTable(machine.MainModuleName, "__indirect_function_table");
        }

        // Called for a C++ program. Runs the main function after calling setup stuff.
        public void _start(Frame frame) => CallExportedFunc("_start", frame);

        // Called for a C program, before calling main.
        public void __wasm_call_ctors(Frame frame)
        {
            Func f = machine.GetFunc(machine.MainModuleName, "__wasm_call_ctors");
            CallFunc(f as ModuleFunc, frame);
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
        public int main(Frame frame, int argc, int argvPtr) =>
            CallExportedFunc<int, int, int>("main", frame, argc, argvPtr);

        // Returns the location in the heap for the global errno variable.
        public int __errno_location(Frame frame) =>
            CallExportedFunc<int>("__errno_location", frame);

        public int fflush(Frame frame, int fd) => CallExportedFunc<int, int>("fflush", frame, fd);

        public void free(Frame frame, int ptr) => CallExportedFunc<int>("free", frame, ptr);

        public void setThrew(Frame frame, int a, int b) =>
            CallExportedFunc<int, int>("setThrew", frame, a, b);

        public void setTempRet0(Frame frame, int a) =>
            CallExportedFunc<int>("setTempRet0", frame, a);

        public void emscripten_stack_init(Frame frame) =>
            CallExportedFunc("emscripten_stack_init", frame);

        public int emscripten_stack_get_free(Frame frame) =>
            CallExportedFunc<int>("emscripten_stack_get_free", frame);

        public int emscripten_stack_get_base(Frame frame) =>
            CallExportedFunc<int>("emscripten_stack_get_base", frame);

        public int emscripten_stack_get_end(Frame frame) =>
            CallExportedFunc<int>("emscripten_stack_get_end", frame);

        public int emscripten_stack_get_current(Frame frame) =>
            CallExportedFunc<int>("emscripten_stack_get_current", frame);

        public int stackSave(Frame frame) => CallExportedFunc<int>("stackSave", frame);

        public void stackRestore(Frame frame, int ptr) =>
            CallExportedFunc<int>("stackRestore", frame, ptr);

        public int stackAlloc(Frame frame, int size) =>
            CallExportedFunc<int, int>("stackAlloc", frame, size);

        public void __cxa_free_exception(Frame frame, int excPtr) =>
            CallExportedFunc<int>("__cxa_free_exception", frame, excPtr);

        public void __cxa_increment_exception_refcount(Frame frame, int excPtr) =>
            CallExportedFunc<int>("__cxa_increment_exception_refcount", frame, excPtr);

        public void __cxa_decrement_exception_refcount(Frame frame, int excPtr) =>
            CallExportedFunc<int>("__cxa_decrement_exception_refcount", frame, excPtr);

        public void __get_exception_message(
            Frame frame,
            int excPtr,
            int typePtrPtr,
            int msgPtrPtr
        ) =>
            CallExportedFunc<int, int, int>(
                "__get_exception_message",
                frame,
                excPtr,
                typePtrPtr,
                msgPtrPtr
            );

        public int __cxa_can_catch(
            Frame frame,
            int caughtType,
            int thrownType,
            int adjusted_ptrPtr
        ) =>
            CallExportedFunc<int, int, int, int>(
                "__cxa_can_catch",
                frame,
                caughtType,
                thrownType,
                adjusted_ptrPtr
            );

        public int __cxa_is_pointer_type(Frame frame, int type) =>
            CallExportedFunc<int, int>("__cxa_is_pointer_type", frame, type);

        // This was present in hello_world.c. It returns a long, but the actual return value
        // is just the low 32 bits. The upper 32 bits get stored in $global1 (although we don't
        // yet support exported globals).
        public int dynCall_jiji(Frame frame, int index, int a, int b_lo, int b_hi, int d) =>
            CallExportedFunc<int, int, int, int, int, int>(
                "dynCall_jiji",
                frame,
                index,
                a,
                b_lo,
                b_hi,
                d
            );

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
        //public void invoke_v(int index)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        CallIndirectFunc(index);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //    }
        //}

        //public void invoke_vi(int index, int a0)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        CallIndirectFunc<int>(index, a0);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //    }
        //}

        //public void invoke_vii(int index, int a0, int a1)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        CallIndirectFunc<int, int>(index, a0, a1);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //    }
        //}

        //public int invoke_i(int index)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        return CallIndirectFunc<int>(index);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //        // In the JavaScript version, the function doesn't have an explicit return, which
        //        // means it returns undefined.
        //        return 0; // ????
        //    }
        //}

        //public int invoke_ii(int index, int a0)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        return CallIndirectFunc<int, int>(index, a0);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //        return 0; // ????
        //    }
        //}

        //public int invoke_iii(int index, int a0, int a1)
        //{
        //    int sp = stackSave();
        //    try
        //    {
        //        return CallIndirectFunc<int, int, int>(index, a0, a1);
        //    }
        //    catch (LongjmpException)
        //    {
        //        stackRestore(sp);
        //        setThrew(1, 0);
        //        return 0; // ????
        //    }
        //}
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
        public int GetExceptionPtr(Frame frame)
        {
            // Work around a fastcomp bug, this code is still included for some reason in a build without
            // exceptions support.
            if (env.__cxa_is_pointer_type(frame, Type) != 0)
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
