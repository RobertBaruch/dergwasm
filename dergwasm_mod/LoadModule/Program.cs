using System;
using System.Collections.Generic;
using System.IO;
using Derg;

public class Program
{
    public Machine machine;
    public ModuleInstance moduleInstance;

    public Program(string filename)
    {
        machine = new Machine();
        machine.Debug = true;
        int[] extern_funcs = RegisterHostFuncs(machine);
        Module module;

        using (var stream = File.OpenRead(filename))
        {
            BinaryReader reader = new BinaryReader(stream);
            module = Module.Read("hello_world", reader);
        }

        module.ResolveExterns(machine);
        moduleInstance = module.Instantiate(machine);
        CheckForUnimplementedInstructions();

        foreach (var f in machine.funcs)
        {
            Console.WriteLine($"Function defined: {f.ModuleName}.{f.Name}");
        }

        MaybeRunEmscriptedCtors();
        RunMain();
    }

    void CheckForUnimplementedInstructions()
    {
        HashSet<InstructionType> needed = new HashSet<InstructionType>();
        foreach (var f in machine.funcs)
        {
            if (f is HostFunc)
            {
                continue;
            }
            ModuleFunc func = (ModuleFunc)f;
            foreach (var instr in func.Code)
            {
                if (!InstructionEvaluation.Map.ContainsKey(instr.Type))
                {
                    needed.Add(instr.Type);
                }
            }
        }

        if (needed.Count > 0)
        {
            Console.WriteLine("Unimplemented instructions:");
            foreach (var instr in needed)
            {
                Console.WriteLine($"  {instr}");
            }
            throw new Trap("Unimplemented instructions");
        }
    }

    void MaybeRunEmscriptedCtors()
    {
        Func ctors = machine.GetFunc(moduleInstance.ModuleName, "__wasm_call_ctors");
        if (ctors == null)
        {
            return;
        }
        Console.WriteLine("Running __wasm_call_ctors");
        machine.InvokeExpr(ctors as ModuleFunc);
    }

    void RunMain()
    {
        Func main = machine.GetFunc(moduleInstance.ModuleName, "main");
        if (main == null)
        {
            main = machine.GetFunc(moduleInstance.ModuleName, "_start");
        }
        if (main == null)
        {
            throw new Trap("No main or _start function found");
        }
        Console.WriteLine($"Running {main.Name}");
        machine.InvokeExpr(main as ModuleFunc);
    }

    int[] RegisterHostFuncs(IMachine machine)
    {
        List<int> extern_funcs = new List<int>();

        extern_funcs.Add(
            machine.RegisterHostFunc(
                "env",
                "emscripten_memcpy_js",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { }
                ),
                new HostProxy<int, int, int>(EmscriptenMemcpyJs)
            )
        );

        extern_funcs.Add(
            machine.RegisterHostFunc(
                "wasi_snapshot_preview1",
                "fd_write",
                new FuncType(
                    new Derg.ValueType[]
                    {
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32,
                        Derg.ValueType.I32
                    },
                    new Derg.ValueType[] { Derg.ValueType.I32 }
                ),
                new ReturningHostProxy<int, int, int, int, int>(FdWrite)
            )
        );

        return extern_funcs.ToArray();
    }

    void EmscriptenMemcpyJs(int dest, int src, int len)
    {
        Console.WriteLine($"EmscriptenMemcpyJs({dest}, {src}, {len})");
        Memory mem = machine.GetMemoryFromIndex(0);
        try
        {
            Array.Copy(mem.Data, src, mem.Data, dest, len);
        }
        catch (Exception)
        {
            throw new Trap(
                "EmscriptenMemcpyJs: Access out of bounds: source offset "
                    + $"0x{src:X8}, destination offset 0x{dest:X8}, length 0x{len:X8} bytes"
            );
        }
    }

    int FdWrite(int fd, int iov, int iovcnt, int pnum)
    {
        Console.WriteLine($"FdWrite({fd}, 0x{iov:X8}, {iovcnt}, 0x{pnum:X8})");
        Memory mem = machine.GetMemoryFromIndex(0);

        MemoryStream iovStream = new MemoryStream(mem.Data);
        iovStream.Position = iov;
        BinaryReader iovReader = new BinaryReader(iovStream);

        int count = 0;

        for (int i = 0; i < iovcnt; i++)
        {
            int ptr = iovReader.ReadInt32();
            int len = iovReader.ReadInt32();
            Console.WriteLine($"  iov[{i}]: ptr=0x{ptr:X8}, len={len}");
            byte[] data = new byte[len];
            Array.Copy(mem.Data, ptr, data, 0, len);
            Console.WriteLine($"  data: {System.Text.Encoding.UTF8.GetString(data)}");
            count += len;
        }

        iovStream.Position = pnum;
        BinaryWriter countWriter = new BinaryWriter(iovStream);
        countWriter.Write(count);
        return 0;
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        Program program = new Program(args[0]);
    }
}
