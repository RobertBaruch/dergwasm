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
        // machine.Debug = true;
        RegisterHostFuncs(machine);
        Module module;

        using (var stream = File.OpenRead(filename))
        {
            BinaryReader reader = new BinaryReader(stream);
            module = Module.Read("hello_world", reader);
        }

        module.ResolveExterns(machine);
        moduleInstance = module.Instantiate(machine);
        CheckForUnimplementedInstructions();

        for (int i = 0; i < machine.funcs.Count; i++)
        {
            Func f = machine.funcs[i];
            Console.WriteLine($"Func [{i}]: {f.ModuleName}.{f.Name}: {f.Signature}");
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

    void RegisterHostFuncs(IMachine machine)
    {
        machine.RegisterHostFunc(
            "env",
            "emscripten_memcpy_js",
            new FuncType(
                new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32, Derg.ValueType.I32 },
                new Derg.ValueType[] { }
            ),
            new HostProxy<int, int, int>(EmscriptenMemcpyJs)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "environ_get",
            new FuncType(
                new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                new Derg.ValueType[] { Derg.ValueType.I32 }
            ),
            new ReturningHostProxy<int, int, int>(EnvironGet)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "environ_sizes_get",
            new FuncType(
                new Derg.ValueType[] { Derg.ValueType.I32, Derg.ValueType.I32 },
                new Derg.ValueType[] { Derg.ValueType.I32 }
            ),
            new ReturningHostProxy<int, int, int>(EnvironSizesGet)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "proc_exit",
            new FuncType(new Derg.ValueType[] { Derg.ValueType.I32 }, new Derg.ValueType[] { }),
            new HostProxy<int>(ProcExit)
        );

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
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "fd_seek",
            new FuncType(
                new Derg.ValueType[]
                {
                    Derg.ValueType.I32,
                    Derg.ValueType.I64,
                    Derg.ValueType.I32,
                    Derg.ValueType.I32
                },
                new Derg.ValueType[] { Derg.ValueType.I32 }
            ),
            new ReturningHostProxy<int, long, int, int, int>(FdSeek)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "fd_read",
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
            new ReturningHostProxy<int, int, int, int, int>(FdRead)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "fd_close",
            new FuncType(
                new Derg.ValueType[] { Derg.ValueType.I32 },
                new Derg.ValueType[] { Derg.ValueType.I32 }
            ),
            new ReturningHostProxy<int, int>(FdClose)
        );

        machine.RegisterHostFunc(
            "wasi_snapshot_preview1",
            "fd_sync",
            new FuncType(
                new Derg.ValueType[] { Derg.ValueType.I32 },
                new Derg.ValueType[] { Derg.ValueType.I32 }
            ),
            new ReturningHostProxy<int, int>(FdSync)
        );
    }

    void ProcExit(int exit_code)
    {
        Console.WriteLine(
            $"ProcExit called with exit_code={exit_code}. {100000 - ((Machine)machine).stepBudget} instructions executed."
        );
        Environment.Exit(exit_code);
    }

    int EnvironGet(int environPtrPtr, int environBufPtr)
    {
        //throw new Trap(
        //    $"Unimplemented call to EnvironGet(0x{environPtrPtr:X8}, 0x{environBufPtr:X8})"
        //);
        return 0;
    }

    int EnvironSizesGet(int argcPtr, int argvBufSizePtr)
    {
        //throw new Trap(
        //    $"Unimplemented call to EnvironSizesGet(0x{argcPtr:X8}, 0x{argvBufSizePtr:X8})"
        //);
        return 0;
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

    // Seeks to a position in a file descriptor.
    //
    // Args:
    //    fd: The file descriptor to seek.
    //    offset: The 64-bit offset to seek to.
    //    whence: The origin of the seek.This is one of:
    //        0: SEEK_SET(seek from the beginning of the file)
    //        1: SEEK_CUR(seek from the current position in the file)
    //        2: SEEK_END(seek from the end of the file)
    //    newoffset_ptr: The address of an i64 to store the new offset.
    //
    // Returns:
    //    0 on success, or -ERRNO on failure.
    int FdSeek(int fd, long offset, int whence, int newOffsetPtr)
    {
        throw new Trap(
            $"Unimplemented call to FdSeek({fd}, {offset}, {whence}, 0x{newOffsetPtr:X8})"
        );
    }

    // Reads from a file descriptor.
    //
    // Args:
    //    fd: The file descriptor to read from.
    //    iovs: The address of an array of __wasi_ciovec_t structs. Such a struct is
    //        simply a pointer and a length.
    //    iovs_len: The length of the array pointed to by iovs.
    //    nread_ptr: The address of an i32 to store the number of bytes read.
    //
    // Returns:
    //    0 on success, or -ERRNO on failure.
    int FdRead(int fd, int iovs, int iovs_len, int nreadPtr)
    {
        throw new Trap(
            $"Unimplemented call to FdRead({fd}, 0x{iovs:X8}, {iovs_len}, 0x{nreadPtr:X8})"
        );
    }

    // Closes a file descriptor.
    //
    // Args:
    //    fd: The file descriptor to close.
    //
    // Returns:
    //    0 on success, or -ERRNO on failure.
    int FdClose(int fd)
    {
        throw new Trap($"Unimplemented call to FdClose({fd})");
    }

    int FdSync(int fd)
    {
        throw new Trap($"Unimplemented call to FdSync({fd})");
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        Program program = new Program(args[0]);
    }
}
