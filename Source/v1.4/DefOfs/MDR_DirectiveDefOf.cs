using Verse;
using RimWorld;

namespace MechHumanlikes
{
    [DefOf]
    public static class MDR_DirectiveDefOf
    {
        static MDR_DirectiveDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MDR_DirectiveDefOf));
        }

        public static DirectiveDef MDR_DirectiveArtisan;

        public static DirectiveDef MDR_DirectiveMartyrdom;

        public static DirectiveDef MDR_DirectiveBerserker;
    }
}
