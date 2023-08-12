using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MechHumanlikes
{
    public static class MDR_Utils
    {
        internal static List<ThingDef> cachedProgrammableDrones = new List<ThingDef>();

        public static List<DirectiveDef> cachedSortedDirectives = new List<DirectiveDef>();

        public static List<string> directiveCategories = new List<string>();

        public static List<ThingDef> ProgrammableDrones
        {
            get
            {
                return cachedProgrammableDrones;
            }
        }

        public static readonly SimpleCurve skillComplexityThresholds = new SimpleCurve
        {
            new CurvePoint(6, 6),
            new CurvePoint(8, 8),
            new CurvePoint(10, 10),
            new CurvePoint(12, 12),
            new CurvePoint(14, 14),
            new CurvePoint(16, 16),
            new CurvePoint(18, 18),
            new CurvePoint(20, 20)
        };

        // GENERAL UTILITIES

        public static bool IsProgrammableDrone(Pawn pawn)
        {
            return IsProgrammableDrone(pawn.def);
        }

        public static bool IsProgrammableDrone(ThingDef thingDef)
        {
            return ProgrammableDrones.Contains(thingDef);
        }


        // Given a pawn and skill, calculate the threshold at which additional skills will take double complexity to add.
        // The threshold is based on baseline complexity and half of the pawn's inherent skill.
        public static int SkillComplexityThresholdFor(Pawn pawn, SkillDef skill)
        {
            return Mathf.FloorToInt(skillComplexityThresholds.Evaluate(pawn.GetComp<CompReprogrammableDrone>().BaselineComplexity)) + (pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentSkills.GetWithFallback(skill, 0) / 2);
        }

        public static void Deprogram(Pawn pawn)
        {
            if (!pawn.def.HasModExtension<MDR_ProgrammableDroneExtension>())
            {
                Log.Warning("[MDR] A pawn " + pawn.LabelShortCap + ", who is not a programmable drone, had Deprogram called on it.");
                return;
            }

            CompReprogrammableDrone reprogramComp = pawn.GetComp<CompReprogrammableDrone>();
            MDR_ProgrammableDroneExtension reprogramExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();

            // WorkTypes
            reprogramComp.enabledWorkTypes.RemoveAll(workType => !reprogramExtension.inherentWorkTypes.NotNullAndContains(workType) && workType.workTags != WorkTags.None && !workType.relevantSkills.NullOrEmpty());
            pawn.Notify_DisabledWorkTypesChanged();
            reprogramComp.UpdateComplexity("Work Types", 0);

            // Skills
            foreach (SkillRecord skillRecord in pawn.skills?.skills)
            {
                if (reprogramExtension.inherentSkills?.ContainsKey(skillRecord.def) == true)
                {
                    skillRecord.Level = reprogramExtension.inherentSkills[skillRecord.def];
                }
                else
                {
                    skillRecord.Level = 0;
                }
                skillRecord.passion = 0;
                skillRecord.xpSinceLastLevel = 0;
                skillRecord.xpSinceMidnight = 0;
            }
            reprogramComp.UpdateComplexity("Skills", 0);

            // Directives
            reprogramComp.SetDirectives(pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentDirectives);
            reprogramComp.UpdateComplexity("Active Directives", 0);
        }

        // Randomize the given programmable drone's skills, work types, and directives within the constraints of their pawn kind def.
        // If pawn group maker parms is provided as context, randomization will take into account the unit's "role" in a group.
        public static void RandomizeProgrammableDrone(Pawn pawn, PawnGroupMakerParms context = null)
        {
            Deprogram(pawn);

            // Purchase required work types
            RandomizeDroneWorkTypes(pawn, context, true);

            // Purchase skills to be in range min
            RandomizeDroneSkills(pawn, context, true);

            // Purchase directives to be in range min
            RandomizeDroneDirectives(pawn, context, true);

            // With spare complexity, randomly choose to use or not use on directives or skills

        }

        // Randomize programmable drone's work types. If requiredOnly is true, it will only apply required work types from the pawn kind def.
        // If it is false, it will select random work types that it does not already have.
        public static void RandomizeDroneWorkTypes(Pawn pawn, PawnGroupMakerParms context = null, bool requiredOnly = true)
        {
            MDR_ProgrammableDroneExtension pawnExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;

            if (requiredOnly)
            {
                if (pawnKindDef.requiredWorkTags != WorkTags.None)
                {
                    List<WorkTypeDef> legalWorkTypes = new List<WorkTypeDef>();
                    foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
                    {
                        if ((workTypeDef.workTags != WorkTags.None || !workTypeDef.relevantSkills.NullOrEmpty())
                            && !pawnExtension.forbiddenWorkTypes.NotNullAndContains(workTypeDef)
                            && (workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ValidFor(pawn).Accepted ?? true)
                            && !pawnComp.enabledWorkTypes.Contains(workTypeDef))
                        {
                            legalWorkTypes.Add(workTypeDef);
                        }
                    }

                    int requiredWorkTypeComplexity = 0;
                    foreach (WorkTypeDef workTypeDef in legalWorkTypes)
                    {
                        if ((workTypeDef.workTags & pawnKindDef.requiredWorkTags) != WorkTags.None)
                        {
                            pawnComp.enabledWorkTypes.Add(workTypeDef);

                            requiredWorkTypeComplexity += workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ComplexityCostFor(pawn, true) ?? 1;
                        }
                    }

                    // Ensure the pawn has both combat work types enabled if it must be a fighter.
                    if (context != null && context.generateFightersOnly)
                    {
                        foreach (WorkTypeDef workTypeDef in legalWorkTypes)
                        {
                            if ((workTypeDef.workTags & WorkTags.Violent) != WorkTags.None && !pawnComp.enabledWorkTypes.Contains(workTypeDef))
                            {
                                pawnComp.enabledWorkTypes.Add(workTypeDef);

                                requiredWorkTypeComplexity += workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ComplexityCostFor(pawn, true) ?? 1;
                            }
                        }
                    }


                    if (requiredWorkTypeComplexity != 0)
                    {
                        pawnComp.UpdateComplexity("Work Types", requiredWorkTypeComplexity + pawnComp.GetComplexityFromSource("Work Types"));
                    }
                }
            }
            else
            {
                // Create randomization, has to account for the context group maker adding weights for work types 
            }
        }

        // Randomize programmable drone's skills. If requiredOnly is true, it will only select skills that get it to the pawn kind def's min skill range.
        // If it is false, it will select random skills, weighted by various factors.
        public static void RandomizeDroneSkills(Pawn pawn, PawnGroupMakerParms context = null, bool requiredOnly = true)
        {
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;
            List<SkillRange> skillRanges = pawnKindDef.skills;

            if (requiredOnly && !skillRanges.NullOrEmpty())
            {
                int requiredSkillComplexity = 0;
                foreach (SkillRange skillRange in skillRanges)
                {
                    SkillDef skillDef = skillRange.Skill;
                    int skillComplexityThreshold = SkillComplexityThresholdFor(pawn, skillDef);
                    SkillRecord skillRecord = pawn.skills.GetSkill(skillDef);
                    int skillFloor = skillRange.Range.min;
                    if (skillRecord.Level > skillFloor)
                    {
                        continue;
                    }

                    if (skillFloor > skillComplexityThreshold && skillComplexityThreshold > skillRecord.Level)
                    {
                        requiredSkillComplexity += Mathf.CeilToInt(Mathf.Max(0.5f * (skillComplexityThreshold - skillRecord.Level), 0) + skillFloor - skillComplexityThreshold);
                    }
                    else if (skillFloor > skillComplexityThreshold)
                    {
                        requiredSkillComplexity += skillFloor - skillRecord.Level;
                    }
                    else
                    {
                        requiredSkillComplexity += Mathf.CeilToInt(0.5f * (skillFloor - skillRecord.Level));
                    }
                    skillRecord.Level = skillFloor;
                }
                pawnComp.UpdateComplexity("Skills", requiredSkillComplexity + pawnComp.GetComplexityFromSource("Skills"));
            }
            else
            {
                // Create randomization, has to account for the context group maker and pawn kind adding weights for skills
            }
        }

        // Randomize programmable drone's directives. If requiredOnly is true, it will only select directives that are required by its pawn kind.
        // If it is false, it will select random directives, weighted by various factors.
        public static void RandomizeDroneDirectives(Pawn pawn, PawnGroupMakerParms context = null, bool requiredOnly = true)
        {
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;
            MDR_ProgrammableDroneKindExtension pawnKindExtension = pawnKindDef.GetModExtension<MDR_ProgrammableDroneKindExtension>();
            MDR_ProgrammableDroneExtension pawnExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();

            List<DirectiveDef> activeDirectiveDefs = new List<DirectiveDef>();
            activeDirectiveDefs.AddRange(pawnExtension.inherentDirectives);
            activeDirectiveDefs.AddRange(pawnComp.ActiveDirectives);
            pawnComp.SetDirectives(activeDirectiveDefs);

            if (requiredOnly && pawnKindExtension?.requiredDirectives.NullOrEmpty() == false)
            {
                int requiredDirectiveComplexity = 0;
                foreach (DirectiveDef directiveDef in pawnKindExtension.requiredDirectives)
                {
                    if (activeDirectiveDefs.Contains(directiveDef))
                    {
                        continue;
                    }

                    requiredDirectiveComplexity += directiveDef.complexityCost;
                    activeDirectiveDefs.Add(directiveDef);
                }

                pawnComp.UpdateComplexity("Active Directives", requiredDirectiveComplexity + pawnComp.GetComplexityFromSource("Active Directives"));
                pawnComp.SetDirectives(activeDirectiveDefs);
            }
            else
            {
                // Create randomization, has to account for the context group maker and pawn kind adding weights for skills
            }
        }
    }
}
