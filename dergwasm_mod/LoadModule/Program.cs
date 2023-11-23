using System;
using System.IO;
using Derg;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        using (var stream = File.OpenRead(args[0]))
        {
            BinaryReader reader = new BinaryReader(stream);
            var module = Module.Read(reader);
        }
    }
}
