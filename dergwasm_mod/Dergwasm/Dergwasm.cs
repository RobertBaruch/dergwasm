using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core; // For UniLog
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using static FrooxEngine.SessionControlDialog;

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

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch(nameof(World.Load))]
        class StartRunningPatch
        {
            static void Postfix(World __instance)
            {
                UniLog.Log("Postfix called on World.Load");
                UniLog.Log($"... WorldName {__instance.Configuration.WorldName.Value}");
                UniLog.Log($"... SessionID {__instance.Configuration.SessionID.Value}");
                Slot dergwasmSlot = __instance
                    .RootSlot
                    .FindChild(s => s.Tag == "_dergwasm", maxDepth: 0);
                if (dergwasmSlot == null)
                {
                    dergwasmSlot = __instance.RootSlot.AddSlot("Dergwasm");
                    dergwasmSlot.Tag = "_dergwasm";
                }
            }
        }
    }
}
