using Verse;
using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_HediffDefOf
    {
        static MDR_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_HediffDefOf));
        }

        // Surgery effect

        public static HediffDef MDR_NoProgramming;

        public static HediffDef MDR_ComplexityRelation;
    }
}
