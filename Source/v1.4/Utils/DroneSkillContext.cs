using RimWorld;
using Verse;

namespace MechHumanlikes
{
    // Simple class containing calculated values for a given skillRecord for the cost at the current level and the min and max of the skill.
    // Best if used in a Dict<SkillRecord, DroneSkillContext> object.
    public class DroneSkillContext
    {
        public float skillComplexityCost = 0;

        public int skillFloor = 0;

        public int skillCeiling = 0;

        public DroneSkillContext(SkillRecord skillRecord)
        {
            skillComplexityCost = MDR_Utils.SkillComplexityCostFor(skillRecord.Pawn, skillRecord.def);
            skillFloor = skillRecord.Pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>().inherentSkills.GetWithFallback(skillRecord.def, SkillRecord.MinLevel);
            skillCeiling = (int)skillRecord.Pawn.GetStatValue(MDR_StatDefOf.MDR_SkillLimit) + skillFloor;
        }

    }
}
