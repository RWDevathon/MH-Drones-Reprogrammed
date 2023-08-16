using HarmonyLib;
using Verse;
using RimWorld;

namespace MechHumanlikes
{
    public static class CharacterCardUtillity_Patch
    {
        // Programmable drones have work types disabled by a singular cause - programming.
        [HarmonyPatch(typeof(CharacterCardUtility), "GetWorkTypeDisabledCausedBy")]
        public class GetWorkTypeDisabledCausedBy_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn, WorkTags workTag, ref string __result)
            {
                if (MDR_Utils.IsProgrammableDrone(pawn))
                {
                    __result = "MDR_IncapableOfTooltipProgramming".Translate() + ": \n";
                    return false;
                }
                return true;
            }
        }
    }
}