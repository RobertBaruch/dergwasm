using System;
using System.Collections.Generic;
using System.Linq;
using Derg.Wasm;
using Derg.Runtime;
using FrooxEngine;
using Xunit;
using System.Reflection;
using System.Linq.Expressions;

namespace DergwasmTests
{
    public class FrameTests
    {
        [Fact]
        public void TestFrame()
        {
            Frame f = new Frame(null, null, null);
            f.Push(2UL);
            f.Push(3UL);

            var expressions = new List<Expression>();

            MethodInfo method = typeof(Frame)
                .GetMethods()
                .Where(m => m.Name == "Pop")
                .Where(m => m.GetParameters().Length == 1)
                .Where(m => m.GetParameters()[0].IsOut)
                .Where(
                    m =>
                        m.GetParameters()[0].ParameterType.Name
                        == typeof(WasmRefID<Slot>).MakeByRefType().Name
                )
                .First();

            Type t = typeof(WasmRefID<Slot>);
            Type innerType = t.GetGenericArguments()[0];

            var locals = new List<ParameterExpression>();
            var popperCallers = new List<MethodCallExpression>();

            ParameterExpression frame = Expression.Variable(typeof(Frame), "frame");
            ParameterExpression x = Expression.Variable(t, "x");
            ParameterExpression y = Expression.Variable(t, "y");
            locals.Add(frame);
            locals.Add(x);
            locals.Add(y);

            MethodCallExpression callx = Expression.Call(
                frame,
                method.MakeGenericMethod(innerType),
                x
            );
            MethodCallExpression cally = Expression.Call(
                frame,
                method.MakeGenericMethod(innerType),
                y
            );
            popperCallers.Add(callx);
            popperCallers.Add(cally);

            BlockExpression block = Expression.Block(new[] { x, y }, callx, cally, x);

            var thing = Expression.Lambda<Func<Frame, WasmRefID<Slot>>>(block, frame).Compile();

            Assert.Equal(3UL, thing(f).Id);
        }
    }
}
