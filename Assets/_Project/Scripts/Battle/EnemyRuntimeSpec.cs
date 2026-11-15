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
            MapNodeType encounterType,
            int rank,
            UnityEngine.Texture2D mainTexture = null,
            float damageMultiplier = 1f,
            EnemyMaterialTexture[] materialTextures = null,
            UnityEngine.Material additionalBodyMaterial = null)
        {
            Definition = definition;
            HardStats = hardStats;
            HealthMultiplier = healthMultiplier;
            ActiveSkills = activeSkills ?? EmptySkills;
            ExperienceReward = System.Math.Max(0, experienceReward);
            DisplayName = displayName ?? string.Empty;
            EffectiveFloor = System.Math.Max(1, effectiveFloor);
            EncounterType = encounterType;
            Rank = System.Math.Max(1, rank);
            MainTexture = mainTexture;
            AdditionalBodyMaterial = additionalBodyMaterial;
            MaterialTextures = materialTextures != null
                ? (EnemyMaterialTexture[])materialTextures.Clone()
                : System.Array.Empty<EnemyMaterialTexture>();
            DamageMultiplier = damageMultiplier > 0f && !float.IsInfinity(damageMultiplier) ? damageMultiplier : 1f;
        }

        public EnemyDefinition Definition { get; }

        public HardStats HardStats { get; }

        /// <summary>Applied to CON-derived max HP after stats are built. Elite and boss use 1.25.</summary>
        public float HealthMultiplier { get; }

        /// <summary>Per-hit damage tuning for this fight. Does not scale healing or shields.</summary>
        public float DamageMultiplier { get; }

        public IReadOnlyList<SkillDefinition> ActiveSkills { get; }

        public int ExperienceReward { get; }

        public string DisplayName { get; }

        public int EffectiveFloor { get; }

        public MapNodeType EncounterType { get; }

        /// <summary>1-based count of rank thresholds reached on <see cref="EffectiveFloor"/>. At least 1.</summary>
        public int Rank { get; }
        public UnityEngine.Texture2D MainTexture { get; }
        public IReadOnlyList<EnemyMaterialTexture> MaterialTextures { get; }
        public UnityEngine.Material AdditionalBodyMaterial { get; }
    }
}
