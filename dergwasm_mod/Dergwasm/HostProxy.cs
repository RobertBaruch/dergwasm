using System;

namespace Derg
{
    public abstract class HostProxy
    {
        // Invokes a host function. The given frame contains the arguments to the function in its locals,
        // and the return value will be pushed onto the frame's stack.
        public abstract void Invoke(Machine machine, Frame frame);

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

    public class ActionHostProxy : HostProxy
    {
        private readonly Action<Machine, Frame> _method;

        private readonly int _numArgs;
        private readonly int _arity;

        public ActionHostProxy(Action<Machine, Frame> method, int numArgs, int arity)
        {
            _method = method;
            _numArgs = numArgs;
            _arity = arity;
        }

        public override void Invoke(Machine machine, Frame frame) => _method(machine, frame);

        public override int NumArgs() => _numArgs;

        public override int Arity() => _arity;
    }
}
