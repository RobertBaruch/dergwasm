using System;
using System.IO;
using Derg;

public class Program
{
    Machine machine;

    public Program(string filename)
    {
        machine = new Machine();

        using (var stream = File.OpenRead(filename))
        {
            BinaryReader reader = new BinaryReader(stream);
            var module = Module.Read(reader);

            module.Instantiate(machine, new int[0], new int[0], new int[0], new int[0]);
        }
    }

    public void EmscriptenMemcpyJs(int dest, int src, int len)
    {
        Console.WriteLine($"EmscriptenMemcpyJs({dest}, {src}, {len})");
        Memory mem = machine.GetMemoryFromIndex(0);
        try
        {
            Array.Copy(mem.Data, src, mem.Data, dest, len);
        }
        catch (Exception e)
        {
            throw new Trap(
                $"EmscriptenMemcpyJs: Access out of bounds: source offset 0x{src:8X}, destination offset 0x{dest:8X}, length 0x{len:8X} bytes"
            );
        }
    }

    public unsafe void FdWrite(int fd, int iov, int iovcnt, int pnum)
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
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        Program program = new Program(args[0]);
    }
}
