using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_StatDefOf
    {
        static MDR_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_StatDefOf));
        }

        public static StatDef MDR_ComplexityLimit;
    }
}