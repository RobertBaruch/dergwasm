using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Derg;
using Derg.Modules;
using Derg.Wasm;
using Derg.Instructions;
using Derg.Runtime;
using DergwasmTests.instructions;
using DergwasmTests.testing;
using FrooxEngine;
using static Derg.ResoniteEnv;
using Dergwasm.Runtime;

namespace DergwasmTests
{
    // BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
    // 11th Gen Intel Core i7-11700K 3.60GHz, 1 CPU, 16 logical and 8 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method         | Mean     | Error    | StdDev   | Ratio | RatioSD |
    // |--------------- |---------:|---------:|---------:|------:|--------:|
    // | I32AddOverhead | 42.62 ns | 0.853 ns | 1.079 ns |  1.00 |    0.00 |
    // | I32Add         | 69.42 ns | 1.406 ns | 2.904 ns |  1.64 |    0.09 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class I32AddBenchmark : TestMachine
    {
        Frame frame;

        public I32AddBenchmark()
        {
            frame = CreateFrame();
            ModuleFunc func = new ModuleFunc("test", "$-1", frame.GetFuncTypeForIndex(0));
            func.Locals = new Derg.Runtime.ValueType[]
            {
                Derg.Runtime.ValueType.I32,
                Derg.Runtime.ValueType.I32
            };
            List<UnflattenedInstruction> instructions = new List<UnflattenedInstruction>
            {
                Insn(InstructionType.I32_ADD),
                Insn(InstructionType.END)
            };
            func.Code = new List<Instruction>();
            instructions.Flatten(func.Code);
            frame.Func = func;
        }

        [Benchmark(Baseline = true)]
        public Value I32AddOverhead()
        {
            frame.PC = 0;
            frame.Push(new Value { u32 = 0x0F });
            frame.Push(new Value { u32 = 0xFFFFFFFF });

            // Enumulate I32.ADD's effect on the stack without actually adding.
            frame.Pop();
            frame.Pop();
            frame.Push(new Value { u32 = 0x0F });

            return frame.Pop();
        }

        // This will measure the effect of a step with an add instruction. It shouldn't be
        // that much more above the overhead than a NOP instruction.
        [Benchmark]
        public Value I32Add()
        {
            frame.PC = 0;
            frame.Push(new Value { u32 = 0x0F });
            frame.Push(new Value { u32 = 0xFFFFFFFF });
            frame.Step(this);
            return frame.Pop();
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method | N     | Mean      | Error     | StdDev    | Ratio |
    // |------- |------ |----------:|----------:|----------:|------:|
    // | Nop    | 1000  |  34.55 us |  0.685 us |  1.629 us |  1.00 |
    // |        |       |           |           |           |       |
    // | Nop    | 10000 | 329.40 us | 11.099 us | 32.024 us |  1.00 |
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
            func.Locals = new Derg.Runtime.ValueType[] { };

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
    // | Method             | N   | Mean     | Error     | StdDev    | Ratio | RatioSD |
    // |------------------- |---- |---------:|----------:|----------:|------:|--------:|
    // | BaselinePushPopInt | 100 | 1.223 us | 0.0092 us | 0.0082 us |  1.00 |    0.00 |
    // | PopAsInt           | 100 | 1.506 us | 0.0191 us | 0.0169 us |  1.23 |    0.02 |
    // | PushOverloadInt    | 100 | 1.240 us | 0.0225 us | 0.0350 us |  1.03 |    0.04 |
    // | PushGenericInt     | 100 | 1.485 us | 0.0120 us | 0.0106 us |  1.21 |    0.01 |
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
        [Benchmark(Baseline = true)]
        public int BaselinePushPopInt()
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
        public int PopAsInt()
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
        public int PushOverloadInt()
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
        public int PushGenericInt()
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
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method       | N    | Mean     | Error   | StdDev  | Ratio |
    // |------------- |----- |---------:|--------:|--------:|------:|
    // | HostFuncCall | 1000 | 314.0 us | 4.39 us | 4.11 us |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class HostFuncCallBenchmark : InstructionTestFixture
    {
        [Params(1000)]
        public int N;

        public HostFuncCallBenchmark()
        {
            // 0: I32_CONST 10
            // 1: I32_CONST 20
            // 2: CALL 4
            // 3: NOP
            //
            // Func 14 (= idx 4): host func
            machine.SetProgram(0, I32Const(10), I32Const(20), Call(4), Nop());
            var (_, hostFunc) = ModuleReflector.ReflectHostFunc(
                "test",
                "env",
                new Func<int, int, int>((a, b) => a - b)
            );
            machine.SetHostFuncAt(14, hostFunc.Proxy);
        }

        [Benchmark]
        public int HostFuncCall()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
            {
                machine.Frame.PC = 0;
                machine.Step(3);
                sum += machine.Frame.Pop().s32;
            }
            return sum;
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

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    //    Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //      [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method         | N     | Mean     | Error   | StdDev   | Ratio |
    // |--------------- |------ |---------:|--------:|---------:|------:|
    // | LargeBlockCopy | 10000 | 496.4 us | 9.72 us | 10.81 us |  1.00 |
    // |                |       |          |         |          |       |
    // | LargeArrayCopy | 10000 | 460.3 us | 6.11 us |  5.71 us |  1.00 |
    // |                |       |          |         |          |       |
    // | SmallBlockCopy | 10000 | 210.2 us | 2.93 us |  2.45 us |  1.00 |
    // |                |       |          |         |          |       |
    // | SmallArrayCopy | 10000 | 221.6 us | 3.27 us |  2.90 us |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class MemoryCopyBenchmark : TestMachine
    {
        [Params(10000)]
        public int N;

        [Benchmark]
        public byte[] LargeBlockCopy()
        {
            byte[] heap = Heap;
            for (int i = 0; i < N; i++)
            {
                Buffer.BlockCopy(heap, 0, heap, 1, 1000);
                Buffer.BlockCopy(heap, 1, heap, 0, 1000);
            }
            return heap;
        }

        [Benchmark]
        public byte[] LargeArrayCopy()
        {
            byte[] heap = Heap;
            for (int i = 0; i < N; i++)
            {
                Array.Copy(heap, 0, heap, 1, 1000);
                Array.Copy(heap, 1, heap, 0, 1000);
            }
            return heap;
        }

        [Benchmark]
        public byte[] SmallBlockCopy()
        {
            byte[] heap = Heap;
            for (int i = 0; i < N; i++)
            {
                Buffer.BlockCopy(heap, 0, heap, 1, 8);
                Buffer.BlockCopy(heap, 1, heap, 0, 8);
            }
            return heap;
        }

        [Benchmark]
        public byte[] SmallArrayCopy()
        {
            byte[] heap = Heap;
            for (int i = 0; i < N; i++)
            {
                Array.Copy(heap, 0, heap, 1, 8);
                Array.Copy(heap, 1, heap, 0, 8);
            }
            return heap;
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method          | Mean     | Error   | StdDev   | Ratio |
    // |---------------- |---------:|--------:|---------:|------:|
    // | LoadMicropython | 279.6 ms | 5.55 ms | 14.52 ms |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class MicropythonLoadBenchmark
    {
        [Benchmark]
        public DergwasmLoadModule.Program LoadMicropython()
        {
            return new DergwasmLoadModule.Program("../../../../../firmware.wasm");
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method     | N   | Mean     | Error     | StdDev    | Ratio |
    // |----------- |---- |---------:|----------:|----------:|------:|
    // | MallocFree | 200 | 8.800 ms | 0.1695 ms | 0.2262 ms |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class MicropythonMallocFreeBenchmark
    {
        DergwasmLoadModule.Program program;

        [Params(200)]
        public int N;

        public MicropythonMallocFreeBenchmark()
        {
            program = new DergwasmLoadModule.Program("../../../../../firmware.wasm");
            program.InitMicropython(64 * 1024);
        }

        [Benchmark]
        public Machine MallocFree()
        {
            // Allocates N/2 segments, then frees every other one, then allocates N/2 more segments,
            // then frees everything.
            Frame frame = program.emscriptenEnv.EmptyFrame();
            List<int> dataPtrs = new List<int>(N);
            for (int i = 0; i < N / 2; i++)
            {
                dataPtrs.Add(program.emscriptenEnv.Malloc(frame, 4 * i + 4));
            }
            for (int i = 0; i < N / 2; i += 2)
            {
                program.emscriptenEnv.Free(frame, dataPtrs[i]);
            }
            List<int> moreDataPtrs = new List<int>(N);
            for (int i = N / 2 - 1; i >= 0; i--)
            {
                moreDataPtrs.Add(program.emscriptenEnv.Malloc(frame, 4 * i + 4));
            }
            for (int i = 1; i < N / 2; i += 2)
            {
                program.emscriptenEnv.Free(frame, dataPtrs[i]);
            }
            for (int i = 0; i < N / 2; i++)
            {
                program.emscriptenEnv.Free(frame, moreDataPtrs[i]);
            }
            return program.machine;
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    //    Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //      [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method        | N   | Mean     | Error    | StdDev   | Ratio |
    // |-------------- |---- |---------:|---------:|---------:|------:|
    // | Serialization |     | 40.27 us | 0.783 us | 0.694 us |  1.00 |
    //
    // With preallocated buffer:
    //
    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //   .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    // | Method        | N   | Mean     | Error   | StdDev  | Ratio |
    // |-------------- |---- |---------:|--------:|--------:|------:|
    // | Serialization |     | 352.7 ns | 6.88 ns | 8.19 ns |  1.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class MicropythonSerializationBenchmark
    {
        DergwasmLoadModule.Program program;
        Frame frame;

        public MicropythonSerializationBenchmark()
        {
            program = new DergwasmLoadModule.Program("../../../../../firmware.wasm");
            program.InitMicropython(64 * 1024);
            frame = program.emscriptenEnv.EmptyFrame();
        }

        [Benchmark]
        public int Serialization()
        {
            int dataPtr = SimpleSerialization.Serialize(
                program.machine,
                program.resoniteEnv,
                frame,
                100
            );
            return (int)
                SimpleSerialization.Deserialize(program.machine, program.resoniteEnv, dataPtr);
        }
    }

    // BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3930/22H2/2022Update)
    // Intel Core i7-7660U CPU 2.50GHz(Kaby Lake), 1 CPU, 4 logical and 2 physical cores
    //   [Host]               : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256 [AttachedDebugger]
    //  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9195.0), X64 RyuJIT VectorSize=256
    //
    // Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2
    //
    //| Method                          | N   | Mean      | Error     | StdDev    | Ratio | RatioSD |
    //|-------------------------------- |---- |----------:|----------:|----------:|------:|--------:|
    //| ComponentUtils_GetIntField      | 100 | 54.957 us | 1.0714 us | 1.3157 us |  1.00 |    0.00 |
    //| ValueGet_GetIntField_UnknownRef | 100 | 31.752 us | 0.5005 us | 0.7938 us |  0.58 |    0.02 |
    //| ValueGet_GetIntField_KnownRef   | 100 |  9.017 us | 0.1200 us | 0.1064 us |  0.16 |    0.00 |
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    public class GetIntFieldBenchmark : TestMachine
    {
        FakeWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        TestComponent testComponent;
        Frame frame;

        [Params(100)]
        public int N;

        public GetIntFieldBenchmark()
        {
            ResonitePatches.Apply();
            worldServices = new FakeWorldServices();
            emscriptenEnv = new TestEmscriptenEnv();
            env = new ResoniteEnv(this, worldServices, emscriptenEnv);
            SimpleSerialization.Initialize(env);
            frame = emscriptenEnv.EmptyFrame();

            testComponent = new TestComponent(worldServices);
            testComponent.Initialize();
            testComponent.IntField.Value = 1;
        }

        [Benchmark(Baseline = true)]
        public object ComponentUtils_GetIntField()
        {
            int sum = 0;
            object value;
            for (int i = 0; i < N; i++)
            {
                ComponentUtils.GetFieldValue(testComponent, "IntField", out value);
                sum += (int)value;
            }
            return sum;
        }

        [Benchmark]
        public object ValueGet_GetIntField_UnknownRef()
        {
            emscriptenEnv.ResetMalloc();
            int sum = 0;
            NullTerminatedString namePtr = new NullTerminatedString(
                emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField")
            );
            Ptr<ResoniteType> outTypePtr = new Ptr<ResoniteType>(namePtr.Data.Addr + 100);
            Output<WasmRefID> outRefIdPtr = new Output<WasmRefID>(outTypePtr.Addr + sizeof(int));
            Ptr<int> outPtr = new Ptr<int>(outRefIdPtr.Ptr.Addr + sizeof(ulong));
            WasmRefID<Component> refId = new WasmRefID<Component>(testComponent);

            for (int i = 0; i < N; i++)
            {
                if (env.component__get_member(frame, refId, namePtr, outTypePtr, outRefIdPtr) != 0)
                {
                    throw new Exception("component__get_member failed");
                }
                if (
                    env.value__get(frame, new WasmRefID<IValue<int>>(HeapGet(outRefIdPtr)), outPtr)
                    != 0
                )
                {
                    throw new Exception("value__get failed");
                }
                sum += HeapGet(outPtr);
            }
            return sum;
        }

        [Benchmark]
        public object ValueGet_GetIntField_KnownRef()
        {
            emscriptenEnv.ResetMalloc();
            int sum = 0;
            Ptr<int> outPtr = new Ptr<int>(4);
            var refId = testComponent.IntField.GetWasmRef<IValue<int>>();
            ;

            for (int i = 0; i < N; i++)
            {
                if (env.value__get(frame, refId, outPtr) != 0)
                {
                    throw new Exception("value__get failed");
                }
                sum += HeapGet(outPtr);
            }
            return sum;
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
