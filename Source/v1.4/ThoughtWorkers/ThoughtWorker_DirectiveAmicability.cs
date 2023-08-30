using RimWorld;
using Verse;

namespace MechHumanlikes
{
    public class ThoughtWorker_DirectiveAmicability : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Faction != Faction.OfPlayerSilentFail || p.Map == null)
            {
                return false;
            }

            int num = MDR_Utils.amicableDroneCount.GetWithFallback(p.Map, 0);
            if (num >= 5)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            else if (num >= 2)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (num > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return false;
        }
    }
}
