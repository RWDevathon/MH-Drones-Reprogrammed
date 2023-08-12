using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MechHumanlikes
{
    public class DirectiveDef : Def
    {
        public Type directiveClass = typeof(Directive);

        [NoTranslate]
        public string iconPath;

        [Unsaved(false)]
        private Texture2D cachedIcon;

        public List<AbilityDef> abilities;

        public List<StatModifier> statOffsets;

        public List<StatModifier> statFactors;

        public float hungerRateFactor = 1f; // For energy, which isn't a stat for some reason.

        public HediffDef associatedHediff;

        public string directiveCategory;

        public List<string> exclusionTags;

        public int complexityCost = 1;

        public TickerType tickerType;

        public List<DirectiveRequirementWorker> requirementWorkers = new List<DirectiveRequirementWorker>();

        // Optional weights to configure whether pawns should or should not be encouraged to "buy" this directive as part of a particular group kind.
        public Dictionary<PawnGroupKindDef, float> groupKindWeights = new Dictionary<PawnGroupKindDef, float>();

        // Optional weights to configure whether pawns should or should not be encouraged to "buy" this directive depending on their skills.
        // Setting this above 0 will mean pawns will not take it if the corresponding skill is absent, and vice versa.
        public Dictionary<SkillDef, float> skillChoiceWeights = new Dictionary<SkillDef, float>();

        public Texture2D Icon
        {
            get
            {
                if (cachedIcon == null)
                {
                    if (iconPath.NullOrEmpty())
                    {
                        cachedIcon = BaseContent.BadTex;
                    }
                    else
                    {
                        cachedIcon = ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                    }
                }
                return cachedIcon;
            }
        }

        // Directives can never be stacked with the same def, and must pass all requirement workers for compatibility.
        public AcceptanceReport CompatibleWith(DirectiveDef other)
        {
            if (this == other) return "MDR_SameDirective".Translate();
            if (requirementWorkers.NullOrEmpty())
            {
                return true;
            }
            foreach (DirectiveRequirementWorker worker in requirementWorkers)
            {
                AcceptanceReport requirementReport = worker.CompatibleWith(other);
                if (!requirementReport)
                {
                    return requirementReport.Reason;
                }
            }
            return true;
        }

        // Some directives are unique, and should be totally hidden if it is never valiid for a particular pawn.
        public AcceptanceReport EverValidFor(Pawn pawn)
        {
            foreach (DirectiveRequirementWorker worker in requirementWorkers)
            {
                AcceptanceReport requirementReport = worker.EverValidFor(pawn);
                if (!requirementReport)
                {
                    return requirementReport.Reason;
                }
            }
            return true;
        }

        // Directive workers control whether the particular directive is valid for a pawn (used when reprogrammed).
        public AcceptanceReport ValidFor(Pawn pawn)
        {
            foreach (DirectiveRequirementWorker worker in requirementWorkers)
            {
                AcceptanceReport requirementReport = worker.ValidFor(pawn);
                if (!requirementReport)
                {
                    return requirementReport.Reason;
                }
            }
            return true;
        }

        // Ensure all directive requiremnet workers attached to this def have a reference stored to this def.
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (!requirementWorkers.NullOrEmpty())
            {
                foreach (DirectiveRequirementWorker requirementWorker in requirementWorkers)
                {
                    requirementWorker.def = this;
                }
            }
            if (directiveCategory == null)
            {
                directiveCategory = "Uncategorized";
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (directiveClass == null)
            {
                yield return "directiveClass is null";
            }
            if (!typeof(Directive).IsAssignableFrom(directiveClass))
            {
                yield return "directiveClass is not Directive or subclass thereof";
            }
            if (directiveCategory == null)
            {
                yield return "directiveCategory is null";
            }
        }
    }
}
