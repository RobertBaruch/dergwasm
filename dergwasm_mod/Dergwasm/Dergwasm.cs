using System;
using System.Collections.Generic;
using System.Linq;
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
        public override string Version => "0.1.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.xekri.Dergwasm");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(World), nameof(World.Load))]
        class StartRunningPatch
        {
            static void Postfix(World __instance)
            {
                UniLog.Log("Postfix called on World.Load");
                UniLog.Log($"... WorldName {__instance.Configuration.WorldName.Value}");
                UniLog.Log($"... SessionID {__instance.Configuration.SessionID.Value}");
                Slot dergwasmSlot = __instance.RootSlot.FindChild(
                    s => s.Tag == "_dergwasm",
                    maxDepth: 0
                );
                if (dergwasmSlot == null)
                {
                    dergwasmSlot = __instance.RootSlot.AddSlot("Dergwasm");
                    dergwasmSlot.Tag = "_dergwasm";
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
                if (tag == "_dergwasm" && hierarchy != null && hierarchy.Tag == "_dergwasm_args")
                {
                    UniLog.Log("_dergwasm called on DynamicImpulseTrigger");
                    // TODO: Call ResoniteEnv.CallWasmFunction(hierarchy)
                }
            }
        }
    }
}
