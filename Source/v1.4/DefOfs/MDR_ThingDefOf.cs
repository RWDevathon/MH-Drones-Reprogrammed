using Verse;
using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_ThingDefOf
    {
        static MDR_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_ThingDefOf));
        }

        public static ThingDef MHC_BedsideChargerFacility;

        public static ThingDef MHC_MaintenanceSpot;

        public static ThingDef MHC_CoolantPack;
    }
}