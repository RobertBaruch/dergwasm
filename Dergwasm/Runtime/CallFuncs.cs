using Dergwasm.Runtime;

namespace Derg.Runtime
{
    // Extensions to Machine which make it easier to call WASM functions.
    //
    // Use machine.CallExportedFunc<T1, T2, ...>(name, frame, arg1, arg2, ...)
    // to call a void exported WASM function with the given name and arguments.
    // The frame argument is the frame in which the function will be called.
    //
    // For such void functions, you do not need to specify the argument types
    // in the generic type parameters. The compiler will infer them from the
    // arguments.
    //
    // Use machine.CallExportedFunc<R, T1, T2, ...>(name, frame, arg1, arg2, ...)
    // to call an exported WASM function with the given name and arguments and
    // returning a value of type R. The frame argument is the frame in which the
    // function will be called.
    //
    // For such functions, you must specify the return type and argument types
    // in the generic type parameters.
    public static class CallFuncExtensions
    {
        public static void CallFunc(this Machine machine, Func f, Frame frame)
        {
            frame.InvokeFunc(machine, f);
        }

        public static void CallFunc<T1>(this Machine machine, Func f, Frame frame, T1 arg1)
            where T1 : unmanaged
        {
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
        }

        public static void CallFunc<T1, T2>(
            this Machine machine,
            Func f,
            Frame frame,
            T1 arg1,
            T2 arg2
        )
            where T1 : unmanaged
            where T2 : unmanaged
        {
            frame.Push(arg1);
            frame.Push(arg2);
            frame.InvokeFunc(machine, f);
        }

        public static void CallFunc<T1, T2, T3>(
            this Machine machine,
            Func f,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.InvokeFunc(machine, f);
        }

        public static void CallFunc<T1, T2, T3, T4>(
            this Machine machine,
            Func f,
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
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.Push(arg4);
            frame.InvokeFunc(machine, f);
        }

        public static void CallFunc<T1, T2, T3, T4, T5>(
            this Machine machine,
            Func f,
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
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.Push(arg4);
            frame.Push(arg5);
            frame.InvokeFunc(machine, f);
        }

        public static R CallFunc<R>(this Machine machine, Func f, Frame frame)
            where R : unmanaged
        {
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static R CallFunc<R, T1>(this Machine machine, Func f, Frame frame, T1 arg1)
            where R : unmanaged
            where T1 : unmanaged
        {
            frame.Push(arg1);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static R CallFunc<R, T1, T2>(
            this Machine machine,
            Func f,
            Frame frame,
            T1 arg1,
            T2 arg2
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
        {
            frame.Push(arg1);
            frame.Push(arg2);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static R CallFunc<R, T1, T2, T3>(
            this Machine machine,
            Func f,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static R CallFunc<R, T1, T2, T3, T4>(
            this Machine machine,
            Func f,
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
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.Push(arg4);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static R CallFunc<R, T1, T2, T3, T4, T5>(
            this Machine machine,
            Func f,
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
            frame.Push(arg1);
            frame.Push(arg2);
            frame.Push(arg3);
            frame.Push(arg4);
            frame.Push(arg5);
            frame.InvokeFunc(machine, f);
            return frame.Pop<R>();
        }

        public static void CallExportedFunc(this Machine machine, string name, Frame frame) =>
            machine.CallFunc(machine.GetRequiredFunc(machine.MainModuleName, name), frame);

        public static void CallExportedFunc<T1>(
            this Machine machine,
            string name,
            Frame frame,
            T1 arg1
        )
            where T1 : unmanaged =>
            machine.CallFunc(machine.GetRequiredFunc(machine.MainModuleName, name), frame, arg1);

        public static void CallExportedFunc<T1, T2>(
            this Machine machine,
            string name,
            Frame frame,
            T1 arg1,
            T2 arg2
        )
            where T1 : unmanaged
            where T2 : unmanaged =>
            machine.CallFunc(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2
            );

        public static void CallExportedFunc<T1, T2, T3>(
            this Machine machine,
            string name,
            Frame frame,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged =>
            machine.CallFunc(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3
            );

        public static void CallExportedFunc<T1, T2, T3, T4>(
            this Machine machine,
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
            machine.CallFunc(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public static void CallExportedFunc<T1, T2, T3, T4, T5>(
            this Machine machine,
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
            machine.CallFunc(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );

        public static R CallExportedFunc<R>(this Machine machine, string name, Frame frame)
            where R : unmanaged =>
            machine.CallFunc<R>(machine.GetRequiredFunc(machine.MainModuleName, name), frame);

        public static R CallExportedFunc<R, T1>(
            this Machine machine,
            string name,
            Frame frame,
            T1 arg1
        )
            where R : unmanaged
            where T1 : unmanaged =>
            machine.CallFunc<R, T1>(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1
            );

        public static R CallExportedFunc<R, T1, T2>(
            this Machine machine,
            string name,
            Frame frame,
            T1 arg1,
            T2 arg2
        )
            where R : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged =>
            machine.CallFunc<R, T1, T2>(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2
            );

        public static R CallExportedFunc<R, T1, T2, T3>(
            this Machine machine,
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
            machine.CallFunc<R, T1, T2, T3>(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3
            );

        public static R CallExportedFunc<R, T1, T2, T3, T4>(
            this Machine machine,
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
            machine.CallFunc<R, T1, T2, T3, T4>(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3,
                arg4
            );

        public static R CallExportedFunc<R, T1, T2, T3, T4, T5>(
            this Machine machine,
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
            machine.CallFunc<R, T1, T2, T3, T4, T5>(
                machine.GetRequiredFunc(machine.MainModuleName, name),
                frame,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
    }
}
