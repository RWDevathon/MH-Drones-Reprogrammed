using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;

namespace MechHumanlikes
{
    public static class MDR_Pawn_StoryTracker_Patch
    {
        // Programmable drones have their disabled work tags identified through their programming comp, and by no other mechanic or feature.
        [HarmonyPatch(typeof(Pawn_StoryTracker), "get_DisabledWorkTagsBackstoryTraitsAndGenes")]
        public class get_DisabledWorkTagsBackstoryTraitsAndGenes_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn ___pawn, ref WorkTags __result)
            {
                if (MDR_Utils.IsProgrammableDrone(___pawn))
                {
                    WorkTags enabledTags = WorkTags.None;
                    List<WorkTypeDef> enabledWorkTypes = ___pawn.GetComp<CompReprogrammableDrone>().enabledWorkTypes;
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
    }
}