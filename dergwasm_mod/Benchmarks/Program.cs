using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Derg;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

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
        [Benchmark]
        public Value I32Add()
        {
            frame.PC = 0;
            frame.Push(new Value(0x0F));
            frame.Push(new Value(0xFFFFFFFF));
            frame.Step(this);
            return frame.Pop();
        }

        [Benchmark]
        public Value I32AddOverhead()
        {
            frame.PC = 0;
            frame.Push(new Value(0x0F));
            frame.Push(new Value(0xFFFFFFFF));
            frame.Pop();
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

    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class PushPop : TestMachine
    {
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
        public Value PushPopInt()
        {
            Frame frame = CreateFrame();
            frame.Push(new Value(1));
            return frame.Pop();
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
