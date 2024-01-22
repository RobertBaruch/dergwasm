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
    // | Method         | Mean     | Error    | StdDev   | Ratio |
    // |--------------- |---------:|---------:|---------:|------:|
    // | I32Add         | 73.85 ns | 1.497 ns | 1.998 ns |  1.00 |
    // |                |          |          |          |       |
    // | I32AddOverhead | 38.75 ns | 0.382 ns | 0.338 ns |  1.00 |
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
            func.Code = new List<Instruction>();
            instructions.Flatten(func.Code);
            frame.Func = func;
        }

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
            func.Code = new List<Instruction>();
            instructions.Flatten(func.Code);
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
    // | Method             | N   | Mean     | Error     | StdDev    | Ratio |
    // |------------------- |---- |---------:|----------:|----------:|------:|
    // | PushPopInt         | 100 | 1.286 us | 0.0255 us | 0.0238 us |  1.00 |
    // |                    |     |          |           |           |       |
    // | PushPopGenericInt  | 100 | 5.886 us | 0.1075 us | 0.0953 us |  1.00 |
    // |                    |     |          |           |           |       |
    // | PushOverloadPopInt | 100 | 1.244 us | 0.0140 us | 0.0131 us |  1.00 |
    // |                    |     |          |           |           |       |
    // | PushGenericPopInt  | 100 | 2.126 us | 0.0421 us | 0.0468 us |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class PushPop : TestMachine
    {
        Frame frame;

        [Params(100)]
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
        public int PushPopGenericInt()
        {
            int x = 0;
            for (int i = 0; i < N; i++)
            {
                frame.Push(new Value { s32 = 1 });
                x += frame.Pop<int>();
            }
            return x;
        }

        [Benchmark]
        public int PushOverloadPopInt()
        {
            int x = 0;
            for (int i = 0; i < N; i++)
            {
                frame.Push(1);
                x += frame.Pop().s32;
            }
            return x;
        }

        [Benchmark]
        public int PushGenericPopInt()
        {
            int x = 0;
            for (int i = 0; i < N; i++)
            {
                frame.Push<int>(1);
                x += frame.Pop().s32;
            }
            return x;
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    //  Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //      [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method              | N    | Mean     | Error   | StdDev  | Ratio |
    // |-------------------- |----- |---------:|--------:|--------:|------:|
    // | FlattenLinear       | 1000 | 101.8 us | 1.64 us | 1.53 us |  1.00 |
    // |                     |      |          |         |         |       |
    // | FlattenBlocks       | 1000 | 117.5 us | 1.61 us | 1.43 us |  1.00 |
    // |                     |      |          |         |         |       |
    // | FlattenNestedBlocks | 1000 | 110.5 us | 1.84 us | 1.72 us |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class FlattenerBenchmark
    {
        [Params(1000)]
        public int N;

        List<UnflattenedInstruction> linearInstructions;
        readonly UnflattenedOperand[] noOperands = { };

        List<UnflattenedInstruction> blockInstructions;
        List<UnflattenedInstruction> nestedBlockInstructions;

        UnflattenedInstruction NestBlock(int level)
        {
            List<UnflattenedInstruction> blockInsns = new List<UnflattenedInstruction>(11);
            for (int j = 0; j < 9; j++)
            {
                blockInsns.Add(new UnflattenedInstruction(InstructionType.NOP, noOperands));
            }
            if (level > 0)
            {
                blockInsns.Add(NestBlock(level - 1));
            }
            blockInsns.Add(new UnflattenedInstruction(InstructionType.END, noOperands));
            UnflattenedBlockOperand[] blockOperand =
            {
                new UnflattenedBlockOperand(
                    new Value(),
                    blockInsns,
                    new List<UnflattenedInstruction>()
                )
            };
            return new UnflattenedInstruction(InstructionType.BLOCK, blockOperand);
        }

        [GlobalSetup]
        public void Setup()
        {
            // Creates a list of non-control instructions to flatten
            linearInstructions = new List<UnflattenedInstruction>(N);
            for (int i = 0; i < linearInstructions.Capacity; i++)
            {
                linearInstructions.Add(new UnflattenedInstruction(InstructionType.NOP, noOperands));
            }

            blockInstructions = new List<UnflattenedInstruction>(N / 10);
            for (int i = 0; i < blockInstructions.Capacity; i++)
            {
                List<UnflattenedInstruction> blockInsns = new List<UnflattenedInstruction>(11);
                for (int j = 0; j < 10; j++)
                {
                    blockInsns.Add(new UnflattenedInstruction(InstructionType.NOP, noOperands));
                }
                blockInsns.Add(new UnflattenedInstruction(InstructionType.END, noOperands));
                UnflattenedBlockOperand[] blockOperand =
                {
                    new UnflattenedBlockOperand(
                        new Value(),
                        blockInsns,
                        new List<UnflattenedInstruction>()
                    )
                };
                blockInstructions.Add(
                    new UnflattenedInstruction(InstructionType.BLOCK, blockOperand)
                );
            }

            nestedBlockInstructions = new List<UnflattenedInstruction>() { NestBlock(N / 10 - 1) };
        }

        [Benchmark]
        public List<Instruction> FlattenLinear()
        {
            List<Instruction> instructions = new List<Instruction>();
            linearInstructions.Flatten(instructions);
            return instructions;
        }

        [Benchmark]
        public List<Instruction> FlattenBlocks()
        {
            List<Instruction> instructions = new List<Instruction>();
            blockInstructions.Flatten(instructions);
            return instructions;
        }

        [Benchmark]
        public List<Instruction> FlattenNestedBlocks()
        {
            List<Instruction> instructions = new List<Instruction>();
            nestedBlockInstructions.Flatten(instructions);
            return instructions;
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
