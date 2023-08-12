using HarmonyLib;
using Verse;
using RimWorld;

namespace MechHumanlikes
{
    public static class MDR_InteractionUtility_Patch
    {
        // Programmable drones may never initiate random social interactions - these are different than ordered/work-related interactions.
        // Note that normal drones do not need to be checked here as they are incapable of interactions entirely.
        [HarmonyPatch(typeof(InteractionUtility), "CanInitiateRandomInteraction")]
        public class CanInitiateRandomInteraction_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn p, ref bool __result)
            {
                if (MDR_Utils.IsProgrammableDrone(p))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}