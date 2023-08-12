using Verse;
using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_JobDefOf
    {
        static MDR_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_JobDefOf));
        }
        public static JobDef MHC_GetRecharge;

        public static JobDef MHC_IngestMechNeed;

        public static JobDef MHC_TendMechanical;

        public static JobDef MHC_DoMaintenanceUrgent;

        public static JobDef MHC_DoMaintenanceIdle;
    }
}