using System;

namespace Derg
{
    public class HostProxy
    {
        public virtual void Invoke(IMachine machine) { }
    }

    public class VoidHostProxy : HostProxy
    {
        Action func;

        public VoidHostProxy(Action func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            func();
        }
    }

    public class HostProxy<T1> : HostProxy
        where T1 : unmanaged
    {
        Action<T1> func;

        public HostProxy(Action<T1> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T1 arg1 = machine.Pop<T1>();
            func(arg1);
        }
    }

    public class HostProxy<T1, T2> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
    {
        Action<T1, T2> func;

        public HostProxy(Action<T1, T2> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            func(arg1, arg2);
        }
    }

    public class HostProxy<T1, T2, T3> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        Action<T1, T2, T3> func;

        public HostProxy(Action<T1, T2, T3> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            func(arg1, arg2, arg3);
        }
    }

    public class HostProxy<T1, T2, T3, T4> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
    {
        Action<T1, T2, T3, T4> func;

        public HostProxy(Action<T1, T2, T3, T4> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            func(arg1, arg2, arg3, arg4);
        }
    }

    public class HostProxy<T1, T2, T3, T4, T5> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
    {
        Action<T1, T2, T3, T4, T5> func;

        public HostProxy(Action<T1, T2, T3, T4, T5> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T5 arg5 = machine.Pop<T5>();
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            func(arg1, arg2, arg3, arg4, arg5);
        }
    }

    public class HostProxy<T1, T2, T3, T4, T5, T6> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where T6 : unmanaged
    {
        Action<T1, T2, T3, T4, T5, T6> func;

        public HostProxy(Action<T1, T2, T3, T4, T5, T6> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T6 arg6 = machine.Pop<T6>();
            T5 arg5 = machine.Pop<T5>();
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            func(arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }

    public class ReturningVoidHostProxy<R> : HostProxy
        where R : unmanaged
    {
        Func<R> func;

        public ReturningVoidHostProxy(Func<R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            machine.Push(func());
        }
    }

    public class ReturningHostProxy<T1, R> : HostProxy
        where T1 : unmanaged
        where R : unmanaged
    {
        Func<T1, R> func;

        public ReturningHostProxy(Func<T1, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1));
        }
    }

    public class ReturningHostProxy<T1, T2, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where R : unmanaged
    {
        Func<T1, T2, R> func;

        public ReturningHostProxy(Func<T1, T2, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1, arg2));
        }
    }

    public class ReturningHostProxy<T1, T2, T3, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where R : unmanaged
    {
        Func<T1, T2, T3, R> func;

        public ReturningHostProxy(Func<T1, T2, T3, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1, arg2, arg3));
        }
    }

    public class ReturningHostProxy<T1, T2, T3, T4, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where R : unmanaged
    {
        Func<T1, T2, T3, T4, R> func;

        public ReturningHostProxy(Func<T1, T2, T3, T4, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1, arg2, arg3, arg4));
        }
    }

    public class ReturningHostProxy<T1, T2, T3, T4, T5, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where R : unmanaged
    {
        Func<T1, T2, T3, T4, T5, R> func;

        public ReturningHostProxy(Func<T1, T2, T3, T4, T5, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T5 arg5 = machine.Pop<T5>();
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1, arg2, arg3, arg4, arg5));
        }
    }

    public class ReturningHostProxy<T1, T2, T3, T4, T5, T6, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where T6 : unmanaged
        where R : unmanaged
    {
        Func<T1, T2, T3, T4, T5, T6, R> func;

        public ReturningHostProxy(Func<T1, T2, T3, T4, T5, T6, R> func)
        {
            this.func = func;
        }

        public override void Invoke(IMachine machine)
        {
            T6 arg6 = machine.Pop<T6>();
            T5 arg5 = machine.Pop<T5>();
            T4 arg4 = machine.Pop<T4>();
            T3 arg3 = machine.Pop<T3>();
            T2 arg2 = machine.Pop<T2>();
            T1 arg1 = machine.Pop<T1>();
            machine.Push(func(arg1, arg2, arg3, arg4, arg5, arg6));
        }
    }
}
