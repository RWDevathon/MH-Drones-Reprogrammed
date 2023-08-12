using HarmonyLib;
using Verse;
using System.Collections.Generic;
using static MechHumanlikes.InteractionUtility_Patch;
using System.Reflection.Emit;
using System.Reflection;
using RimWorld;
using System;

namespace MechHumanlikes
{
    public static class CanInitiateInteraction_Patch_Patch
    {
        // Reprogrammable drones
        [HarmonyPatch(typeof(CanInitiateInteraction_Patch), "Prefix")]
        public class Prefix_Patch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, ILGenerator generator)
            {
                List<CodeInstruction> instructions = new List<CodeInstruction>(insts);
                MethodInfo targetMethod = AccessTools.Method(typeof(MHC_Utils), nameof(MHC_Utils.IsConsideredMechanicalDrone), new Type[] { typeof(Pawn) });

                for (int i = 0; i < instructions.Count; i++)
                {
                    yield return instructions[i];
                    if (instructions[i].operand as MethodBase == targetMethod)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // Load Pawn
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Prefix_Patch), nameof(IncapableOfInitiatingInteractions))); // Our function call
                    }
                }
            }

            // Organics and mechanical sapients may initiate interactions. Reprogrammable drones that have social or animal skills enabled may initiate interactions.
            // Drones that can't be reprogrammed or that do not have the appropriate skills may not.
            private static bool IncapableOfInitiatingInteractions(bool isDrone, Pawn pawn)
            {
                if (!MDR_Utils.IsProgrammableDrone(pawn))
                {
                    return isDrone;
                }

                if (pawn.skills is Pawn_SkillTracker skills && (!skills.GetSkill(SkillDefOf.Social).TotallyDisabled || !skills.GetSkill(SkillDefOf.Animals).TotallyDisabled))
                {
                    return false;
                }
                return true;
            }
        }
    }
}