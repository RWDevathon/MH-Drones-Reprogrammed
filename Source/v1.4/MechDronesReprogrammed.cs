﻿using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace MechHumanlikes
{
    public class MechDronesReprogrammed : Mod
    {
        public static MechDronesReprogrammed ModSingleton { get; private set; }

        public MechDronesReprogrammed(ModContentPack content) : base(content)
        {
            ModSingleton = this;
            new Harmony("MechDronesReprogrammed").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [StaticConstructorOnStartup]
    public static class MechDronesReprogrammed_PostInit
    {
        static MechDronesReprogrammed_PostInit()
        {
            // Must dynamically modify some ThingDefs based on certain qualifications.
            foreach (DirectiveDef def in DefDatabase<DirectiveDef>.AllDefsListForReading)
            {
                if (!MDR_Utils.directiveCategories.Contains(def.directiveCategory))
                {
                    MDR_Utils.directiveCategories.Add(def.directiveCategory);
                }

                MDR_Utils.cachedSortedDirectives.Add(def);
            }
            MDR_Utils.cachedSortedDirectives.SortBy(def => def.directiveCategory, def => def.exclusionTags?.FirstOrFallback() ?? def.label, def => def.complexityCost);

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                // Programmable drones have a race and a programmable drone pawn extension, and should have the programming ITab.
                if (thingDef.race != null && MHC_Utils.IsConsideredMechanicalDrone(thingDef) && thingDef.HasModExtension<MDR_ProgrammableDroneExtension>())
                {
                    MDR_Utils.cachedProgrammableDrones.Add(thingDef);

                    CompProperties compProps = new CompProperties
                    {
                        compClass = typeof(CompReprogrammableDrone)
                    };
                    thingDef.comps.Add(compProps);

                    thingDef.inspectorTabs.Insert(thingDef.inspectorTabs.Count / 2, typeof(ITab_Programming));
                    thingDef.inspectorTabsResolved.Insert(thingDef.inspectorTabsResolved.Count / 2, InspectTabManager.GetSharedInstance(typeof(ITab_Programming)));
                }
            }

            // WorkTypeExtensions must have their def reference manually defined here as they are normally def-blind and do not self-initialize.
            foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workTypeDef.GetModExtension<MDR_WorkTypeExtension>() is MDR_WorkTypeExtension workTypeExtension)
                {
                    workTypeExtension.def = workTypeDef;
                }
            }

            // Some SkillDefs are disabled by WorkTags rather than by WorkTypes alone. We need to keep track of them here.
            foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                if (skillDef.disablingWorkTags != WorkTags.None)
                {
                    MDR_Utils.cachedSkillsDisabledByWorkTags.Add(skillDef);
                }
            }
        }
    }
}