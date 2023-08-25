using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MechHumanlikes
{
    public class Recipe_ReprogramDrone : Recipe_SurgeryMechanical
    {
        // Only available for programmable drones.
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (thing is Pawn pawn && MDR_Utils.IsProgrammableDrone(pawn))
            {
                return true;
            }
            return false;
        }

        // Always "apply" to the brain as it is easy to target, and guaranteed to exist if the pawn is alive.
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            yield return pawn.health.hediffSet.GetBrain();
        }

        // Open the dialog for reprogramming the unit.
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            pawn.health.AddHediff(recipe.addsHediff);
            Find.WindowStack.Add(new Dialog_ReprogramDrone(pawn));
            // If the unit had the no programming hediff, remove that hediff.
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MDR_HediffDefOf.MDR_NoProgramming);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
            // Reprogrammable drones do not need to restart after programming is complete.
            hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MHC_HediffDefOf.MHC_Restarting);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
            else
            {
                hediff.Severity = hediff.def.initialSeverity;
            }
        }
    }
}