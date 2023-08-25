using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace MechHumanlikes
{
    public class MDR_Pawn_HealthTracker_Patch
    {
        // Upon a pawn being downed, Martyrdom drones should die if they are foreign (non-player).
        [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
        public static class MakeDowned_Patch
        {
            [HarmonyPrefix]
            public static bool Listener(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff, ref Pawn ___pawn)
            {
                if (___pawn.Faction != Faction.OfPlayer && MDR_Utils.IsProgrammableDrone(___pawn) && ___pawn.GetComp<CompReprogrammableDrone>().ActiveDirectives.Contains(MDR_DirectiveDefOf.MDR_DirectiveMartyrdom))
                {
                    ___pawn.Kill(dinfo, hediff);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
