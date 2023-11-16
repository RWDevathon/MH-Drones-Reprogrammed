using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    // Mod extension for programmable drone pawn kinds to control appropriate features.
    public class MDR_ProgrammableDroneKindExtension : DefModExtension
    {
        // Work Types that the drone must generate with, even if this exceeds complexity limits and the discretionary complexity.
        public List<WorkTypeDef> requiredWorkTypes = new List<WorkTypeDef>();

        // Directives that the drone must generate with, even if this exceeds complexity limits and the discretionary complexity.
        public List<DirectiveDef> requiredDirectives = new List<DirectiveDef>();

        // How many non-mandatory, non-inherent directives the drone may generate with if it does not exceed discretionary complexity.
        public IntRange discretionaryDirectives = new IntRange(0, 0);

        // How many non-mandatory, non-inherent work types the drone may generate with if it does not exceed discretionary complexity.
        public IntRange discretionaryWorkTypes = new IntRange(1, 3);

        // How much complexity the drone can use for random generation on top of their required complexity - does not take limits into account.
        public IntRange discretionaryComplexity = new IntRange(0, 10);
    }
}