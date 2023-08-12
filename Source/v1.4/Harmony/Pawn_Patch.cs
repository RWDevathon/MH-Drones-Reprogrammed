using HarmonyLib;
using Verse;
using System.Collections.Generic;

namespace MechHumanlikes
{
    public static class Pawn_Patch
    {
        // Programmable drones have their disabled work types identified through their programming comp, and by no other mechanic or feature.
        [HarmonyPatch(typeof(Pawn), "GetDisabledWorkTypes")]
        public class GetDisabledWorkTypes_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn __instance, ref List<WorkTypeDef> ___cachedDisabledWorkTypes, ref List<WorkTypeDef> __result, bool permanentOnly = false)
            {
                if (MDR_Utils.IsProgrammableDrone(__instance))
                {
                    if (___cachedDisabledWorkTypes == null)
                    {
                        List<WorkTypeDef> allWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                        List<WorkTypeDef> enabledWorkTypes = __instance.GetComp<CompReprogrammableDrone>().enabledWorkTypes;
                        if (enabledWorkTypes == null || enabledWorkTypes.Count == 0)
                        {
                            Log.Warning("[MDR] Pawn " + __instance.LabelShortCap + " has no enabled work types!");
                            return true;
                        }
                        else
                        {
                            ___cachedDisabledWorkTypes = new List<WorkTypeDef>();
                            for (int i = allWorkTypes.Count - 1; i >= 0; i--)
                            {
                                if (!enabledWorkTypes.Contains(allWorkTypes[i]))
                                {
                                    ___cachedDisabledWorkTypes.Add(allWorkTypes[i]);
                                }
                            }
                        }
                    }
                    __result = ___cachedDisabledWorkTypes;
                    return false;
                }
                return true;
            }
        }
    }
}