using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_HistoryEventDefOf
    {
        static MDR_HistoryEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_HistoryEventDefOf));
        }
        public static HistoryEventDef MHC_ExtractedCoolantPack;
    }
}