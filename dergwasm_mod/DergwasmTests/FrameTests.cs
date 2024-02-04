using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Derg.Wasm;
using Derg.Runtime;
using Elements.Core;
using FrooxEngine;
using Xunit;
using Derg;
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
            f.Push(2);
            f.Push(3);

            var expressions = new List<Expression>();

            MethodInfo method = typeof(Frame)
                .GetMethods()
                .Where(m => m.Name == "Pop")
                .Where(m => m.GetParameters().Length == 1)
                .Where(m => m.GetParameters()[0].IsOut)
                .Where(
                    m => m.GetParameters()[0].ParameterType.Name == typeof(int).MakeByRefType().Name
                )
                .First();

            var locals = new List<ParameterExpression>();
            var popperCallers = new List<MethodCallExpression>();

            ParameterExpression frame = Expression.Variable(typeof(Frame), "frame");
            ParameterExpression x = Expression.Variable(typeof(int), "x");
            ParameterExpression y = Expression.Variable(typeof(int), "y");
            locals.Add(frame);
            locals.Add(x);
            locals.Add(y);

            MethodCallExpression callx = Expression.Call(frame, method, x);
            MethodCallExpression cally = Expression.Call(frame, method, y);
            popperCallers.Add(callx);
            popperCallers.Add(cally);

            BlockExpression block = Expression.Block(
                new[] { x, y },
                callx,
                cally,
                Expression.AddAssign(x, y),
                x
            );

            var thing = Expression.Lambda<Func<Frame, int>>(block, frame).Compile();

            Assert.Equal(5, thing(f));
        }
    }
}
