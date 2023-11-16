using HarmonyLib;
using RimWorld;
using Verse;

namespace MechHumanlikes
{
    public static class MHC_Utils_Patch
    {
        // Programmable drones have additional mechanics to them, that should be handled after normal drone details are settled.
        // Only do this for pawns that are not part of faction groups - those are handled elsewhere.
        [HarmonyPatch(typeof(MHC_Utils), "ReconfigureDrone")]
        public class ReconfigureDrone_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn pawn)
            {
                if (MDR_Utils.IsProgrammableDrone(pawn))
                {
                    // Factionless units or player-spawned units (newly constructed) should have no programming to start.
                    if (pawn.Faction == null || pawn.Faction == Faction.OfPlayerSilentFail)
                    {
                        MDR_Utils.Deprogram(pawn);
                        pawn.health.AddHediff(MDR_HediffDefOf.MDR_NoProgramming);
                    }
                }
            }
        }
    }
}