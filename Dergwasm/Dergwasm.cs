using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Derg.Wasm;
using Derg.Instructions;
using Derg.Runtime;
using Elements.Core; // For UniLog
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using Dergwasm.Runtime;

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
                WorldServices worldServices = new WorldServices(__instance);
                DergwasmMachine.InitStage0(worldServices, new DergwasmSlots(worldServices));
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
                    {
                        WorldServices worldServices = new WorldServices(context.World);
                        DergwasmMachine.InitStage0(worldServices, new DergwasmSlots(worldServices));
                    }
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
        public static IDergwasmSlots dergwasmSlots = null;
        public static Machine machine = null;
        public static ModuleInstance moduleInstance = null;
        public static EmscriptenEnv emscriptenEnv = null;
        public static EmscriptenWasi emscriptenWasi = null;
        public static ResoniteEnv resoniteEnv = null;
        public static FilesystemEnv filesystemEnv = null;
        public static bool initialized = false;

        public static void Output(string msg)
        {
            if (dergwasmSlots?.ConsoleSlot == null)
            {
                UniLog.Log($"[Dergwasm] Couldn't find console slot to log this message: {msg}");
                return;
            }
            UniLog.Log($"[Dergwasm] {msg}");
            dergwasmSlots.ConsoleSlot.GetComponent<FrooxEngine.UIX.Text>().Content.Value += msg;
        }

        public static void Msg(string msg)
        {
            Output($"> {msg}\n");
        }

        public static void DebugMemHex(Ptr ptr, int size)
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

        public static void InitStage0(IWorldServices worldServices, IDergwasmSlots dergwasmSlots)
        {
            DergwasmMachine.worldServices = worldServices;
            DergwasmMachine.dergwasmSlots = dergwasmSlots;
            machine = null;
            moduleInstance = null;
            emscriptenEnv = null;
            emscriptenWasi = null;
            resoniteEnv = null;
            filesystemEnv = null;
            initialized = false;

            if (!dergwasmSlots.Ready)
            {
                Msg($"[Dergwasm] Slots in world {worldServices.GetName()} are not set up");
                return;
            }

            BinaryAssetLoader loader = new BinaryAssetLoader(worldServices, dergwasmSlots);

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
                if (dergwasmSlots.ConsoleSlot != null)
                    dergwasmSlots.ConsoleSlot.GetComponent<FrooxEngine.UIX.Text>().Content.Value =
                        "";

                Msg($"Dergwasm v{typeof(Dergwasm).Assembly.GetName().Version}");
                Msg("Init called");
                machine = new Machine();
                // machine.Debug = true;

                // Register all the environments.
                emscriptenEnv = new EmscriptenEnv(machine) { outputWriter = Output };
                machine.Allocator = emscriptenEnv;
                machine.RegisterModule(emscriptenEnv);

                emscriptenWasi = new EmscriptenWasi(machine, emscriptenEnv);
                machine.RegisterModule(emscriptenWasi);

                resoniteEnv = new ResoniteEnv(machine, worldServices, emscriptenEnv);
                machine.RegisterModule(resoniteEnv);

                filesystemEnv = new FilesystemEnv(
                    machine,
                    dergwasmSlots.FilesystemSlot,
                    emscriptenEnv,
                    emscriptenWasi
                );
                machine.RegisterModule(filesystemEnv);

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
                initialized = true;
            }
            catch (Exception e)
            {
                Msg($"Exception: {e}");
                throw;
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
