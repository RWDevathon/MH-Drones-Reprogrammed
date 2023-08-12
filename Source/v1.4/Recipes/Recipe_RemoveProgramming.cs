using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MechHumanlikes
{
    public class Recipe_RemoveProgramming : Recipe_Surgery
    {
        // This operation is only available on programmable drones that have directives uploaded.
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn
                && MDR_Utils.IsProgrammableDrone(pawn)
                && pawn.health.hediffSet.GetFirstHediffOfDef(MDR_HediffDefOf.MDR_NoProgramming) == null;
        }

        // Always "apply" to the brain as it is easy to target, and guaranteed to exist if the pawn is alive.
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            yield return pawn.health.hediffSet.GetBrain();
        }

        // This operation resets the drone back to being "blank" and having no directives.
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (bill.recipe.addsHediff != null)
            {
                pawn.health.AddHediff(bill.recipe.addsHediff);
            }
            MDR_Utils.Deprogram(pawn);
        }
    }
}

