using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
                Slot dergwasmSlot = __instance.RootSlot.FindChild(
                    s => s.Tag == "_dergwasm",
                    maxDepth: 0
                );
                if (dergwasmSlot == null)
                {
                    dergwasmSlot = __instance.RootSlot.AddSlot("Dergwasm");
                    dergwasmSlot.Tag = "_dergwasm";
                }
                if (dergwasmSlot == null)
                {
                    Msg(
                        $"Couldn't find dergwasm slot with tag _dergwasm in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }
                Slot byteDisplay = dergwasmSlot.FindChild(
                    s => s.Tag == "_dergwasm_byte_display",
                    maxDepth: 0
                );
                // We expect a slot with the tag _dergwasm_byte_display to exist, and to have
                // a StaticBinary component.
                Slot wasmBinarySlot = dergwasmSlot.FindChild(
                    s => s.Tag == "_dergwasm_wasm_file",
                    maxDepth: 0
                );

                if (wasmBinarySlot == null)
                {
                    Msg(
                        $"Couldn't find WASM binary slot with tag _dergwasm_wasm_file in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }

                if (byteDisplay == null)
                {
                    Msg(
                        $"Couldn't find byte display slot with tag _dergwasm_byte_display in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }

                StaticBinary binary = wasmBinarySlot.GetComponent<StaticBinary>();
                if (binary == null)
                {
                    Msg(
                        $"Couldn't access WASM StaticBinary component in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }

                BinaryAssetLoader loader = new BinaryAssetLoader(__instance, binary, byteDisplay);

                // Equivalent to Worker.StartGlobalTask
                Task<string> task = __instance.Coroutines.StartTask<string>(loader.Load);
                if (task == null)
                {
                    Msg($"Couldn't start task in world {__instance.Configuration.WorldName.Value}");
                    return;
                }
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
                if (tag != "_dergwasm" || hierarchy == null || hierarchy.Tag != "_dergwasm_args")
                    return;

                try
                {
                    DergwasmMachine.resoniteEnv.CallWasmFunction(hierarchy);
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
        public static World world = null;
        public static Machine machine = null;
        public static ModuleInstance moduleInstance = null;
        public static EmscriptenEnv emscriptenEnv = null;
        public static ResoniteEnv resoniteEnv = null;
        public static Slot dergwasmSlot = null;
        public static Slot consoleSlot = null;

        public static void Output(string msg)
        {
            if (consoleSlot == null)
            {
                UniLog.Log($"[Dergwasm] Couldn't find console slot");
                return;
            }
            consoleSlot.GetComponent<FrooxEngine.UIX.Text>().Content.Value += msg;
        }

        public static void Msg(string msg)
        {
            Output($"> {msg}\n");
        }

        public static void Init(World world, string filename)
        {
            DergwasmMachine.world = world;
            try
            {
                dergwasmSlot = world.RootSlot.FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
                consoleSlot = dergwasmSlot?.FindChild(s => s.Tag == "_dergwasm_console_content");

                Msg("Init called");
                machine = new Machine();
                // machine.Debug = true;

                new EmscriptenWasi(machine).RegisterHostFuncs();

                emscriptenEnv = new EmscriptenEnv(machine);
                emscriptenEnv.RegisterHostFuncs();
                emscriptenEnv.outputWriter = Output;

                resoniteEnv = new ResoniteEnv(machine, world, emscriptenEnv);
                resoniteEnv.RegisterHostFuncs();

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
                CheckForUnimplementedInstructions();
                Msg("No unimplemented WASM instructions found");

                MaybeRunEmscriptenCtors();
                MaybeInitMicropython(64 * 1024);
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
                frame.Push(new Value(stackSizeBytes));
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
