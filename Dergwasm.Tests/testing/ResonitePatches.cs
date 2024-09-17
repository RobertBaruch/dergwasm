using System;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;

namespace DergwasmTests.testing
{
    public class ResonitePatches
    {
        public static void Apply()
        {
            if (Harmony.HasAnyPatches("dev.xekri.Dergwasm"))
                return;
            Harmony harmony = new Harmony("dev.xekri.Dergwasm");
            harmony.PatchAll();
            Console.WriteLine("Patched!");
        }

        [HarmonyPatch(typeof(SyncElement), "BeginModification")]
        class SyncElementBeginModificationPatch
        {
            static bool Prefix(SyncElement __instance, ref bool __result, bool throwOnError)
            {
                FieldInfo fieldInfo = typeof(SyncElement).GetField(
                    "modificationLevel",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                fieldInfo.SetValue(__instance, (ushort)1);
                __result = true;
                return false; // skip original method
            }
        }

        [HarmonyPatch(typeof(SyncElement), "get_GenerateSyncData")]
        class SyncElementGetGenerateSyncDataPatch
        {
            static bool Prefix(ref bool __result)
            {
                __result = false;
                return false; // skip original method
            }
        }
    }
}
