using Verse;

namespace MechHumanlikes
{
    // This worker prevents directives that share an exclusion tag from being used together.
    public class DirectiveRequirementWorker_ExclusionTag : DirectiveRequirementWorker
    {
        public override AcceptanceReport CompatibleWith(DirectiveDef other)
        {
            AcceptanceReport baseReport = base.CompatibleWith(other);
            if (!baseReport)
            {
                Log.Error("WAAAAGHER");
                return baseReport.Reason;
            }

            if (def.exclusionTags.NullOrEmpty() || other.exclusionTags.NullOrEmpty())
            {
                Log.Error("WAAAAGH");
                return true;
            }

            for (int i = def.exclusionTags.Count - 1; i > 0; i--)
            {
                if (other.exclusionTags.Contains(def.exclusionTags[i]))
                {
                    return "MDR_ExclusionTagConflict".Translate(def.LabelCap, other.LabelCap, def.exclusionTags[i]);
                }
            }
            return true;
        }
    }
}