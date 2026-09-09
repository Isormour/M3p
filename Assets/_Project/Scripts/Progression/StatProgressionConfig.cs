using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public struct StatPerk
    {
        [Min(1)] public int StatLevel;
        public PerkDefinition Perk;
    }

    /// <summary>
    /// Applies every configured perk whose threshold has been reached in a hard stat.
    /// </summary>
    [CreateAssetMenu(fileName = "StatProgressionConfig", menuName = "M3P/Stat Progression Config", order = 2)]
    public class StatProgressionConfig : ScriptableObject
    {
        [Header("Base values")]
        [Min(1), SerializeField] int _baseBasicAttackDamage = 1;
        [Min(1), SerializeField] int _baseMaxHp = 15;
        [Min(1), SerializeField] int _baseMaxActionPoints = 2;
        [Min(0), SerializeField] int _baseMaxHandSize = 3;

        [Header("Talents")]
        [Tooltip("Every N points in a hard stat offers a talent choice.")]
        [Min(1), SerializeField] int _talentMilestoneInterval = 5;

        [Header("Limits")]
        [Tooltip("Highest value a single hard stat can reach. Points cannot be spent past it.")]
        [Min(1), SerializeField] int _maxStatValue = 30;

        [Header("Stat progression")]
        [Tooltip("Catalog containing every perk referenced by the progression lists.")]
        [SerializeField] PerkConfig _perkConfig;
        [Tooltip("Perks applied after reaching their Strength threshold.")]
        [SerializeField] StatPerk[] _strengthProgression = Array.Empty<StatPerk>();
        [Tooltip("Perks applied after reaching their Intelligence threshold.")]
        [SerializeField] StatPerk[] _intelligenceProgression = Array.Empty<StatPerk>();
        [Tooltip("Perks applied after reaching their Constitution threshold.")]
        [SerializeField] StatPerk[] _constitutionProgression = Array.Empty<StatPerk>();
        [Tooltip("Perks applied after reaching their Agility threshold.")]
        [SerializeField] StatPerk[] _agilityProgression = Array.Empty<StatPerk>();

        public StatPerk[] StrengthProgression => _strengthProgression;
        public StatPerk[] IntelligenceProgression => _intelligenceProgression;
        public StatPerk[] ConstitutionProgression => _constitutionProgression;
        public StatPerk[] AgilityProgression => _agilityProgression;
        public PerkConfig Perks => _perkConfig;
        public int MilestoneInterval => Mathf.Max(1, _talentMilestoneInterval);
        public int MaxStatValue => Mathf.Max(1, _maxStatValue);

        /// <summary>The perk ladder authored for one hard stat, ordered as it was authored.</summary>
        public StatPerk[] GetProgression(EStatType stat)
        {
            switch (stat)
            {
                case EStatType.Strength:
                    return _strengthProgression ?? Array.Empty<StatPerk>();
                case EStatType.Intelligence:
                    return _intelligenceProgression ?? Array.Empty<StatPerk>();
                case EStatType.Constitution:
                    return _constitutionProgression ?? Array.Empty<StatPerk>();
                case EStatType.Agility:
                    return _agilityProgression ?? Array.Empty<StatPerk>();
                default:
                    return Array.Empty<StatPerk>();
            }
        }

        public static StatProgressionConfig CreateDefault()
        {
            StatProgressionConfig config = CreateInstance<StatProgressionConfig>();
            config.name = "StatProgressionConfig (Default)";
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }

        public SoftStatValues CalculateSoftStats(HardStats hard, TalentBonuses talents = default)
        {
            SoftStatValues stats = new SoftStatValues
            {
                BasicAttackDamage = _baseBasicAttackDamage,
                MaxHP = _baseMaxHp,
                MaxActionPoints = _baseMaxActionPoints,
                MaxHandSize = _baseMaxHandSize,
            };

            ApplyUnlocked(_strengthProgression, hard.Strength, ref stats);
            ApplyUnlocked(_intelligenceProgression, hard.Intelligence, ref stats);
            ApplyUnlocked(_constitutionProgression, hard.Constitution, ref stats);
            ApplyUnlocked(_agilityProgression, hard.Agility, ref stats);

            stats.BasicAttackDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(stats.BasicAttackDamage * (1f + talents.PhysicalDamagePercent)));
            stats.MaxHP = Mathf.Max(1, stats.MaxHP + talents.MaxHp);
            stats.MaxActionPoints = Mathf.Max(1, stats.MaxActionPoints + talents.MaxActionPoints);
            stats.MaxHandSize = Mathf.Max(0, stats.MaxHandSize + talents.MaxHandSize);
            return stats;
        }

        public int CalculateBasicAttackDamage(HardStats hard, TalentBonuses talents = default)
        {
            return CalculateSoftStats(hard, talents).BasicAttackDamage;
        }

        public int CalculateMaxHp(HardStats hard, TalentBonuses talents = default)
        {
            return CalculateSoftStats(hard, talents).MaxHP;
        }

        public int CalculateMaxActionPoints(HardStats hard, TalentBonuses talents = default)
        {
            return CalculateSoftStats(hard, talents).MaxActionPoints;
        }

        public int CalculateMaxHandSize(HardStats hard, TalentBonuses talents = default)
        {
            return CalculateSoftStats(hard, talents).MaxHandSize;
        }

        public float GetPhysicalDamageMultiplier(HardStats hard, TalentBonuses talents = default)
        {
            return 1f + talents.PhysicalDamagePercent;
        }

        public float GetMagicEffectMultiplier(HardStats hard, TalentBonuses talents = default)
        {
            return 1f + talents.MagicEffectPercent;
        }

        public int ScaleMagicEffect(HardStats hard, int baseAmount, TalentBonuses talents = default)
        {
            if (baseAmount <= 0)
                return baseAmount;

            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * GetMagicEffectMultiplier(hard, talents)));
        }

        public int ScalePhysicalEffect(HardStats hard, int baseAmount, TalentBonuses talents = default)
        {
            if (baseAmount <= 0)
                return baseAmount;

            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * GetPhysicalDamageMultiplier(hard, talents)));
        }

        static void ApplyUnlocked(StatPerk[] progression, int statLevel, ref SoftStatValues stats)
        {
            if (progression == null || statLevel <= 0)
                return;

            for (int i = 0; i < progression.Length; i++)
            {
                StatPerk perk = progression[i];
                if (perk.StatLevel > 0 && statLevel >= perk.StatLevel && perk.Perk != null)
                    perk.Perk.Apply(ref stats);
            }
        }

        void OnValidate()
        {
            ValidateProgression(_strengthProgression, nameof(_strengthProgression));
            ValidateProgression(_intelligenceProgression, nameof(_intelligenceProgression));
            ValidateProgression(_constitutionProgression, nameof(_constitutionProgression));
            ValidateProgression(_agilityProgression, nameof(_agilityProgression));
        }

        void ValidateProgression(StatPerk[] progression, string progressionName)
        {
            if (_perkConfig == null || progression == null)
                return;

            for (int i = 0; i < progression.Length; i++)
            {
                PerkDefinition perk = progression[i].Perk;
                if (perk != null && !_perkConfig.Contains(perk))
                    Debug.LogWarning(
                        $"{nameof(StatProgressionConfig)} '{name}': perk '{perk.name}' in {progressionName} is missing from '{_perkConfig.name}'.",
                        this);
            }
        }
    }
}
