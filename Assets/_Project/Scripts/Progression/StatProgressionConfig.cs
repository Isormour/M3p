using System;
using System.Collections.Generic;
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
    /// Applies every configured perk whose threshold has been reached in a perk tree.
    /// Hard stats for the player are derived from owned tree points.
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
        [Tooltip("Every N points in a perk tree that grants a hard stat offers a talent choice.")]
        [Min(1), SerializeField] int _talentMilestoneInterval = 5;

        [Header("Limits")]
        [Tooltip("Highest value a single perk tree can reach. Points cannot be spent past it.")]
        [Min(1), SerializeField] int _maxStatValue = 30;

        [Header("Perk trees")]
        [Tooltip("Catalog containing every perk referenced by the trees.")]
        [SerializeField] PerkConfig _perkConfig;
        [Tooltip("Every spendable perk tree. New profiles receive these with their starting values.")]
        [SerializeField] PerkTree[] _perkTrees = Array.Empty<PerkTree>();

        public PerkTree[] PerkTrees => _perkTrees ?? Array.Empty<PerkTree>();
        public PerkConfig Perks => _perkConfig;
        public int MilestoneInterval => Mathf.Max(1, _talentMilestoneInterval);
        public int MaxStatValue => Mathf.Max(1, _maxStatValue);

        public PerkTree GetTree(int treeId)
        {
            if (treeId == PerkTree.InvalidId)
                return null;

            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree != null && tree.Id == treeId)
                    return tree;
            }

            return null;
        }

        public PerkTree GetTreeForStat(EStatType stat)
        {
            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree != null && tree.TryGetGrantedStat(out EStatType granted) && granted == stat)
                    return tree;
            }

            return null;
        }

        /// <summary>The perk ladder authored for one tree, or empty when the id is unknown.</summary>
        public StatPerk[] GetProgression(int treeId)
        {
            PerkTree tree = GetTree(treeId);
            return tree != null ? tree.Perks : Array.Empty<StatPerk>();
        }

        public StatPerk[] GetProgression(EStatType stat)
        {
            PerkTree tree = GetTreeForStat(stat);
            return tree != null ? tree.Perks : Array.Empty<StatPerk>();
        }

        public Sprite GetStatIcon(int treeId)
        {
            PerkTree tree = GetTree(treeId);
            return tree != null ? tree.Icon : null;
        }

        public Sprite GetStatIcon(EStatType stat)
        {
            PerkTree tree = GetTreeForStat(stat);
            return tree != null ? tree.Icon : null;
        }

        public static StatProgressionConfig CreateDefault()
        {
            StatProgressionConfig config = CreateInstance<StatProgressionConfig>();
            config.name = "StatProgressionConfig (Default)";
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }

        public int GetTreeValue(IReadOnlyList<PerkTreePoints> treePoints, int treeId)
        {
            int index = IndexOfTreePoints(treePoints, treeId);
            return index >= 0 ? Mathf.Max(0, treePoints[index].Value) : 0;
        }

        public HardStats CalculateHardStats(IReadOnlyList<PerkTreePoints> treePoints)
        {
            HardStats hard = default;
            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree == null)
                    continue;

                tree.ApplyStatBonus(GetTreeValue(treePoints, tree.Id), ref hard);
            }

            return hard;
        }

        public SoftStatValues CalculateSoftStats(HardStats hard, TalentBonuses talents = default)
        {
            return CalculateSoftStats(hard, null, talents);
        }

        public SoftStatValues CalculateSoftStats(
            IReadOnlyList<PerkTreePoints> treePoints,
            TalentBonuses talents = default)
        {
            return CalculateSoftStats(CalculateHardStats(treePoints), treePoints, talents);
        }

        SoftStatValues CalculateSoftStats(
            HardStats hard,
            IReadOnlyList<PerkTreePoints> treePoints,
            TalentBonuses talents)
        {
            SoftStatValues stats = new SoftStatValues
            {
                BasicAttackDamage = _baseBasicAttackDamage,
                MaxHP = _baseMaxHp,
                MaxActionPoints = _baseMaxActionPoints,
                MaxHandSize = _baseMaxHandSize,
            };

            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree == null)
                    continue;

                int level = treePoints != null
                    ? GetTreeValue(treePoints, tree.Id)
                    : tree.GetProgressionLevel(hard);
                ApplyUnlocked(tree.Perks, level, ref stats);
            }

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

        /// <summary>
        /// Inserts every authored tree the profile does not already own. Starting values come from
        /// matching hard stats when the tree grants one, otherwise 1.
        /// </summary>
        public void EnsureDefaultTrees(PlayerProfile profile, HardStats startingHard)
        {
            if (profile == null)
                return;

            profile.PerkTreePoints ??= new List<PerkTreePoints>();

            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree == null || tree.Id == PerkTree.InvalidId)
                    continue;

                if (IndexOfTreePoints(profile.PerkTreePoints, tree.Id) >= 0)
                    continue;

                int startValue = tree.TryGetGrantedStat(out EStatType stat)
                    ? Mathf.Max(1, startingHard.Get(stat))
                    : 1;
                profile.PerkTreePoints.Add(new PerkTreePoints(tree.Id, startValue));
            }

            profile.HardStats = CalculateHardStats(profile.PerkTreePoints);
        }

        public static int IndexOfTreePoints(IReadOnlyList<PerkTreePoints> treePoints, int treeId)
        {
            if (treePoints == null || treeId == PerkTree.InvalidId)
                return -1;

            for (int i = 0; i < treePoints.Count; i++)
            {
                if (treePoints[i].TreeId == treeId)
                    return i;
            }

            return -1;
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
            PerkTree[] trees = PerkTrees;
            for (int i = 0; i < trees.Length; i++)
            {
                PerkTree tree = trees[i];
                if (tree == null)
                    continue;

                if (tree.Id == PerkTree.InvalidId)
                {
                    Debug.LogWarning($"{nameof(StatProgressionConfig)} '{name}': tree '{tree.name}' has id 0, so profiles cannot store it.", this);
                    continue;
                }

                for (int j = i + 1; j < trees.Length; j++)
                {
                    PerkTree other = trees[j];
                    if (other != null && other.Id == tree.Id)
                        Debug.LogWarning(
                            $"{nameof(StatProgressionConfig)} '{name}': trees '{tree.name}' and '{other.name}' share id {tree.Id}.",
                            this);
                }

                ValidateProgression(tree.Perks, tree.name);
            }
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
                        $"{nameof(StatProgressionConfig)} '{name}': perk '{perk.name}' in '{progressionName}' is missing from '{_perkConfig.name}'.",
                        this);
            }
        }
    }
}
