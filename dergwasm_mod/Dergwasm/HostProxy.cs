using System;

namespace Derg
{
    public class HostProxy
    {
        // Invokes a host function. The given frame contains the arguments to the function in its locals,
        // and the return value will be pushed onto the frame's stack.
        public virtual void Invoke(Machine machine, Frame frame) { }

        public virtual int NumArgs() => 0;

        public virtual int Arity() => 0;
    }

    public class VoidHostProxy : HostProxy
    {
        Action<Frame> func;

        public VoidHostProxy(Action<Frame> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(frame);
        }
    }

    public class HostProxy<T1> : HostProxy
        where T1 : unmanaged
    {
        Action<Frame, T1> func;

        public HostProxy(Action<Frame, T1> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(frame, frame.Locals[0].As<T1>());
        }

        public override int NumArgs() => 1;
    }

    public class HostProxy<T1, T2> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
    {
        Action<Frame, T1, T2> func;

        public HostProxy(Action<Frame, T1, T2> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(frame, frame.Locals[0].As<T1>(), frame.Locals[1].As<T2>());
        }

        public override int NumArgs() => 2;
    }

    public class HostProxy<T1, T2, T3> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        Action<Frame, T1, T2, T3> func;

        public HostProxy(Action<Frame, T1, T2, T3> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(
                frame,
                frame.Locals[0].As<T1>(),
                frame.Locals[1].As<T2>(),
                frame.Locals[2].As<T3>()
            );
        }

        public override int NumArgs() => 3;
    }

    public class HostProxy<T1, T2, T3, T4> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
    {
        Action<Frame, T1, T2, T3, T4> func;

        public HostProxy(Action<Frame, T1, T2, T3, T4> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(
                frame,
                frame.Locals[0].As<T1>(),
                frame.Locals[1].As<T2>(),
                frame.Locals[2].As<T3>(),
                frame.Locals[3].As<T4>()
            );
        }

        public override int NumArgs() => 4;
    }

    public class HostProxy<T1, T2, T3, T4, T5> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
    {
        Action<Frame, T1, T2, T3, T4, T5> func;

        public HostProxy(Action<Frame, T1, T2, T3, T4, T5> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(
                frame,
                frame.Locals[0].As<T1>(),
                frame.Locals[1].As<T2>(),
                frame.Locals[2].As<T3>(),
                frame.Locals[3].As<T4>(),
                frame.Locals[4].As<T5>()
            );
        }

        public override int NumArgs() => 5;
    }

    public class HostProxy<T1, T2, T3, T4, T5, T6> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where T6 : unmanaged
    {
        Action<Frame, T1, T2, T3, T4, T5, T6> func;

        public HostProxy(Action<Frame, T1, T2, T3, T4, T5, T6> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            func(
                frame,
                frame.Locals[0].As<T1>(),
                frame.Locals[1].As<T2>(),
                frame.Locals[2].As<T3>(),
                frame.Locals[3].As<T4>(),
                frame.Locals[4].As<T5>(),
                frame.Locals[5].As<T6>()
            );
        }

        public override int NumArgs() => 6;
    }

    public class ReturningVoidHostProxy<R> : HostProxy
        where R : unmanaged
    {
        Func<Frame, R> func;

        public ReturningVoidHostProxy(Func<Frame, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(func(frame));
        }

        public override int Arity() => 1;
    }

    public class ReturningHostProxy<T1, R> : HostProxy
        where T1 : unmanaged
        where R : unmanaged
    {
        Func<Frame, T1, R> func;

        public ReturningHostProxy(Func<Frame, T1, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(func(frame, frame.Locals[0].As<T1>()));
        }

        public override int NumArgs() => 1;

        public override int Arity() => 1;
    }

    public class ReturningHostProxy<T1, T2, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where R : unmanaged
    {
        Func<Frame, T1, T2, R> func;

        public ReturningHostProxy(Func<Frame, T1, T2, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(func(frame, frame.Locals[0].As<T1>(), frame.Locals[1].As<T2>()));
        }

        public override int NumArgs() => 2;

        public override int Arity() => 1;
    }

    public class ReturningHostProxy<T1, T2, T3, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where R : unmanaged
    {
        Func<Frame, T1, T2, T3, R> func;

        public ReturningHostProxy(Func<Frame, T1, T2, T3, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(
                func(
                    frame,
                    frame.Locals[0].As<T1>(),
                    frame.Locals[1].As<T2>(),
                    frame.Locals[2].As<T3>()
                )
            );
        }

        public override int NumArgs() => 3;

        public override int Arity() => 1;
    }

    public class ReturningHostProxy<T1, T2, T3, T4, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where R : unmanaged
    {
        Func<Frame, T1, T2, T3, T4, R> func;

        public ReturningHostProxy(Func<Frame, T1, T2, T3, T4, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(
                func(
                    frame,
                    frame.Locals[0].As<T1>(),
                    frame.Locals[1].As<T2>(),
                    frame.Locals[2].As<T3>(),
                    frame.Locals[3].As<T4>()
                )
            );
        }

        public override int NumArgs() => 4;

        public override int Arity() => 1;
    }

    public class ReturningHostProxy<T1, T2, T3, T4, T5, R> : HostProxy
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where R : unmanaged
    {
        Func<Frame, T1, T2, T3, T4, T5, R> func;

        public ReturningHostProxy(Func<Frame, T1, T2, T3, T4, T5, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(
                func(
                    frame,
                    frame.Locals[0].As<T1>(),
                    frame.Locals[1].As<T2>(),
                    frame.Locals[2].As<T3>(),
                    frame.Locals[3].As<T4>(),
                    frame.Locals[4].As<T5>()
                )
            );
        }

        public override int NumArgs() => 5;

        public override int Arity() => 1;
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
        Func<Frame, T1, T2, T3, T4, T5, T6, R> func;

        public ReturningHostProxy(Func<Frame, T1, T2, T3, T4, T5, T6, R> func)
        {
            this.func = func;
        }

        public override void Invoke(Machine machine, Frame frame)
        {
            frame.Push(
                func(
                    frame,
                    frame.Locals[0].As<T1>(),
                    frame.Locals[1].As<T2>(),
                    frame.Locals[2].As<T3>(),
                    frame.Locals[3].As<T4>(),
                    frame.Locals[4].As<T5>(),
                    frame.Locals[5].As<T6>()
                )
            );
        }

        public override int NumArgs() => 6;

        public override int Arity() => 1;
    }
}
