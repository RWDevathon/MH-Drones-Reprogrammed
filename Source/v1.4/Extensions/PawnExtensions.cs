using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    // Mod extension for programmable drones to control appropriate features.
    public class MDR_ProgrammableDroneExtension : DefModExtension
    {
        public List<DirectiveDef> inherentDirectives = new List<DirectiveDef>();

        public List<WorkTypeDef> inherentWorkTypes = new List<WorkTypeDef>();

        public Dictionary<SkillDef, int> inherentSkills = new Dictionary<SkillDef, int>();

        public int maxDirectives = 3;
        
        public List<WorkTypeDef> forbiddenWorkTypes = new List<WorkTypeDef>();

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