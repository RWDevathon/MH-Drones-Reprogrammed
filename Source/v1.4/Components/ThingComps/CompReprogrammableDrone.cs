using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    [StaticConstructorOnStartup]
    public class CompReprogrammableDrone: ThingComp
    {
        public List<WorkTypeDef> enabledWorkTypes = new List<WorkTypeDef>();

        private List<DirectiveDef> directiveDefs = new List<DirectiveDef>();

        private Hediff_Complexity complexityHediff;

        private int cachedComplexity = 0;

        private int cachedMaxComplexity = 0;

        private int cachedBaselineComplexity = 0;

        private List<Directive> directives = new List<Directive>();

        // Local reserved storage for saving/loading complexitySources in the ExposeData method.
        private List<string> sourceKey = new List<string>();
        private List<int> sourceValue = new List<int>();

        private Dictionary<string, int> complexitySources = new Dictionary<string, int>();

        public Pawn Pawn => (Pawn)parent;

        public IEnumerable<DirectiveDef> ActiveDirectives => directiveDefs.AsReadOnly();

        public int Complexity
        {
            get
            {
                return cachedComplexity;
            }
        }

        public int MaxComplexity
        {
            get
            {
                return cachedMaxComplexity;
            }
        }

        public int BaselineComplexity
        {
            get
            {
                return cachedBaselineComplexity;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RecalculateComplexity();
            if (complexityHediff == null)
            {
                complexityHediff = (Hediff_Complexity)HediffMaker.MakeHediff(MDR_HediffDefOf.MDR_ComplexityRelation, Pawn);
                Pawn.health.AddHediff(complexityHediff);
            }
            complexityHediff.UpdateHediffStage();

            if (!respawningAfterLoad)
            {
                foreach (Directive directive in directives)
                {
                    directive.PostSpawn(Pawn.Map);
                }
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            foreach (Directive directive in directives)
            {
                directive.PostDespawn(map);
            }
        }

        public override void Notify_MapRemoved()
        {
            base.Notify_MapRemoved();
            foreach (Directive directive in directives)
            {
                directive.PostDespawn(null);
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();

            // Cache the baseline complexity stat from the pawn's statBases. Do this exactly once as it does not change without reloading the game.
            StatDef baselineComplexityStat = MDR_StatDefOf.MDR_ComplexityLimit;
            cachedBaselineComplexity = (int)baselineComplexityStat.defaultBaseValue;
            foreach (StatModifier statMod in Pawn.def.statBases)
            {
                if (statMod.stat == baselineComplexityStat)
                {
                    cachedBaselineComplexity = (int)statMod.value;
                    break;
                }
            }

            InitializeEnabledWorkTypes();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action printComplexities = new Command_Action
                {
                    defaultLabel = "DEV: Log Complexity Sources",
                    action = delegate
                    {
                        Log.Message("[MHC DEBUG] Complexity Sources for " + Pawn);
                        foreach (string sourceKey in complexitySources.Keys)
                        {
                            Log.Message("[MHC DEBUG] " + sourceKey + ": " + complexitySources[sourceKey]);
                        }
                    }
                };
                yield return printComplexities;
                Command_Action forceReprogramming = new Command_Action
                {
                    defaultLabel = "DEV: Force Reprogramming",
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_ReprogramDrone(Pawn));
                        // If the unit had the no programming hediff, remove that hediff.
                        Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(MDR_HediffDefOf.MDR_NoProgramming);
                        if (hediff != null)
                        {
                            Pawn.health.RemoveHediff(hediff);
                        }
                        // Reprogrammable drones do not need to restart after programming is complete.
                        hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(MHC_HediffDefOf.MHC_Restarting);
                        if (hediff != null)
                        {
                            Pawn.health.RemoveHediff(hediff);
                        }
                    }
                };
                yield return forceReprogramming;
            }
            yield break;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref enabledWorkTypes, "MDR_enabledWorkTypes", LookMode.Def);
            Scribe_Collections.Look(ref complexitySources, "MDR_complexitySources", LookMode.Value, LookMode.Value, ref sourceKey, ref sourceValue);
            Scribe_Collections.Look(ref directives, "MDR_activeDirectives", LookMode.Deep);
            Scribe_References.Look(ref complexityHediff, "MDR_complexityHediff");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (enabledWorkTypes == null)
                {
                    InitializeEnabledWorkTypes();
                }
                if (complexitySources == null)
                {
                    complexitySources = new Dictionary<string, int>();
                }
                if (directives == null)
                {
                    directives = new List<Directive>();
                    SetDirectives(Pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentDirectives);
                }
                RecalculateComplexity();
                // Cache the baseline complexity stat from the pawn's statBases. Do this exactly once as it does not change without reloading the game.
                StatDef baselineComplexityStat = MDR_StatDefOf.MDR_ComplexityLimit;
                cachedBaselineComplexity = (int)baselineComplexityStat.defaultBaseValue;
                foreach (StatModifier statMod in Pawn.def.statBases)
                {
                    if (statMod.stat == baselineComplexityStat)
                    {
                        cachedBaselineComplexity = (int)statMod.value;
                        break;
                    }
                }
                // Directives should have their pawn reference set and any tickers restored.
                directiveDefs = new List<DirectiveDef>();
                foreach (Directive directive in directives)
                {
                    directiveDefs.Add(directive.def);
                    directive.pawn = Pawn;
                    if (directive.def.tickerType != TickerType.Never)
                    {
                        Find.World.GetComponent<WorldDirectiveTicker>().RegisterDirective(directive, directive.def.tickerType);
                    }
                }
            }
        }

        // Take a list of directive defs, and create new Directives for this pawn with them. Skip defs that already have an instance of that Directive active.
        // Remove all directives that have a def not in the given list.
        public void SetDirectives(List<DirectiveDef> newDirectives)
        {
            // Remove all directives that should no longer exist, calling PostRemove on them as it goes.
            List<Directive> oldDirectives = directives;
            for (int i = oldDirectives.Count - 1; i >= 0; i--)
            {
                Directive oldDirective = oldDirectives[i];
                if (!newDirectives.Contains(oldDirective.def))
                {
                    directives.RemoveAt(i);
                    directiveDefs.Remove(oldDirective.def);
                    oldDirective.PostRemove();
                }
            }
            
            // Add all directives that should now exist, skipping directives that are already stored.
            foreach (DirectiveDef newDirectiveDef in newDirectives)
            {
                if (directiveDefs.Contains(newDirectiveDef))
                {
                    continue;
                }

                Directive newDirective = DirectiveMaker.MakeDirective(newDirectiveDef, Pawn);
                directives.Add(newDirective);
                directiveDefs.Add(newDirectiveDef);
                newDirective.PostAdd();
            }
        }

        // Initialize inherent work tasks for the drone as per the race and work types that should always be permitted.
        public void InitializeEnabledWorkTypes()
        {
            enabledWorkTypes = new List<WorkTypeDef> {};
            // Initialize enabledWorkTypes with those that are inherent to this pawn's race. They have no cost and do not reduce the number of free work types.
            foreach (WorkTypeDef workTypeDef in Pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentWorkTypes)
            {
                enabledWorkTypes.Add(workTypeDef);
            }
            // Programmable drones may also inherently do any task which has no associated work tags and no relevant skills, like bed resting.
            foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workTypeDef.relevantSkills.NullOrEmpty() && workTypeDef.workTags == WorkTags.None)
                {
                    enabledWorkTypes.Add(workTypeDef);
                }
            }
        }

        // Take a source as the key value and an int as the value to update, and recalculate complexity.
        // If source does not exist as a key and value is not 0, then add it as a source.
        // If the source exists, update its existing value. If value is 0, remove the source and value.
        public void UpdateComplexity(string source, int value)
        {
            if (complexitySources.ContainsKey(source))
            {
                if (value == 0)
                {
                    complexitySources.Remove(source);
                }
                else
                {
                    complexitySources[source] = value;
                }
                RecalculateComplexity();
                complexityHediff?.UpdateHediffStage();
            }
            else if (value != 0)
            {
                complexitySources.Add(source, value);
                RecalculateComplexity();
                complexityHediff?.UpdateHediffStage();
            }
        }

        // Given a string source, return the int value associated with that key string in complexitySources.
        // If the source does not exist, return 0.
        public int GetComplexityFromSource(string source)
        {
            return complexitySources.GetWithFallback(source, 0);
        }

        // Recalculates and recaches the complexity value for this pawn based on their complexity sources.
        public void RecalculateComplexity()
        {
            int sum = 0;
            foreach (int complexity in complexitySources.Values)
            {
                sum += complexity;
            }
            cachedComplexity = Math.Max(0, sum);
            cachedMaxComplexity = (int)Pawn.GetStatValue(MDR_StatDefOf.MDR_ComplexityLimit);
        }
    }
}