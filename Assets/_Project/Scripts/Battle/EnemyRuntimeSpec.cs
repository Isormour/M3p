using System.Collections.Generic;

namespace M3P
{
    /// <summary>
    /// Immutable fight-ready enemy. Built from an <see cref="EnemyDefinition"/> plus floor and
    /// encounter type; the ScriptableObject itself is never written.
    /// </summary>
    public sealed class EnemyRuntimeSpec
    {
        static readonly SkillDefinition[] EmptySkills = System.Array.Empty<SkillDefinition>();

        public EnemyRuntimeSpec(
            EnemyDefinition definition,
            HardStats hardStats,
            float healthMultiplier,
            SkillDefinition[] activeSkills,
            int experienceReward,
            string displayName,
            int effectiveFloor,
            MapNodeType encounterType)
        {
            Definition = definition;
            HardStats = hardStats;
            HealthMultiplier = healthMultiplier;
            ActiveSkills = activeSkills ?? EmptySkills;
            ExperienceReward = System.Math.Max(0, experienceReward);
            DisplayName = displayName ?? string.Empty;
            EffectiveFloor = System.Math.Max(1, effectiveFloor);
            EncounterType = encounterType;
        }

        public EnemyDefinition Definition { get; }

        public HardStats HardStats { get; }

        /// <summary>Applied to CON-derived max HP after stats are built. Elite uses 1.25.</summary>
        public float HealthMultiplier { get; }

        public IReadOnlyList<SkillDefinition> ActiveSkills { get; }

        public int ExperienceReward { get; }

        public string DisplayName { get; }

        public int EffectiveFloor { get; }

        public MapNodeType EncounterType { get; }
    }
}
