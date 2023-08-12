using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_NeedDefOf
    {
        static MDR_NeedDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_NeedDefOf));
        }
        public static NeedDef MHC_Coolant;

        public static NeedDef MHC_Lubrication;
    }
}