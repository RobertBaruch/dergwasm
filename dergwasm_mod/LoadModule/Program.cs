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
        int[] extern_funcs = RegisterHostFuncs(machine);

        using (var stream = File.OpenRead(filename))
        {
            BinaryReader reader = new BinaryReader(stream);
            var module = Module.Read("hello_world", reader);

            module.ResolveExterns(machine);

            moduleInstance = module.Instantiate(machine);
        }
    }

    public int[] RegisterHostFuncs(IMachine machine)
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

    public void EmscriptenMemcpyJs(int dest, int src, int len)
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
                    + $"0x{src:8X}, destination offset 0x{dest:8X}, length 0x{len:8X} bytes"
            );
        }
    }

    public int FdWrite(int fd, int iov, int iovcnt, int pnum)
    {
        Console.WriteLine($"FdWrite({fd}, 0x{iov:8X}, {iovcnt}, 0x{pnum:8X})");
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

        foreach (var f in program.machine.funcs)
        {
            Console.WriteLine($"{f.ModuleName}.{f.Name}");
        }
    }
}
