using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Derg;

namespace DergwasmTests
{
    // BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
    // 11th Gen Intel Core i7-11700K 3.60GHz, 1 CPU, 16 logical and 8 physical cores
    //  [Host]               : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
    //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method     | Mean      | Error     | StdDev    | Ratio |
    // |----------- |----------:|----------:|----------:|------:|
    // | GenericAdd | 2.1387 ns | 0.0603 ns | 0.0471 ns |  1.00 |
    // |            |           |           |           |       |
    // | ActualAdd  | 0.0306 ns | 0.0081 ns | 0.0076 ns |  1.00 |
    public static class Add<T>
    {
        public static readonly Func<T, T, T> Do;

        static Add()
        {
            var par1 = Expression.Parameter(typeof(T));
            var par2 = Expression.Parameter(typeof(T));

            var add = Expression.Add(par1, par2);

            Do = Expression.Lambda<Func<T, T, T>>(add, par1, par2).Compile();
        }
    }

    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class GenericAddBenchmark : TestMachine
    {
        private readonly long c1;
        private readonly long c2;

        public GenericAddBenchmark()
        {
            Random r = new Random();
            c1 = r.Next();
            c2 = r.Next();
        }

        [Benchmark]
        public long GenericAdd()
        {
            return Add<long>.Do(c1, c2);
        }

        [Benchmark]
        public long ActualAdd()
        {
            return c1 + c2;
        }
    }

    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class I32AddBenchmark : TestMachine
    {
        Frame frame;

        [GlobalSetup]
        public void Setup()
        {
            frame = CreateFrame();
            ModuleFunc func = new ModuleFunc("test", "$-1", frame.GetFuncTypeForIndex(0));
            func.Locals = new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 };
            List<UnflattenedInstruction> instructions = new List<UnflattenedInstruction>
            {
                Insn(InstructionType.I32_ADD),
                Insn(InstructionType.END)
            };
            func.Code = instructions.Flatten(0);
            frame.Func = func;
        }

        // BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
        // 11th Gen Intel Core i7-11700K 3.60GHz, 1 CPU, 16 logical and 8 physical cores
        //   [Host]               : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
        //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method         | Mean     | Error    | StdDev   | Ratio |
        // |--------------- |---------:|---------:|---------:|------:|
        // | I32Add         | 77.63 ns | 1.314 ns | 2.158 ns |  1.00 |
        // |                |          |          |          |       |
        // | I32AddOverhead | 36.79 ns | 0.372 ns | 0.330 ns |  1.00 |

        // With Value + list:
        //
        // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
        // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
        //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
        //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method         | Mean     | Error   | StdDev  | Ratio |
        // |--------------- |---------:|--------:|--------:|------:|
        // | I32Add         | 169.6 ns | 2.99 ns | 3.56 ns |  1.00 |
        // |                |          |         |         |       |
        // | I32AddOverhead | 145.6 ns | 1.66 ns | 1.56 ns |  1.00 |

        // With union (64 bits) + stack:
        //
        // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
        // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
        //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
        //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method         | Mean     | Error    | StdDev   | Ratio |
        // |--------------- |---------:|---------:|---------:|------:|
        // | I32Add         | 62.46 ns | 0.786 ns | 0.656 ns |  1.00 |
        // |                |          |          |          |       |
        // | I32AddOverhead | 26.63 ns | 0.411 ns | 0.365 ns |  1.00 |
        //
        // With union (128 bits) + stack:
        //
        // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
        // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
        //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
        //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method         | Mean     | Error    | StdDev   | Ratio |
        // |--------------- |---------:|---------:|---------:|------:|
        // | I32Add         | 64.93 ns | 1.284 ns | 1.800 ns |  1.00 |
        // |                |          |          |          |       |
        // | I32AddOverhead | 34.49 ns | 0.222 ns | 0.218 ns |  1.00 |
        [Benchmark]
        public Value I32Add()
        {
            frame.PC = 0;
            frame.Push(new Value { u32 = 0x0F });
            frame.Push(new Value { u32 = 0xFFFFFFFF });
            frame.Step(this);
            return frame.Pop();
        }

        [Benchmark]
        public Value I32AddOverhead()
        {
            frame.PC = 0;
            frame.Push(new Value { u32 = 0x0F });
            frame.Push(new Value { u32 = 0xFFFFFFFF });
            frame.Pop();
            frame.Pop();
            frame.Push(new Value { u32 = 0x0F });
            return frame.Pop();
        }
    }

    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class NopBenchmark : TestMachine
    {
        Frame frame;

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            frame = CreateFrame();
            ModuleFunc func = new ModuleFunc("test", "$-1", frame.GetFuncTypeForIndex(0));
            func.Locals = new Derg.ValueType[] { };

            List<UnflattenedInstruction> instructions = new List<UnflattenedInstruction>();
            for (int i = 0; i < N; i++)
            {
                instructions.Add(Insn(InstructionType.NOP));
            }
            func.Code = instructions.Flatten(0);
            frame.Func = func;
        }

        // BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
        // 11th Gen Intel Core i7-11700K 3.60GHz, 1 CPU, 16 logical and 8 physical cores
        //   [Host]               : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
        //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method | N     | Mean      | Error    | StdDev   | Ratio |
        // |------- |------ |----------:|---------:|---------:|------:|
        // | Nop    | 1000  |  18.79 us | 0.253 us | 0.423 us |  1.00 |
        // |        |       |           |          |          |       |
        // | Nop    | 10000 | 188.36 us | 2.372 us | 2.218 us |  1.00 |
        [Benchmark]
        public void Nop()
        {
            frame.PC = 0;
            frame.Step(this, N);
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method       | N     | Mean      | Error    | StdDev   | Ratio |
    // |------------- |------ |----------:|---------:|---------:|------:|
    // | PushPopInt   | 1000  |  12.56 us | 0.234 us | 0.219 us |  1.00 |
    // |              |       |           |          |          |       |
    // | PushPopAsInt | 1000  |  56.41 us | 0.756 us | 0.631 us |  1.00 |
    // |              |       |           |          |          |       |
    // | PushPopInt   | 10000 | 121.01 us | 0.692 us | 0.647 us |  1.00 |
    // |              |       |           |          |          |       |
    // | PushPopAsInt | 10000 | 588.64 us | 7.023 us | 6.569 us |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class PushPop : TestMachine
    {
        Frame frame;

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            frame = CreateFrame();
        }

        // BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
        // 11th Gen Intel Core i7-11700K 3.60GHz, 1 CPU, 16 logical and 8 physical cores
        //  [Host]               : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
        //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
        //
        // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
        //
        // | Method     | Mean     | Error    | StdDev   | Median   | Ratio |
        // |----------- |---------:|---------:|---------:|---------:|------:|
        // | PushPopInt | 22.30 ns | 0.414 ns | 0.991 ns | 21.96 ns |  1.00 |
        [Benchmark]
        public int PushPopInt()
        {
            int x = 0;
            for (int i = 0; i < N; i++)
            {
                frame.Push(new Value { s32 = 1 });
                x += frame.Pop().s32;
            }
            return x;
        }

        [Benchmark]
        public int PushPopAsInt()
        {
            int x = 0;
            for (int i = 0; i < N; i++)
            {
                frame.Push(new Value { s32 = 1 });
                x += frame.Pop<int>();
            }
            return x;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
