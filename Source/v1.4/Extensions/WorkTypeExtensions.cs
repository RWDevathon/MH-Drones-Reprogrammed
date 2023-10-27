using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    // Mod extension to work type defs for integration with reprogrammable drones.
    public class MDR_WorkTypeExtension : DefModExtension
    {
        public WorkTypeDef def;

        public int baseComplexity = 1;

        public int minimumRequiredComplexity = 0;

        // Method for establishing whether a particular pawn may have this work type enabled.
        public AcceptanceReport ValidFor(Pawn pawn)
        {
            CompReprogrammableDrone programComp = pawn.GetComp<CompReprogrammableDrone>();
            if (programComp.MaxComplexity < minimumRequiredComplexity)
            {
                return "MDR_ComplexityRequirementNotMet".Translate(def.labelShort.CapitalizeFirst(), minimumRequiredComplexity, pawn.LabelShortCap, programComp.MaxComplexity);
            }
            return true;
        }

        // Method for establishing how much complexity is required to activate this work type for the pawn.
        public int ComplexityCostFor(Pawn pawn, bool adding)
        {
            int result = baseComplexity;
            if (adding)
            {
                // The additional cost is only applied to skills that are currently disabled.
                // It does not apply if another work type enabled it already.
                foreach (SkillDef skillDef in def.relevantSkills)
                {
                    if (pawn.skills.GetSkill(skillDef).TotallyDisabled)
                    {
                        result += 1;
                    }
                }
            }
            else
            {
                // In order to properly calculate the complexity cost reduction, the worker must take into account other enabled work types.
                // It will only refund the additional complexity for skills that have 0 other work types enabling them.
                List<WorkTypeDef> otherEnabledWorkTypes = new List<WorkTypeDef>();
                foreach (WorkTypeDef workTypeDef in pawn.GetComp<CompReprogrammableDrone>().enabledWorkTypes)
                {
                    if (workTypeDef != def)
                    {
                        otherEnabledWorkTypes.Add(workTypeDef);
                    }
                }

                // Refund all skills that would be disabled if the parent work type were removed. Account for skills that are never disabled and other work types.
                foreach (SkillDef skillDef in def.relevantSkills)
                {
                    if (!otherEnabledWorkTypes.Any(workType => workType.relevantSkills.NotNullAndContains(skillDef)))
                    {
                        result += 1;
                    }
                }
            }
            return result;
        }
    }
}