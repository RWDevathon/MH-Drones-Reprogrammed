using Verse;
using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_WorkTypeDefOf
    {
        static MDR_WorkTypeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_WorkTypeDefOf));
        }

        public static WorkTypeDef MHC_Mechanic;
    }
}