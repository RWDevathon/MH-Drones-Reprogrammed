using RimWorld;
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
                return "MDR_ComplexityRequirementInsufficient".Translate(def.labelShort.CapitalizeFirst(), minimumRequiredComplexity, pawn.LabelShortCap, programComp.MaxComplexity);
            }
            return true;
        }
    }
}