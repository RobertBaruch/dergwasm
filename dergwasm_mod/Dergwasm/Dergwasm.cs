using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Elements.Core; // For UniLog
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;

namespace Derg
{
    public class Dergwasm : ResoniteMod
    {
        public override string Name => "Dergwasm";
        public override string Author => "Xekri";
        public override string Version => typeof(Dergwasm).Assembly.GetName().Version.ToString();
        public static ModConfiguration Config;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.xekri.Dergwasm");
            Config = GetConfiguration();
            Config?.Save(true);
            harmony.PatchAll();
            Msg("Dergwasm patches applied");
        }

        [HarmonyPatch(typeof(World), nameof(World.Load))]
        class StartRunningPatch
        {
            static void Postfix(World __instance)
            {
                Msg("Postfix called on World.Load");
                Msg($"... WorldName {__instance.Configuration.WorldName.Value}");
                Msg($"... SessionID {__instance.Configuration.SessionID.Value}");
                DergwasmMachine.InitStage0(new WorldServices(__instance));
            }
        }

        [HarmonyPatch(typeof(ProtoFlux.Runtimes.Execution.Nodes.Actions.DynamicImpulseTrigger))]
        [HarmonyPatch("Trigger")]
        class TriggerPatch
        {
            static void Prefix(
                Slot hierarchy,
                string tag,
                bool excludeDisabled,
                FrooxEngineContext context
            )
            {
                // Use tag __dergwasm_init and slot with tag _dergwasm to initialize Dergwasm. You don't have to do
                // this if the world already has the Dergwasm hiearchy set up, since this automatically happens
                // on world load. When you change the hierarchy or the WASM file while the world is running, you
                // can call this to reinitialize Dergwasm.

                // Use tag __dergwasm and slot with tag _dergwasm_args to call a WASM function.

                if (hierarchy == null)
                    return;
                if (
                    (tag != "_dergwasm" || hierarchy.Tag != "_dergwasm_args")
                    && (tag != "_dergwasm_init" || hierarchy.Tag != "_dergwasm")
                )
                    return;

                try
                {
                    if (tag == "_dergwasm")
                        DergwasmMachine.resoniteEnv.CallWasmFunction(hierarchy);
                    else if (tag == "_dergwasm_init")
                        DergwasmMachine.InitStage0(new WorldServices(context.World));
                }
                catch (Exception e)
                {
                    DergwasmMachine.Msg($"Exception: {e}");
                }
            }
        }
    }

    public static class DergwasmMachine
    {
        public static IWorldServices worldServices = null;
        public static Machine machine = null;
        public static ModuleInstance moduleInstance = null;
        public static EmscriptenEnv emscriptenEnv = null;
        public static EmscriptenWasi emscriptenWasi = null;
        public static ResoniteEnv resoniteEnv = null;
        public static FilesystemEnv filesystemEnv = null;
        public static ISlot dergwasmSlot = null;
        public static ISlot consoleSlot = null;
        public static ISlot fsSlot = null; // The equivalent of the root directory in a filesystem.

        public static void Output(string msg)
        {
            if (consoleSlot == null)
            {
                UniLog.Log($"[Dergwasm] Couldn't find console slot to log this message: {msg}");
                return;
            }
            UniLog.Log($"[Dergwasm] {msg}");
            consoleSlot.GetComponent<FrooxEngine.UIX.Text>().Content.Value += msg;
        }

        public static void Msg(string msg)
        {
            Output($"> {msg}\n");
        }

        public static void DebugMemHex(int ptr, int size)
        {
            Span<byte> mem = machine.HeapSpan(ptr, size);
            string collect = "";
            for (int i = 0; i < mem.Length; ++i)
            {
                collect += $"{mem[i]:X2} ";
                if (i % 8 == 7)
                {
                    Msg($"{i - 7:X4}: {collect}");
                    collect = "";
                }
            }
            if (collect != "")
                Msg($"{(mem.Length / 8) * 8:X4}: {collect}");
        }

        public static void InitStage0(IWorldServices worldServices)
        {
            DergwasmMachine.worldServices = worldServices;
            machine = null;
            moduleInstance = null;
            emscriptenEnv = null;
            emscriptenWasi = null;
            resoniteEnv = null;
            filesystemEnv = null;
            DergwasmMachine.dergwasmSlot = null;
            consoleSlot = null;
            fsSlot = null;

            ISlot dergwasmSlot = worldServices
                .GetRootSlot()
                .FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
            if (dergwasmSlot == null)
            {
                Msg(
                    $"Couldn't find dergwasm slot with tag _dergwasm in world {worldServices.GetName()}"
                );
                return;
            }
            ISlot byteDisplay = dergwasmSlot.FindChild(
                s => s.Tag == "_dergwasm_byte_display",
                maxDepth: 0
            );
            // We expect a slot with the tag _dergwasm_byte_display to exist, and to have
            // a StaticBinary component.
            ISlot wasmBinarySlot = dergwasmSlot.FindChild(
                s => s.Tag == "_dergwasm_wasm_file",
                maxDepth: 0
            );

            if (wasmBinarySlot == null)
            {
                Msg(
                    $"Couldn't find WASM binary slot with tag _dergwasm_wasm_file in world {worldServices.GetName()}"
                );
                return;
            }

            if (byteDisplay == null)
            {
                Msg(
                    $"Couldn't find byte display slot with tag _dergwasm_byte_display in world {worldServices.GetName()}"
                );
                return;
            }

            StaticBinary binary = wasmBinarySlot.GetComponent<StaticBinary>();
            if (binary == null)
            {
                Msg(
                    $"Couldn't access WASM StaticBinary component in world {worldServices.GetName()}"
                );
                return;
            }

            BinaryAssetLoader loader = new BinaryAssetLoader(worldServices, binary, byteDisplay);

            Task<string> task = worldServices.StartTask(loader.Load);
            if (task == null)
            {
                Msg($"Couldn't start task in world {worldServices.GetName()}");
                return;
            }
        }

        public static void Init(IWorldServices worldServices, string filename)
        {
            try
            {
                dergwasmSlot = worldServices
                    .GetRootSlot()
                    .FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
                consoleSlot = dergwasmSlot?.FindChild(s => s.Tag == "_dergwasm_console_content");
                fsSlot = dergwasmSlot?.FindChild(s => s.Tag == "_dergwasm_fs_root");

                if (consoleSlot != null)
                    consoleSlot.GetComponent<FrooxEngine.UIX.Text>().Content.Value = "";

                Msg($"Dergwasm v{typeof(Dergwasm).Assembly.GetName().Version}");
                Msg("Init called");
                machine = new Machine();
                // machine.Debug = true;

                // Register all the environments.
                emscriptenEnv = new EmscriptenEnv(machine);
                emscriptenEnv.RegisterHostFuncs();
                emscriptenEnv.outputWriter = Output;

                emscriptenWasi = new EmscriptenWasi(machine, emscriptenEnv);
                emscriptenWasi.RegisterHostFuncs();

                resoniteEnv = new ResoniteEnv(machine, worldServices, emscriptenEnv);
                resoniteEnv.RegisterHostFuncs();

                filesystemEnv = new FilesystemEnv(machine, fsSlot, emscriptenEnv, emscriptenWasi);
                filesystemEnv.RegisterHostFuncs();

                // Read and parse the WASM file.
                Module module;

                Msg("Opening WASM file");
                using (var stream = File.OpenRead(filename))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    module = Module.Read("wasm_main", reader);
                }
                Msg("WASM file read");
                machine.MainModuleName = module.ModuleName;

                module.ResolveExterns(machine);
                moduleInstance = module.Instantiate(machine);
                machine.mainModuleInstance = moduleInstance;
                CheckForUnimplementedInstructions();
                Msg("No unimplemented WASM instructions found");

                // Run any initializers we might find.
                MaybeRunEmscriptenCtors();
                MaybeInitMicropython(64 * 1024);

                // Initialize the primitive serialization buffer. This relies on
                // having a working malloc in WASM.
                SimpleSerialization.Initialize(resoniteEnv);
            }
            catch (Exception e)
            {
                Msg($"Exception: {e}");
            }
        }

        static void CheckForUnimplementedInstructions()
        {
            HashSet<InstructionType> needed = new HashSet<InstructionType>();
            foreach (var f in machine.funcs)
            {
                if (f is HostFunc)
                    continue;
                ModuleFunc func = (ModuleFunc)f;
                foreach (var instr in func.Code)
                {
                    if (!InstructionEvaluation.Map.ContainsKey(instr.Type))
                    {
                        needed.Add(instr.Type);
                    }
                }
            }

            if (needed.Count == 0)
                return;

            Msg("Unimplemented instructions:");
            foreach (var instr in needed)
            {
                Msg($"  {instr}");
            }
            throw new Trap("Unimplemented instructions");
        }

        static void MaybeRunEmscriptenCtors()
        {
            Func ctors = machine.GetFunc(moduleInstance.ModuleName, "__wasm_call_ctors");
            if (ctors == null)
                return;
            Msg("Running __wasm_call_ctors");
            Frame frame = new Frame(ctors as ModuleFunc, moduleInstance, null);
            frame.Label = new Label(0, 0);
            frame.InvokeFunc(machine, ctors);
            Msg("Completed __wasm_call_ctors");
        }

        static void MaybeInitMicropython(int stackSizeBytes)
        {
            Func mp_js_init = machine.GetFunc(moduleInstance.ModuleName, "mp_js_init");
            if (mp_js_init == null)
                return;
            Msg($"Running mp_js_init with stack size {stackSizeBytes} bytes");
            try
            {
                Frame frame = new Frame(mp_js_init as ModuleFunc, moduleInstance, null);
                frame.Label = new Label(1, 0);
                frame.Push(new Value { s32 = stackSizeBytes });
                frame.InvokeFunc(machine, mp_js_init);
                Msg("Completed mp_js_init");
            }
            catch (ExitTrap)
            {
                Msg("mp_js_init exited");
            }
        }
    }
}
