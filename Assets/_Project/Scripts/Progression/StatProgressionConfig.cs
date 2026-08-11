using UnityEngine;

namespace M3P
{
    /// <summary>
    /// How hard stats translate into combat numbers. Per-point scaling handles the steady curve;
    /// milestone bonuses fire every few points in a stat, as in the GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "StatProgressionConfig", menuName = "M3P/Stat Progression Config", order = 2)]
    public class StatProgressionConfig : ScriptableObject
    {
        [Header("Base values")]
        [Min(1), SerializeField] int _baseMaxHp = 15;
        [Min(1), SerializeField] int _baseMaxActionPoints = 2;
        [Min(0), SerializeField] int _baseMaxHandSize = 3;

        [Header("Per point")]
        [Tooltip("Extra max HP per Constitution point.")]
        [Min(0), SerializeField] int _healthPerConstitution = 5;

        [Tooltip("Physical damage multiplier added per Strength point. 0.05 means +5% per point.")]
        [Min(0f), SerializeField] float _physicalDamagePercentPerStrength = 0.05f;

        [Tooltip("Magic effect multiplier added per Intelligence point.")]
        [Min(0f), SerializeField] float _magicEffectPercentPerIntelligence = 0.05f;

        [Header("Milestones")]
        [Tooltip("Every N points invested in a stat counts as one milestone tier.")]
        [Min(1), SerializeField] int _milestoneInterval = 5;

        [Tooltip("Extra max hand size per Intelligence milestone. GDD: +1 card every 5 INT.")]
        [Min(0), SerializeField] int _bonusHandSizePerIntelligenceMilestone = 1;

        [Tooltip("Extra max action points per Agility milestone. GDD: +1 AP every 5 AGI.")]
        [Min(0), SerializeField] int _bonusActionPointsPerAgilityMilestone = 1;

        public int MilestoneInterval => Mathf.Max(1, _milestoneInterval);

        public static StatProgressionConfig CreateDefault()
        {
            StatProgressionConfig config = CreateInstance<StatProgressionConfig>();
            config.name = "StatProgressionConfig (Default)";
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }

        public int GetMilestoneTier(int statValue)
        {
            if (statValue <= 0)
                return 0;

            return statValue / MilestoneInterval;
        }

        public int CalculateMaxHp(HardStats hard, TalentBonuses talents = default)
        {
            return Mathf.Max(1, _baseMaxHp + hard.Constitution * _healthPerConstitution + talents.MaxHp);
        }

        public int CalculateMaxActionPoints(HardStats hard, TalentBonuses talents = default)
        {
            int milestones = GetMilestoneTier(hard.Agility);
            return Mathf.Max(1, _baseMaxActionPoints + milestones * _bonusActionPointsPerAgilityMilestone + talents.MaxActionPoints);
        }

        public int CalculateMaxHandSize(HardStats hard, TalentBonuses talents = default)
        {
            int milestones = GetMilestoneTier(hard.Intelligence);
            return Mathf.Max(0, _baseMaxHandSize + milestones * _bonusHandSizePerIntelligenceMilestone + talents.MaxHandSize);
        }

        public float GetPhysicalDamageMultiplier(HardStats hard, TalentBonuses talents = default)
        {
            float percent = hard.Strength * _physicalDamagePercentPerStrength + talents.PhysicalDamagePercent;
            return 1f + percent;
        }

        public float GetMagicEffectMultiplier(HardStats hard, TalentBonuses talents = default)
        {
            float percent = hard.Intelligence * _magicEffectPercentPerIntelligence + talents.MagicEffectPercent;
            return 1f + percent;
        }

        public int ScaleMagicEffect(HardStats hard, int baseAmount, TalentBonuses talents = default)
        {
            if (baseAmount <= 0)
                return baseAmount;

            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * GetMagicEffectMultiplier(hard, talents)));
        }
    }
}
