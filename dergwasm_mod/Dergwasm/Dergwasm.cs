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
                        $"Couldn't access dergwasm slot in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }
                Slot byteDisplay = dergwasmSlot.FindChild(
                    s => s.Tag == "_dergwasm_byte_display",
                    maxDepth: 0
                );
                Slot firmwareSlot = dergwasmSlot.FindChild(
                    s => s.Name == "firmware.wasm",
                    maxDepth: 0
                );

                if (firmwareSlot == null)
                {
                    Msg(
                        $"Couldn't access firmware slot in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }

                if (byteDisplay == null)
                {
                    Msg(
                        $"Couldn't access byte display slot in world {__instance.Configuration.WorldName.Value}"
                    );
                    return;
                }

                StaticBinary binary = firmwareSlot.GetComponent<StaticBinary>();
                if (binary == null)
                {
                    Msg(
                        $"Couldn't access firmware binary in world {__instance.Configuration.WorldName.Value}"
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
                DergwasmMachine.Msg("_dergwasm called on DynamicImpulseTrigger");
                // TODO: Call ResoniteEnv.CallWasmFunction(hierarchy)

                Slot programSlot = hierarchy.FindChild(
                    s => s.Tag == "_dergwasm_micropython_program"
                );
                if (programSlot == null)
                {
                    DergwasmMachine.Msg("Couldn't find program slot");
                    return;
                }
                string program = programSlot.GetComponent<TextRenderer>().Text.Value;
                try
                {
                    DergwasmMachine.MicropythonDoStr(program);
                }
                catch (Exception e)
                {
                    DergwasmMachine.Msg($"Exception: {e}");
                }

                //Slot child = hierarchy[0];
                //if (child == null)
                //    return;
                //foreach (Component c in child.Components)
                //{
                //    if (c is ValueField<int> intField)
                //    {
                //        intField.Value.Value += 1;
                //        break;
                //    }
                //}
            }
        }
    }

    public static class DergwasmMachine
    {
        public static World world = null;
        public static Machine machine = null;
        public static ModuleInstance moduleInstance = null;
        public static EmscriptenEnv emscriptenEnv = null;

        public static void Output(string msg)
        {
            UniLog.Log($"[Dergwasm] {msg}");
            if (world == null)
            {
                UniLog.Log($"[Dergwasm] World is null");
                return;
            }
            Slot dergwasmSlot = world.RootSlot.FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
            if (dergwasmSlot == null)
            {
                UniLog.Log($"[Dergwasm] Couldn't find dergwasm slot");
                return;
            }
            Slot consoleSlot = dergwasmSlot.FindChild(s => s.Tag == "_dergwasm_console_content");
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
                Msg("Init called");
                machine = new Machine();
                // machine.Debug = true;

                new EmscriptenWasi(machine).RegisterHostFuncs();

                emscriptenEnv = new EmscriptenEnv(machine);
                emscriptenEnv.RegisterHostFuncs();
                emscriptenEnv.outputWriter = Output;

                ResoniteEnv resoniteEnv = new ResoniteEnv(machine, world, emscriptenEnv);
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
                Msg("No unimplemented instructions found");

                MaybeRunEmscriptenCtors();

                InitMicropython(64 * 1024);
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

        static int AddUTF8StringToStack(string s)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            int size = utf.Length + 1;
            Frame frame = new Frame(null, moduleInstance, null);
            frame.Label = new Label(1, 0);
            int stackPtr = emscriptenEnv.stackAlloc(frame, size);

            Array.Copy(utf, 0, machine.Memory0, stackPtr, utf.Length);
            machine.Memory0[stackPtr + utf.Length] = 0; // NUL-termination

            return stackPtr;
        }

        public static void MicropythonDoStr(string s)
        {
            if (machine == null)
            {
                throw new Trap("MicropythonDoStr: Machine is null");
            }
            int stackPtr = AddUTF8StringToStack(s);
            MicropythonDoStr(stackPtr);
        }

        static void MicropythonDoStr(int stackPtr)
        {
            Func mp_js_do_str = machine.GetFunc(moduleInstance.ModuleName, "mp_js_do_str");
            if (mp_js_do_str == null)
            {
                throw new Trap("No mp_js_do_str function found");
            }
            Msg($"Running mp_js_do_str");
            try
            {
                Frame frame = new Frame(mp_js_do_str as ModuleFunc, moduleInstance, null);
                frame.Label = new Label(1, 0);
                frame.Push(new Value(stackPtr)); // source
                frame.InvokeFunc(machine, mp_js_do_str);
            }
            catch (ExitTrap)
            {
                Msg("MicroPython exited");
            }
        }

        static void InitMicropython(int stackSizeBytes)
        {
            Func mp_js_init = machine.GetFunc(moduleInstance.ModuleName, "mp_js_init");
            if (mp_js_init == null)
            {
                throw new Trap("No mp_js_init function found");
            }
            Msg($"Running mp_js_init with stack size {stackSizeBytes} bytes");
            try
            {
                Frame frame = new Frame(mp_js_init as ModuleFunc, moduleInstance, null);
                frame.Label = new Label(1, 0);
                frame.Push(new Value(stackSizeBytes));
                frame.InvokeFunc(machine, mp_js_init);
                Msg($"Completed mp_js_init");
            }
            catch (ExitTrap)
            {
                Msg($"mp_js_init exited");
            }
        }
    }
}
