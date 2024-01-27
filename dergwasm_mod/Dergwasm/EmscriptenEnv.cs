using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;

namespace Derg
{
    // Exception thrown when longjmp is called in a C program. The WASM code and the EmscriptenEnv
    // work together to implement setjmp/longjmp functionality.
    public class LongjmpException : Exception
    {
        public LongjmpException()
            : base() { }
    }

    public static class Errno
    {
        public const int ENOENT = 2; // No such file or directory
        public const int EBADF = 9; // Bad file descriptor
        public const int EACCES = 13; // Permission denied
        public const int EFAULT = 14; // Bad address
        public const int ENOTDIR = 20; // Not a directory
        public const int EINVAL = 22; // Invalid argument
        public const int EFBIG = 27; // File too large
        public const int ERANGE = 34; // Math result not representable
    }

    // Host environment expected by Emscripten.
    public class EmscriptenEnv
    {
        public Machine machine;
        public Action<string> outputWriter = null;

        public EmscriptenEnv(Machine machine)
        {
            this.machine = machine;
        }

        // Creates an empty frame which can be used to call a WASM function, if you weren't
        // already in a frame. Specify the ModuleFunc if you are going to use this to call
        // a WASM function.
        public Frame EmptyFrame(ModuleFunc f = null)
        {
            Frame frame = new Frame(f, machine.mainModuleInstance, null);
            frame.Label = new Label(0, 0);
            return frame;
        }

        // Allocates `size` bytes in WASM memory and returns the pointer to it.
        // Virtual for testing.
        public virtual int Malloc(Frame frame, int size)
        {
            if (frame == null)
                frame = EmptyFrame();

            return malloc(frame, size);
        }

        public void Free(Frame frame, int ptr)
        {
            if (frame == null)
                frame = EmptyFrame();

            free(frame, ptr);
        }

        // Allocates a UTF-8 encoded string in WASM memory in length-data format
        // and returns the pointer to it. You can pass null as the frame if you're
        // calling this from outside a WASM function. Otherwise pass the frame you're in.
        public int AllocateUTF8StringInMemLenData(Frame frame, string s)
        {
            int stringLen = Encoding.UTF8.GetByteCount(s);
            int stringPtr = Malloc(frame, 4 + stringLen);
            machine.HeapSet<int>(stringPtr, stringLen);
            WriteUTF8StringToMem(
                stringPtr + 4,
                s,
                false /* nullTerminate */
            );
            return stringPtr;
        }

        // Allocates a UTF-8 encoded string in WASM memory and returns the pointer to it.
        // You can pass null as the frame if you're calling this from outside a WASM function.
        // Otherwise pass the frame you're in.
        public int AllocateUTF8StringInMem(Frame frame, string s)
        {
            return AllocateUTF8StringInMemWithPadding(frame, 0, s, out _);
        }

        public int AllocateUTF8StringInMem(Frame frame, string s, out int allocated_size)
        {
            return AllocateUTF8StringInMemWithPadding(frame, 0, s, out allocated_size);
        }

        // Allocates a UTF-8 encoded string in WASM memory, with padding *before* the string.
        // You can pass null as the frame if you're calling this from outside a WASM function.
        // Otherwise pass the frame you're in.
        public int AllocateUTF8StringInMemWithPadding(
            Frame frame,
            int padding,
            string s,
            out int allocated_size
        )
        {
            int stringPtr = Malloc(frame, padding + Encoding.UTF8.GetByteCount(s) + 1);
            int len = WriteUTF8StringToMem(stringPtr + padding, s);
            allocated_size = padding + len;
            return stringPtr;
        }

        public string GetUTF8StringFromMem(int ptr)
        {
            int endPtr = ptr;
            while (machine.Heap[endPtr] != 0)
            {
                endPtr++;
            }
            return Encoding.UTF8.GetString(machine.Heap, ptr, endPtr - ptr);
        }

        public string GetUTF8StringFromMem(int ptr, uint len)
        {
            return Encoding.UTF8.GetString(machine.Heap, ptr, (int)len);
        }

