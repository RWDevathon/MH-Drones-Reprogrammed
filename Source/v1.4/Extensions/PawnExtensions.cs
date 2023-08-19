using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    // Mod extension for programmable drones to control appropriate features.
    public class MDR_ProgrammableDroneExtension : DefModExtension
    {
        // List of DirectiveDefs that all members of this race will have at all times.
        // Inherent directives cost no complexity and do not contribute to the directive limit.
        public List<DirectiveDef> inherentDirectives = new List<DirectiveDef>();

        // List of WorkTypes that all members of this race will have at all times.
        // Inherent work types cost no complexity and enable corresponding skill groups for free.
        public List<WorkTypeDef> inherentWorkTypes = new List<WorkTypeDef>();

        // List of WorkTypes that all members of this race may never have.
        // Forbidden work types will not appear in the reprogramming interface.
        // If a corresponding skill group can have none of its work types enabled, it is effectively forbidden.
        public List<WorkTypeDef> forbiddenWorkTypes = new List<WorkTypeDef>();

        // Dictionary matching SkillDefs to the level that all members of this race will have as a minimum.
        // Inherent skills cost no complexity, contribute to a higher skill ceiling, and can not be removed.
        public Dictionary<SkillDef, int> inherentSkills = new Dictionary<SkillDef, int>();

        // Dictionary matching SkillDefs to the level that all members of this race will have as a minimum.
        // Inherent skills cost no complexity, contribute to a higher skill ceiling, and can not be removed.
        public int maxDirectives = 3;
        
        public override IEnumerable<string> ConfigErrors()
        {
            if (!inherentWorkTypes.NullOrEmpty() && !forbiddenWorkTypes.NullOrEmpty()) 
            {
                if (inherentWorkTypes.Any(workType => forbiddenWorkTypes.Contains(workType)))
                {
                    yield return "A programmable drone extension had a workType that was both inherent and forbidden.";
                }
            }
        }
    }
}