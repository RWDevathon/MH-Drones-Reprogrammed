using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace MechHumanlikes
{
    public static class QualityUtility_Patch
    {
        // Drones can only produce up to normal quality. Reprogrammable drones with a certain directive may produce fixed Excellent quality goods.
        [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn")]
        [HarmonyPatch(new Type[] { typeof(Pawn), typeof(SkillDef) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        public class GenerateQualityCreatedByPawn_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result)
            {
                if (MHC_Utils.IsConsideredMechanicalDrone(pawn))
                {
                    if (MDR_Utils.IsProgrammableDrone(pawn) && pawn.GetComp<CompReprogrammableDrone>().ActiveDirectives.Contains(MDR_DirectiveDefOf.MDR_DirectiveArtisan))
                    {
                        __result = QualityCategory.Excellent;
                        return false;
                    }
                    else if (__result > QualityCategory.Normal)
                    {
                        __result = QualityCategory.Normal;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}