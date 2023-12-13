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
        new EmscriptenWasi(machine).RegisterHostFuncs();
        Module module;

        using (var stream = File.OpenRead(filename))
        {
            BinaryReader reader = new BinaryReader(stream);
            module = Module.Read("hello_world", reader);
        }
        machine.MainModuleName = module.ModuleName;

        module.ResolveExterns(machine);
        moduleInstance = module.Instantiate(machine);
        CheckForUnimplementedInstructions();

        for (int i = 0; i < machine.funcs.Count; i++)
        {
            Func f = machine.funcs[i];
            // Console.WriteLine($"Func [{i}]: {f.ModuleName}.{f.Name}: {f.Signature}");
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
        try
        {
            machine.InvokeExpr(main as ModuleFunc);
        }
        catch (ExitTrap) { }
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        Program program = new Program(args[0]);
    }
}