        // Writes a UTF-8 encoded string to the heap. Returns the number of bytes written.
        public int WriteUTF8StringToMem(int ptr, string s, bool nullTerminate = true)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(s);
            Buffer.BlockCopy(stringData, 0, machine.Heap, ptr, stringData.Length);
            machine.Heap[ptr + stringData.Length] = 0; // NUL-termination
            return stringData.Length + 1;
        }

        // Returns a funcref.
        Value GetWasmTableEntry(Frame frame, int index)
        {
            Table wasmTable = __indirect_function_table(frame);
            if (index < 0 || index >= wasmTable.Elements.Length)
            {
                throw new Trap($"__indirect_function_table index out of bounds: {index}");
            }
            return wasmTable.Elements[index];
        }

        //
        // The CallIndirectFuncs were here because at some point the invoke_* functions
        // seemed to access the __indirect_function_table rather than calling dyncall_*
        // directly. I am not sure what makes the difference. It's possible that Emscripten
        // uses an indirect function table for small numbers of functions, but splits them
        // up into dyncalls for larger numbers of functions. That's just speculation.
        //

        void CallIndirectFunc(int index, Frame frame) =>
            machine.CallFunc(machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr), frame);

        void CallIndirectFunc<T1>(int index, Frame frame, T1 arg1)
            where T1 : unmanaged =>
            machine.CallFunc(machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr), frame, arg1);

        void CallIndirectFunc<T1, T2>(int index, Frame frame, T1 arg1, T2 arg2)
            where T1 : unmanaged
            where T2 : unmanaged =>
            machine.CallFunc(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2
            );

        void CallIndirectFunc<T1, T2, T3>(int index, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            machine.CallFunc(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2,
                arg3
            );

        void CallIndirectFunc<T1, T2, T3, T4>(
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
            machine.CallFunc(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        void CallIndirectFunc<T1, T2, T3, T4, T5>(
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
            machine.CallFunc(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        R CallIndirectFunc<R>(int index, Frame frame)
            where R : unmanaged =>
            machine.CallFunc<R>(machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr), frame);

        R CallIndirectFunc<R, T1>(int index, Frame frame, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged =>
            machine.CallFunc<R, T1>(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1
            );

        R CallIndirectFunc<R, T1, T2>(int index, Frame frame, T1 arg1, T2 arg2)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            machine.CallFunc<R, T1, T2>(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2
            );

        R CallIndirectFunc<R, T1, T2, T3>(int index, Frame frame, T1 arg1, T2 arg2, T3 arg3)
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            machine.CallFunc<R, T1, T2, T3>(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2,
                arg3
            );

        R CallIndirectFunc<R, T1, T2, T3, T4>(
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
            machine.CallFunc<R, T1, T2, T3, T4>(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        R CallIndirectFunc<R, T1, T2, T3, T4, T5>(
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
            machine.CallFunc<R, T1, T2, T3, T4, T5>(
                machine.GetFunc(GetWasmTableEntry(frame, index).RefAddr),
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

        Table __indirect_function_table(Frame frame)
        {
            return machine.GetTable(machine.MainModuleName, "__indirect_function_table");
        }

        // Called for a C++ program. Runs the main function after calling setup stuff.
        public void _start(Frame frame) => machine.CallExportedFunc("_start", frame);

        // Called for a C program, before calling main.
        public void __wasm_call_ctors(Frame frame) =>
            machine.CallExportedFunc("__wasm_call_ctors", frame);

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
            machine.CallExportedFunc<int, int, int>("main", frame, argc, argvPtr);

        // Returns the location in the heap for the global errno variable.
        public int __errno_location(Frame frame) =>
            machine.CallExportedFunc<int>("__errno_location", frame);

        public int fflush(Frame frame, int fd) =>
            machine.CallExportedFunc<int, int>("fflush", frame, fd);

        public int malloc(Frame frame, int amt) =>
            machine.CallExportedFunc<int, int>("malloc", frame, amt);

        public void free(Frame frame, int ptr) => machine.CallExportedFunc<int>("free", frame, ptr);

        public void setThrew(Frame frame, int a, int b) =>
            machine.CallExportedFunc<int, int>("setThrew", frame, a, b);

        public void setTempRet0(Frame frame, int a) =>
            machine.CallExportedFunc<int>("setTempRet0", frame, a);

        public void emscripten_stack_init(Frame frame) =>
            machine.CallExportedFunc("emscripten_stack_init", frame);

        public int emscripten_stack_get_free(Frame frame) =>
            machine.CallExportedFunc<int>("emscripten_stack_get_free", frame);

        public int emscripten_stack_get_base(Frame frame) =>
            machine.CallExportedFunc<int>("emscripten_stack_get_base", frame);

        public int emscripten_stack_get_end(Frame frame) =>
            machine.CallExportedFunc<int>("emscripten_stack_get_end", frame);

        public int emscripten_stack_get_current(Frame frame) =>
            machine.CallExportedFunc<int>("emscripten_stack_get_current", frame);

        public int stackSave(Frame frame) => machine.CallExportedFunc<int>("stackSave", frame);

        public void stackRestore(Frame frame, int ptr) =>
            machine.CallExportedFunc<int>("stackRestore", frame, ptr);

        public int stackAlloc(Frame frame, int size) =>
            machine.CallExportedFunc<int, int>("stackAlloc", frame, size);

        //
        // Micropython-specific functions.
        //

        public void mp_sched_keyboard_interrupt(Frame frame) =>
            machine.CallExportedFunc("mp_sched_keyboard_interrupt", frame);

        public int mp_js_do_str(Frame frame, int a) =>
            machine.CallExportedFunc<int, int>("mp_js_do_str", frame, a);

        public int mp_js_process_char(Frame frame, int a) =>
            machine.CallExportedFunc<int, int>("mp_js_process_char", frame, a);

        public void mp_js_init(Frame frame, int a) =>
            machine.CallExportedFunc<int>("mp_js_init", frame, a);

        public void mp_js_init_repl(Frame frame) =>
            machine.CallExportedFunc("mp_js_init_repl", frame);

        // This was present in hello_world.c. It returns a long, but the actual return value
        // is just the low 32 bits. The upper 32 bits get stored in $global1 (although we don't
        // yet support exported globals).
        public long dynCall_jiji(Frame frame, int index, int a, long b, int d) =>
            machine.CallExportedFunc<long, int, int, long, int>(
                "dynCall_jiji",
                frame,
                index,
                a,
                b,
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
        public void dynCall_v(Frame frame, int index) =>
            machine.CallExportedFunc("dynCall_v", frame, index);

        public void dynCall_vi(Frame frame, int index, int a0) =>
            machine.CallExportedFunc("dynCall_vi", frame, index, a0);

        public void dynCall_vii(Frame frame, int index, int a0, int a1) =>
            machine.CallExportedFunc("dynCall_vii", frame, index, a0, a1);

        public void dynCall_viii(Frame frame, int index, int a0, int a1, int a2) =>
            machine.CallExportedFunc("dynCall_viii", frame, index, a0, a1, a2);

        public void dynCall_viiii(Frame frame, int index, int a0, int a1, int a2, int a3) =>
            machine.CallExportedFunc("dynCall_viiii", frame, index, a0, a1, a2, a3);

        public int dynCall_i(Frame frame, int index) =>
            machine.CallExportedFunc<int, int>("dynCall_i", frame, index);

        public int dynCall_ii(Frame frame, int index, int a0) =>
            machine.CallExportedFunc<int, int, int>("dynCall_ii", frame, index, a0);

        public int dynCall_iii(Frame frame, int index, int a0, int a1) =>
            machine.CallExportedFunc<int, int, int, int>("dynCall_iii", frame, index, a0, a1);

        public int dynCall_iiii(Frame frame, int index, int a0, int a1, int a2) =>
            machine.CallExportedFunc<int, int, int, int, int>(
                "dynCall_iiii",
                frame,
                index,
                a0,
                a1,
                a2
            );

        public int dynCall_iiiii(Frame frame, int index, int a0, int a1, int a2, int a3) =>
            machine.CallExportedFunc<int, int, int, int, int, int>(
                "dynCall_iiiii",
                frame,
                index,
                a0,
                a1,
                a2,
                a3
            );

        //
        // Imports to WASM
        //

        public void RegisterHostFuncs()
        {
            machine.RegisterVoidHostFunc<int, int, int>(
                "env",
                "emscripten_memcpy_js",
                emscripten_memcpy_js
            );
            machine.RegisterVoidHostFunc<int>("env", "exit", emscripten_exit);
            machine.RegisterReturningHostFunc<int, int>(
                "env",
                "emscripten_resize_heap",
                emscripten_resize_heap
            );
            machine.RegisterVoidHostFunc(
                "env",
                "_emscripten_throw_longjmp",
                _emscripten_throw_longjmp
            );
            machine.RegisterVoidHostFunc<int>(
                "env",
                "emscripten_scan_registers",
                emscripten_scan_registers
            );
            machine.RegisterReturningHostFunc<int, int>("env", "invoke_i", invoke_i);
            machine.RegisterReturningHostFunc<int, int, int>("env", "invoke_ii", invoke_ii);
            machine.RegisterReturningHostFunc<int, int, int, int>("env", "invoke_iii", invoke_iii);
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "env",
                "invoke_iiii",
                invoke_iiii
            );
            machine.RegisterReturningHostFunc<int, int, int, int, int, int>(
                "env",
                "invoke_iiiii",
                invoke_iiiii
            );
            machine.RegisterVoidHostFunc<int>("env", "invoke_v", invoke_v);
            machine.RegisterVoidHostFunc<int, int>("env", "invoke_vi", invoke_vi);
            machine.RegisterVoidHostFunc<int, int, int>("env", "invoke_vii", invoke_vii);
            machine.RegisterVoidHostFunc<int, int, int, int>("env", "invoke_viii", invoke_viii);
            machine.RegisterVoidHostFunc<int, int, int, int, int>(
                "env",
                "invoke_viiii",
                invoke_viiii
            );
            machine.RegisterVoidHostFunc("env", "mp_js_hook", mp_js_hook);
            machine.RegisterReturningHostFunc<int>("env", "mp_js_ticks_ms", mp_js_ticks_ms);
            machine.RegisterVoidHostFunc<int, int>("env", "mp_js_write", mp_js_write);
        }

        public void __assert_fail(
            int conditionStrPtr,
            int filenameStrPtr,
            int line,
            int funcStrPtr
        ) => throw new NotImplementedException();

        public void abort() => throw new NotImplementedException();

        public void _emscripten_throw_longjmp(Frame frame)
        {
            throw new LongjmpException();
        }

        public void emscripten_memcpy_js(Frame frame, int dest, int src, int len)
        {
            Console.WriteLine($"emscripten_memcpy_js({dest}, {src}, {len})");
            byte[] mem = machine.Heap;
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

        public void emscripten_scan_registers(Frame frame, int scanPtr)
        {
            Console.WriteLine($"emscripten_scan_registers({scanPtr})");
            // throw new NotImplementedException();
        }

        public int emscripten_resize_heap(Frame frame, int requestedSize) =>
            throw new NotImplementedException();

        public void emscripten_exit(Frame frame, int exit_code) => throw new ExitTrap(exit_code);

        // Implementation of exceptions when not supported in WASM.
        public int __cxa_begin_catch(int excPtr) => throw new NotImplementedException();

        public void __cxa_end_catch() => throw new NotImplementedException();

        public int __cxa_find_matching_catch_2() => throw new NotImplementedException();

        public int __cxa_find_matching_catch_3(int arg0) => throw new NotImplementedException();

        public void __cxa_throw(int excPtr, int type, int destructor) =>
            throw new NotImplementedException();

        public void __resumeException(int excPtr) => throw new NotImplementedException();

        //
        // Indirect function calls resulting from Emscripten's setjmp/longjmp implementation.
        //

        public void invoke_v(Frame frame, int index)
        {
            int sp = stackSave(frame);
            try
            {
                // CallIndirectFunc(index, frame);
                dynCall_v(frame, index);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
            }
        }

        public void invoke_vi(Frame frame, int index, int a0)
        {
            int sp = stackSave(frame);
            try
            {
                // CallIndirectFunc<int>(index, frame, a0);
                dynCall_vi(frame, index, a0);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
            }
        }

        public void invoke_vii(Frame frame, int index, int a0, int a1)
        {
            int sp = stackSave(frame);
            try
            {
                // CallIndirectFunc<int, int>(index, frame, a0, a1);
                dynCall_vii(frame, index, a0, a1);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
            }
        }

        public void invoke_viii(Frame frame, int index, int a0, int a1, int a2)
        {
            int sp = stackSave(frame);
            try
            {
                // CallIndirectFunc<int, int, int>(index, frame, a0, a1, a2);
                dynCall_viii(frame, index, a0, a1, a2);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
            }
        }

        public void invoke_viiii(Frame frame, int index, int a0, int a1, int a2, int a3)
        {
            int sp = stackSave(frame);
            try
            {
                // CallIndirectFunc<int, int, int, int>(index, frame, a0, a1, a2, a3);
                dynCall_viiii(frame, index, a0, a1, a2, a3);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
            }
        }

        public int invoke_i(Frame frame, int index)
        {
            int sp = stackSave(frame);
            try
            {
                // return CallIndirectFunc<int>(index, frame);
                return dynCall_i(frame, index);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
                return 0; // ????
            }
        }

        public int invoke_ii(Frame frame, int index, int a0)
        {
            int sp = stackSave(frame);
            try
            {
                // return CallIndirectFunc<int, int>(index, frame, a0);
                return dynCall_ii(frame, index, a0);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
                return 0; // ????
            }
        }

        public int invoke_iii(Frame frame, int index, int a0, int a1)
        {
            int sp = stackSave(frame);
            try
            {
                // return CallIndirectFunc<int, int, int>(index, frame, a0, a1);
                return dynCall_iii(frame, index, a0, a1);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
                return 0; // ????
            }
        }

        public int invoke_iiii(Frame frame, int index, int a0, int a1, int a2)
        {
            int sp = stackSave(frame);
            try
            {
                // return CallIndirectFunc<int, int, int, int>(index, frame, a0, a1, a2);
                return dynCall_iiii(frame, index, a0, a1, a2);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
                return 0; // ????
            }
        }

        public int invoke_iiiii(Frame frame, int index, int a0, int a1, int a2, int a3)
        {
            int sp = stackSave(frame);
            try
            {
                // return CallIndirectFunc<int, int, int, int, int>(index, frame, a0, a1, a2, a3);
                return dynCall_iiiii(frame, index, a0, a1, a2, a3);
            }
            catch (LongjmpException)
            {
                stackRestore(frame, sp);
                setThrew(frame, 1, 0);
                return 0; // ????
            }
        }

        //
        // MicroPython-specific functions.
        //

        // Seems to read a char from stdin, writing it to stdout, unless it's a ctrl-C, in which
        // case mp_sched_keyboard_interrupt() is called.
        public void mp_js_hook(Frame frame) { }

        // Returns the number of milliseconds since the interpreter started.
        public int mp_js_ticks_ms(Frame frame)
        {
            return 0;
        }

        // Writes a string to the console.
        public void mp_js_write(Frame frame, int ptr, int len)
        {
            byte[] data = new byte[len];
            Array.Copy(machine.Heap, ptr, data, 0, len);
            Console.WriteLine($"  MicroPython wrote: {System.Text.Encoding.UTF8.GetString(data)}");
            if (outputWriter != null)
            {
                outputWriter(System.Text.Encoding.UTF8.GetString(data));
            }
        }
    }
}
