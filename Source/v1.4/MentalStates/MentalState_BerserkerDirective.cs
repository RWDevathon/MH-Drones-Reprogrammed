using RimWorld;
using Verse.AI;
using Verse;
using System.Linq;

namespace MechHumanlikes
{
    // Attack anything that isn't also under the influence of berserker directives.
    public class MentalState_BerserkerDirective : MentalState
    {
        public override bool ForceHostileTo(Thing t)
        {
            if (t is Pawn pawn && MDR_Utils.IsProgrammableDrone(pawn) && pawn.GetComp<CompReprogrammableDrone>().ActiveDirectives.Contains(MDR_DirectiveDefOf.MDR_DirectiveBerserker))
            {
                return false;
            }
            return true;
        }

        public override bool ForceHostileTo(Faction f)
        {
            return true;
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}
