using RimWorld;
using Verse.AI;
using Verse;

namespace MechHumanlikes
{
    // The berserker mental state drives pawns afflicted with it to fight enemies no matter how far away they are, and with no regards to safety or logic, even through doors.
    public class JobGiver_AIBerserkerRage : JobGiver_AIFightEnemies
    {
        protected override Thing FindAttackTarget(Pawn pawn)
        {
            return (Thing)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f, default, float.MaxValue, true);
        }
    }
}
