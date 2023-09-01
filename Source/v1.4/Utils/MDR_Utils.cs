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

        public static Dictionary<Map, int> amicableDroneCount = new Dictionary<Map, int>();

        public static IEnumerable<ThingDef> ProgrammableDrones
        {
            get
            {
                for (int i = cachedProgrammableDrones.Count - 1; i >= 0; i--)
                {
                    if (MHC_Utils.IsConsideredMechanicalDrone(cachedProgrammableDrones[i]))
                    {
                        yield return cachedProgrammableDrones[i];
                    }
                }
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


        // Given a pawn and skill, calculate and return the complexity cost of the pawn's current skill level.
        public static float SkillComplexityCostFor(Pawn pawn, SkillDef skill)
        {
            int offsetSkillLevel = pawn.skills.GetSkill(skill).Level - pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentSkills.GetWithFallback(skill, 0);
            if (offsetSkillLevel < 0)
            {
                Log.Warning("[MDR] A programmable drone, " + pawn.LabelShortCap + ", had a lower skill level than their inherent skills. Complexity calculations may be incorrect.");
            }
            return 0.5f * offsetSkillLevel;
        }

        // Given a pawn and skill, calculate the maximum level the pawn may have in the skill.
        public static float SkillLimitFor(Pawn pawn, SkillDef skill)
        {
            return pawn.GetStatValue(MDR_StatDefOf.MDR_SkillLimit) + pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>()?.inherentSkills?.GetWithFallback(skill, 0) ?? 0;
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
            SetRequiredDroneWorkTypes(pawn, context);

            // Purchase skills to be in range min
            SetRequiredDroneSkills(pawn, context);

            // Purchase directives to be in range min
            SetRequiredDroneDirectives(pawn, context);

            // With spare complexity, randomly choose to use or not use on directives, work types, and skills.
            RandomizeDroneCharacteristics(pawn, context);
        }

        // Randomize programmable drone's work types. If requiredOnly is true, it will only apply required work types from the pawn kind def.
        // If it is false, it will select random work types that it does not already have.
        public static void SetRequiredDroneWorkTypes(Pawn pawn, PawnGroupMakerParms context = null)
        {
            MDR_ProgrammableDroneExtension pawnExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;

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

        // Randomize programmable drone's skills based on the group kind context and the pawn kind.
        public static void SetRequiredDroneSkills(Pawn pawn, PawnGroupMakerParms context = null)
        {
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;
            List<SkillRange> skillRanges = pawnKindDef.skills;

            if (!skillRanges.NullOrEmpty())
            {
                float requiredSkillComplexity = pawnComp.GetComplexityFromSource("Skills");
                foreach (SkillRange skillRange in skillRanges)
                {
                    SkillDef skillDef = skillRange.Skill;
                    SkillRecord skillRecord = pawn.skills.GetSkill(skillDef);
                    int skillFloor = skillRange.Range.min;
                    int skillLevel = skillRecord.Level;
                    if (skillLevel >= skillFloor)
                    {
                        continue;
                    }

                    float skillComplexityCost = SkillComplexityCostFor(pawn, skillDef);
                    float skillComplexityUsage = 0;
                    while (skillLevel < skillFloor)
                    {
                        skillComplexityUsage += skillComplexityCost;
                        skillComplexityCost += 0.5f;
                        skillLevel++;
                    }
                    requiredSkillComplexity += skillComplexityUsage;
                    skillRecord.Level = skillFloor;
                }
                pawnComp.UpdateComplexity("Skills", Mathf.CeilToInt(requiredSkillComplexity));
            }
        }

        // Randomize programmable drone's directives based on the group kind context and the pawn kind.
        public static void SetRequiredDroneDirectives(Pawn pawn, PawnGroupMakerParms context = null)
        {
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;
            MDR_ProgrammableDroneKindExtension pawnKindExtension = pawnKindDef.GetModExtension<MDR_ProgrammableDroneKindExtension>();
            MDR_ProgrammableDroneExtension pawnExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();

            List<DirectiveDef> activeDirectiveDefs = new List<DirectiveDef>();
            activeDirectiveDefs.AddRange(pawnExtension.inherentDirectives);
            activeDirectiveDefs.AddRange(pawnComp.ActiveDirectives);
            pawnComp.SetDirectives(activeDirectiveDefs);

            if (pawnKindExtension?.requiredDirectives.NullOrEmpty() == false)
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
        }

        // Randomize programmable drone's directives, work types, and skills. This is dependent upon the pawn kind primarily, and assumes required characteristics are already set.
        public static void RandomizeDroneCharacteristics(Pawn pawn, PawnGroupMakerParms context = null)
        {
            CompReprogrammableDrone pawnComp = pawn.GetComp<CompReprogrammableDrone>();
            PawnKindDef pawnKindDef = pawn.kindDef;
            MDR_ProgrammableDroneKindExtension pawnKindExtension = pawnKindDef.GetModExtension<MDR_ProgrammableDroneKindExtension>();
            MDR_ProgrammableDroneExtension pawnExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();
            List<SkillRange> skillRanges = pawnKindDef.skills;

            // Get the complexity spare for discretionary spending on purchasing random effects. If the pawn kind does not exist, give 0-10 complexity.
            float discretionaryComplexity;
            if (pawnKindExtension == null)
            {
                discretionaryComplexity = Rand.RangeInclusive(0, 10);
            }
            else
            {
                discretionaryComplexity = pawnKindExtension.discretionaryComplexity.RandomInRange;
            }

            if (discretionaryComplexity < 0.5f)
            {
                return;
            }

            // Randomize directives
            List<DirectiveDef> activeDirectiveDefs = new List<DirectiveDef>();
            activeDirectiveDefs.AddRange(pawnComp.ActiveDirectives);
            int discretionaryDirectives = Mathf.Min(
                pawnKindExtension.discretionaryDirectives.RandomInRange,
                pawnExtension.maxDirectives - (activeDirectiveDefs.Count - pawnExtension.inherentDirectives?.Count ?? 0));

            if (discretionaryDirectives > 0)
            {
                List<DirectiveDef> desiredDirectiveDefs = new List<DirectiveDef>();
                desiredDirectiveDefs.AddRange(pawnComp.ActiveDirectives);

                // Acquire a list of legal directives with their weighted chance to be selected in this instance as a pair.
                List<KeyValuePair<DirectiveDef, float>> legalDirectiveDefsWeighted = new List<KeyValuePair<DirectiveDef, float>>();
                foreach (DirectiveDef directiveDef in cachedSortedDirectives)
                {
                    // If the pawn already has this directive, skip it.
                    if (activeDirectiveDefs.Contains(directiveDef))
                    {
                        continue;
                    }

                    // Only valid directives with complexity lower than the spending limit are acceptable.
                    if (directiveDef.ValidFor(pawn) && directiveDef.complexityCost < discretionaryComplexity)
                    {
                        float selectionWeight = 1f;
                        // If this pawn is part of a group, consider the group kind weights of the directive.
                        if (!directiveDef.groupKindWeights.NullOrEmpty() && context != null)
                        {
                            float groupKindWeight = directiveDef.groupKindWeights.GetWithFallback(context.groupKind, 1f);
                            // If the group kind weight indicates this directive is undesirable, skip to the next directive.
                            if (groupKindWeight <= 0)
                            {
                                continue;
                            }
                            else
                            {
                                selectionWeight *= groupKindWeight;
                            }
                        }

                        // If this directive has skill weights, consider those weights in comparison to the pawn's skills.
                        if (!directiveDef.skillChoiceWeights.NullOrEmpty())
                        {
                            float skillWeight = 1f;
                            foreach (KeyValuePair<SkillDef, float> skillWeightPair in directiveDef.skillChoiceWeights)
                            {
                                SkillRecord pawnSkill = pawn.skills.GetSkill(skillWeightPair.Key);
                                // Positive weights mean the skill must be enabled, non-positive weights mean the skill must be disabled.
                                if ((pawnSkill.TotallyDisabled && skillWeightPair.Value > 0f) || (!pawnSkill.TotallyDisabled && skillWeightPair.Value <= 0))
                                {
                                    continue;
                                }
                                else
                                {
                                    skillWeight *= skillWeightPair.Value;
                                }
                            }
                            selectionWeight *= skillWeight;
                        }

                        // Pair the calculated selection weight and the legal def for use in randomization.
                        legalDirectiveDefsWeighted.Add(new KeyValuePair<DirectiveDef, float>(directiveDef, selectionWeight));
                    }
                }

                if (legalDirectiveDefsWeighted.Count > 0)
                {
                    int directiveComplexity = 0;
                    // Keep picking random directives until there's no valid ones, complexity left, or directive slots left.
                    while (legalDirectiveDefsWeighted.Count > 0 && discretionaryComplexity > 0 && discretionaryDirectives > 0)
                    {
                        legalDirectiveDefsWeighted.TryRandomElementByWeight((valuePair => valuePair.Value), out KeyValuePair<DirectiveDef, float> result);
                        // Previously added directives may change the discretionaryComplexity available without recalculating validity for the list.
                        if (result.Key.complexityCost > discretionaryComplexity)
                        {
                            legalDirectiveDefsWeighted.Remove(result);
                        }
                        else
                        {
                            activeDirectiveDefs.Add(result.Key);
                            legalDirectiveDefsWeighted.Remove(result);
                            directiveComplexity += result.Key.complexityCost;
                            discretionaryComplexity -= result.Key.complexityCost;
                            discretionaryDirectives--;
                        }
                    }
                    pawnComp.UpdateComplexity("Active Directives", directiveComplexity + pawnComp.GetComplexityFromSource("Active Directives"));
                    pawnComp.SetDirectives(activeDirectiveDefs);
                }
            }

            // If all discretionary complexity was used by directives, stop here.
            if (discretionaryComplexity < 0.5f)
            {
                return;
            }

            // Randomize Work Types
            int discretionaryWorkTypes = pawnKindExtension.discretionaryWorkTypes.RandomInRange;
            if (discretionaryWorkTypes > 0)
            {
                List<WorkTypeDef> legalWorkTypes = new List<WorkTypeDef>();
                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
                {
                    MDR_WorkTypeExtension workExtension = workTypeDef.GetModExtension<MDR_WorkTypeExtension>();
                    if ((workTypeDef.workTags != WorkTags.None || !workTypeDef.relevantSkills.NullOrEmpty())
                        && !pawnExtension.forbiddenWorkTypes.NotNullAndContains(workTypeDef)
                        && (workExtension?.ValidFor(pawn).Accepted ?? true) && !pawnComp.enabledWorkTypes.Contains(workTypeDef)
                        && discretionaryComplexity >= workExtension?.ComplexityCostFor(pawn, true))
                    {
                        legalWorkTypes.Add(workTypeDef);
                    }
                }

                int requiredWorkTypeComplexity = 0;
                while (legalWorkTypes.Count > 0 && discretionaryComplexity > 0 && discretionaryWorkTypes > 0)
                {
                    legalWorkTypes.TryRandomElement(out WorkTypeDef result);
                    int workTypeCost = result.GetModExtension<MDR_WorkTypeExtension>()?.ComplexityCostFor(pawn, true) ?? 1;
                    if (workTypeCost > discretionaryComplexity)
                    {
                        legalWorkTypes.Remove(result);
                    }
                    else
                    {
                        pawnComp.enabledWorkTypes.Add(result);
                        legalWorkTypes.Remove(result);
                        requiredWorkTypeComplexity += workTypeCost;
                        discretionaryComplexity -= workTypeCost;
                        discretionaryWorkTypes--;
                    }
                }

                pawn.Notify_DisabledWorkTypesChanged();
                pawnComp.UpdateComplexity("Work Types", requiredWorkTypeComplexity + pawnComp.GetComplexityFromSource("Work Types"));
            }

            // If all discretionary complexity was used by work types, stop here.
            if (discretionaryComplexity < 0.5f)
            {
                return;
            }

            // Randomize Skills

            // Assemble a dictionary matching skill records to pairs of the cost to add and the maximum level possible for the skill.
            Dictionary<SkillRecord, DroneSkillContext> skillsToRandomize = new Dictionary<SkillRecord, DroneSkillContext>();
            foreach (SkillRecord skillRecord in pawn.skills.skills)
            {
                if (skillRecord.TotallyDisabled)
                { 
                    continue;
                }
                skillsToRandomize[skillRecord] = new DroneSkillContext(skillRecord);
            }

            if (skillsToRandomize.Count > 0)
            {
                float requiredSkillComplexity = 0;
                while (skillsToRandomize.Count > 0 && discretionaryComplexity > 0)
                {
                    skillsToRandomize.TryRandomElement(out KeyValuePair<SkillRecord, DroneSkillContext> randomSkill);
                    if (randomSkill.Key.Level >= randomSkill.Value.skillCeiling)
                    {
                        skillsToRandomize.Remove(randomSkill.Key);
                    }
                    else
                    {
                        randomSkill.Key.Level++;
                        requiredSkillComplexity += randomSkill.Value.skillComplexityCost;
                        discretionaryComplexity -= randomSkill.Value.skillComplexityCost;
                        randomSkill.Value.skillComplexityCost += 0.5f;
                    }
                }
                pawnComp.UpdateComplexity("Skills", Mathf.Max(0, Mathf.CeilToInt(requiredSkillComplexity + pawnComp.GetComplexityFromSource("Skills"))));
            }
        }

        public static void UpdateAmicableDroneCount(Map map)
        {
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            amicableDroneCount = new Dictionary<Map, int>();
            int resultCount = 0;
            for (int i = playerPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = playerPawns[i];
                if (IsProgrammableDrone(pawn) && pawn.GetComp<CompReprogrammableDrone>().ActiveDirectives.Contains(MDR_DirectiveDefOf.MDR_DirectiveAmicability))
                {
                    resultCount++;
                }
            }
            amicableDroneCount[map] = resultCount;
        }
    }
}
