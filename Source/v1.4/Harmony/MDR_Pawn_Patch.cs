using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace MechHumanlikes
{
    public static class MDR_Pawn_Patch
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

        // Programmable drones have their disabled work tags identified through their programming comp, and by no other mechanic or feature.
        [HarmonyPatch(typeof(Pawn), "get_CombinedDisabledWorkTags")]
        public class get_CombinedDisabledWorkTags_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn __instance, ref List<WorkTypeDef> ___cachedDisabledWorkTypes, ref WorkTags __result)
            {
                if (MDR_Utils.IsProgrammableDrone(__instance))
                {
                    WorkTags enabledTags = WorkTags.None;
                    List<WorkTypeDef> enabledWorkTypes = __instance.GetComp<CompReprogrammableDrone>().enabledWorkTypes;
                    for (int i = enabledWorkTypes.Count - 1; i >= 0; i--)
                    {
                        enabledTags |= enabledWorkTypes[i].workTags;
                    }

                    __result = ~enabledTags;
                    return false;
                }
                return true;
            }
        }

        // Martyrdom directives cause drones to turn into slag when killed (which occurs when they are downed for foreign pawns).
        [HarmonyPatch(typeof(Pawn), "Kill")]
        public static class Kill_Patch
        {
            [HarmonyPrefix]
            public static bool Listener(ref Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
            {
                // Save details and destroy before doing the explosion to avoid the damage hitting the pawn, killing them again.
                if (MDR_Utils.IsProgrammableDrone(__instance) && __instance.GetComp<CompReprogrammableDrone>().ActiveDirectives.Contains(MDR_DirectiveDefOf.MDR_DirectiveMartyrdom) && !__instance.Destroyed)
                {
                    IntVec3 tempPos = __instance.Position;
                    Map tempMap = __instance.Map;
                    __instance.Destroy();
                    GenExplosion.DoExplosion(tempPos, tempMap, 0.4f, DamageDefOf.Bomb, __instance, 1);
                    Thing slag = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel);
                    GenSpawn.Spawn(slag, tempPos, tempMap);
                    return false;
                }
                return true;
            }
        }
    }
}