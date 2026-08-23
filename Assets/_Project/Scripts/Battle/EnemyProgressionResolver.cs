using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Builds a fight-ready <see cref="EnemyRuntimeSpec"/> from one archetype, the dungeon floor,
    /// and the map-node type. Pure: never mutates the ScriptableObject.
    /// </summary>
    public static class EnemyProgressionResolver
    {
        public const float ExperiencePerEffectiveFloor = 0.20f;
        public const int EliteFloorBonus = 2;
        public const float EliteHealthMultiplier = 1.25f;
        public const float EliteExperienceMultiplier = 1.75f;
        public const int BossFloorBonus = 4;
        public const float BossExperienceMultiplier = 3f;
        public const int MaxActiveSkills = 4;

        public static EnemyRuntimeSpec Resolve(
            EnemyDefinition definition,
            int floorIndex,
            MapNodeType encounterType)
        {
            int floor = Mathf.Max(1, floorIndex);
            int effectiveFloor = GetEffectiveFloor(floor, encounterType);
            float healthMultiplier = GetHealthMultiplier(encounterType);
            float experienceMultiplier = GetExperienceMultiplier(encounterType);

            if (definition == null)
            {
                return new EnemyRuntimeSpec(
                    null,
                    default,
                    healthMultiplier,
                    System.Array.Empty<SkillDefinition>(),
                    0,
                    string.Empty,
                    effectiveFloor,
                    encounterType);
            }

            HardStats hardStats = GrowStats(definition.HardStats, definition.StatGrowthPerFloor, effectiveFloor);
            SkillDefinition[] skills = BuildActiveSkills(definition, effectiveFloor);
            int experience = ScaleExperience(definition.ExperienceReward, effectiveFloor, experienceMultiplier);
            string displayName = BuildDisplayName(definition.Name, definition.RankThresholds, effectiveFloor);

            return new EnemyRuntimeSpec(
                definition,
                hardStats,
                healthMultiplier,
                skills,
                experience,
                displayName,
                effectiveFloor,
                encounterType);
        }

        public static int GetEffectiveFloor(int floorIndex, MapNodeType encounterType)
        {
            int floor = Mathf.Max(1, floorIndex);
            switch (encounterType)
            {
                case MapNodeType.Elite:
                    return floor + EliteFloorBonus;
                case MapNodeType.Boss:
                    return floor + BossFloorBonus;
                default:
                    return floor;
            }
        }

        public static float GetHealthMultiplier(MapNodeType encounterType)
        {
            return encounterType == MapNodeType.Elite ? EliteHealthMultiplier : 1f;
        }

        public static float GetExperienceMultiplier(MapNodeType encounterType)
        {
            switch (encounterType)
            {
                case MapNodeType.Elite:
                    return EliteExperienceMultiplier;
                case MapNodeType.Boss:
                    return BossExperienceMultiplier;
                default:
                    return 1f;
            }
        }

        public static HardStats GrowStats(HardStats baseline, StatGrowthPerFloor growth, int effectiveFloor)
        {
            int steps = Mathf.Max(0, effectiveFloor - 1);
            return new HardStats(
                GrowStat(baseline.Strength, growth.Strength, steps),
                GrowStat(baseline.Intelligence, growth.Intelligence, steps),
                GrowStat(baseline.Constitution, growth.Constitution, steps),
                GrowStat(baseline.Agility, growth.Agility, steps));
        }

        public static int ScaleExperience(int baseExperience, int effectiveFloor, float encounterMultiplier)
        {
            if (baseExperience <= 0)
                return 0;

            int floor = Mathf.Max(1, effectiveFloor);
            float floorScale = 1f + ExperiencePerEffectiveFloor * (floor - 1);
            return Mathf.Max(0, Mathf.RoundToInt(baseExperience * floorScale * Mathf.Max(0f, encounterMultiplier)));
        }

        static int GrowStat(int baseline, float growthPerFloor, int steps)
        {
            return baseline + Mathf.FloorToInt(steps * growthPerFloor);
        }

        static SkillDefinition[] BuildActiveSkills(EnemyDefinition definition, int effectiveFloor)
        {
            var unlocks = new List<EnemySkillUnlock>(8);
            definition.CollectSkillUnlocks(unlocks);
            if (unlocks.Count == 0)
                return System.Array.Empty<SkillDefinition>();

            unlocks.Sort(CompareUnlocks);

            var skills = new List<SkillDefinition>(MaxActiveSkills);
            for (int i = 0; i < unlocks.Count; i++)
            {
                EnemySkillUnlock unlock = unlocks[i];
                if (!unlock.IsValid || unlock.MinFloor > effectiveFloor)
                    continue;

                if (unlock.Replaces != null)
                    skills.Remove(unlock.Replaces);

                if (!skills.Contains(unlock.Skill))
                    skills.Add(unlock.Skill);
            }

            if (skills.Count > MaxActiveSkills)
                skills.RemoveRange(MaxActiveSkills, skills.Count - MaxActiveSkills);

            return skills.ToArray();
        }

        static int CompareUnlocks(EnemySkillUnlock left, EnemySkillUnlock right)
        {
            int floorCompare = left.MinFloor.CompareTo(right.MinFloor);
            if (floorCompare != 0)
                return floorCompare;

            string leftName = left.Skill != null ? left.Skill.name : string.Empty;
            string rightName = right.Skill != null ? right.Skill.name : string.Empty;
            return string.CompareOrdinal(leftName, rightName);
        }

        static string BuildDisplayName(string speciesName, IReadOnlyList<EnemyRankThreshold> ranks, int effectiveFloor)
        {
            string rank = null;
            int bestFloor = int.MinValue;

            if (ranks != null)
            {
                for (int i = 0; i < ranks.Count; i++)
                {
                    EnemyRankThreshold threshold = ranks[i];
                    if (string.IsNullOrEmpty(threshold.RankName) || threshold.MinFloor > effectiveFloor)
                        continue;

                    if (threshold.MinFloor < bestFloor)
                        continue;

                    bestFloor = threshold.MinFloor;
                    rank = threshold.RankName;
                }
            }

            if (string.IsNullOrEmpty(speciesName))
                return rank ?? string.Empty;

            if (string.IsNullOrEmpty(rank))
                return speciesName;

            return $"{speciesName} {rank}";
        }
    }
}
