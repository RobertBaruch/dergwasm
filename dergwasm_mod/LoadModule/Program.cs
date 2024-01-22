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
        EmscriptenEnv emscriptenEnv = new EmscriptenEnv(machine);
        emscriptenEnv.RegisterHostFuncs();

        EmscriptenWasi emscriptenWasi = new EmscriptenWasi(machine, emscriptenEnv);
        emscriptenWasi.RegisterHostFuncs();

        ResoniteEnv resoniteEnv = new ResoniteEnv(machine, null, emscriptenEnv);
        resoniteEnv.RegisterHostFuncs();

        FilesystemEnv filesystemEnv = new FilesystemEnv(
            machine,
            null,
            emscriptenEnv,
            emscriptenWasi
        );
        filesystemEnv.RegisterHostFuncs();

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

        MaybeRunEmscriptenCtors();
        // RunMain();
        RunMicropython(emscriptenEnv);
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

    void MaybeRunEmscriptenCtors()
    {
        Func ctors = machine.GetFunc(moduleInstance.ModuleName, "__wasm_call_ctors");
        if (ctors == null)
        {
            return;
        }
        Console.WriteLine("Running __wasm_call_ctors");
        Frame frame = new Frame(ctors as ModuleFunc, moduleInstance, null);
        frame.Label = new Label(0, 0);
        frame.InvokeFunc(machine, ctors);
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
            Frame frame = new Frame(main as ModuleFunc, moduleInstance, null);
            frame.Label = new Label(1, 0);
            frame.Push(new Value { s32 = 0 }); // argc
            frame.Push(new Value { s32 = 0 }); // argv
            frame.InvokeFunc(machine, main);
        }
        catch (ExitTrap) { }
    }

    int AddUTF8StringToStack(EmscriptenEnv emscriptenEnv, string s)
    {
        byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
        int size = utf.Length + 1;
        Frame frame = new Frame(null, moduleInstance, null);
        frame.Label = new Label(1, 0);
        int stackPtr = emscriptenEnv.stackAlloc(frame, size);

        Array.Copy(utf, 0, machine.Heap, stackPtr, utf.Length);
        machine.Heap[stackPtr + utf.Length] = 0; // NUL-termination

        return stackPtr;
    }

    void MicropythonDoStr(EmscriptenEnv emscriptenEnv, int stackPtr)
    {
        Func mp_js_do_str = machine.GetFunc(moduleInstance.ModuleName, "mp_js_do_str");
        if (mp_js_do_str == null)
        {
            throw new Trap("No mp_js_do_str function found");
        }
        Console.WriteLine($"Running {mp_js_do_str.Name}");
        try
        {
            Frame frame = new Frame(mp_js_do_str as ModuleFunc, moduleInstance, null);
            frame.Label = new Label(1, 0);
            frame.Push(new Value { s32 = stackPtr }); // source
            frame.InvokeFunc(machine, mp_js_do_str);
        }
        catch (ExitTrap) { }
    }

    void InitMicropython(EmscriptenEnv emscriptenEnv, int stackSizeBytes)
    {
        Func mp_js_init = machine.GetFunc(moduleInstance.ModuleName, "mp_js_init");
        if (mp_js_init == null)
        {
            throw new Trap("No mp_js_init function found");
        }
        Console.WriteLine($"Running {mp_js_init.Name}");
        try
        {
            Frame frame = new Frame(mp_js_init as ModuleFunc, moduleInstance, null);
            frame.Label = new Label(1, 0);
            frame.Push(new Value { s32 = stackSizeBytes });
            frame.InvokeFunc(machine, mp_js_init);
        }
        catch (ExitTrap) { }
    }

    void RunMicropython(EmscriptenEnv emscriptenEnv)
    {
        InitMicropython(emscriptenEnv, 64 * 1024);
        Console.WriteLine("Micropython initialized");

        string s = "print('hello world!')\n"; // The Python code to run

        int ptr = AddUTF8StringToStack(emscriptenEnv, s);
        MicropythonDoStr(emscriptenEnv, ptr);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Reading WASM file '{args[0]}'");
        Program program = new Program(args[0]);
    }
}
