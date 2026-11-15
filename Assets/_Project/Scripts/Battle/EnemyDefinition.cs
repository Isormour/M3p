using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "M3P/Enemy Definition", order = 2)]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] string _name;

        /// <remarks>Distinct from Unity’s inherited <see cref="Object.name"/> (asset filename).</remarks>
        public string Name => _name;

        [Tooltip("Hard stats on floor 1 of a normal fight. Later floors add Stat Growth.")]
        [SerializeField] HardStats _hardStats = new HardStats(1, 1, 10, 1);

        public HardStats HardStats => _hardStats;

        [Tooltip("Flat max HP added after constitution perks and the encounter health multiplier.")]
        [SerializeField] int _bonusMaxHp;

        public int BonusMaxHp => Mathf.Max(0, _bonusMaxHp);

        [Tooltip("Experience the player banks for winning a floor-1 normal fight. Scaled at runtime.")]
        [Min(0), SerializeField] int _experienceReward = 50;

        public int ExperienceReward => _experienceReward;

        [Tooltip("Added each effective floor after the first. Fractions accumulate, then floor.")]
        [SerializeField] StatGrowthPerFloor _statGrowthPerFloor;

        public StatGrowthPerFloor StatGrowthPerFloor => _statGrowthPerFloor;

        [Tooltip("Damage multipliers by effective floor for normal fights and elites without an override. Elites use floor + 1. Empty means 1; later floors reuse the last entry. Bosses use their own multipliers.")]
        [FormerlySerializedAs("_ordinaryDamageMultipliersByFloor")]
        [SerializeField] float[] _damageMultipliersByEffectiveFloor = System.Array.Empty<float>();

        public float GetDamageMultiplier(int effectiveFloor)
        {
            if (_damageMultipliersByEffectiveFloor == null || _damageMultipliersByEffectiveFloor.Length == 0)
                return 1f;

            int index = Mathf.Clamp(Mathf.Max(1, effectiveFloor) - 1, 0, _damageMultipliersByEffectiveFloor.Length - 1);
            float value = _damageMultipliersByEffectiveFloor[index];
            return value > 0f && !float.IsInfinity(value) ? value : 1f;
        }

        [Tooltip("Optional elite K by actual map floor. Missing, zero or invalid entries use the effective-floor table. Short arrays do not repeat their last entry.")]
        [SerializeField] float[] _eliteDamageMultipliersByMapFloor = System.Array.Empty<float>();

        public float GetEliteDamageMultiplier(int mapFloor)
        {
            int floor = Mathf.Max(1, mapFloor);
            int index = floor - 1;
            if (_eliteDamageMultipliersByMapFloor != null && index < _eliteDamageMultipliersByMapFloor.Length)
            {
                float value = _eliteDamageMultipliersByMapFloor[index];
                if (value > 0f && !float.IsInfinity(value))
                    return value;
            }

            return GetDamageMultiplier(EnemyProgressionResolver.GetEffectiveFloor(floor, MapNodeType.Elite));
        }

        [Tooltip("Fallback boss damage multiplier when no valid map-floor override is authored.")]
        [Min(0.01f), SerializeField] float _bossDamageMultiplier = 2.1f;

        public float BossDamageMultiplier =>
            _bossDamageMultiplier > 0f && !float.IsInfinity(_bossDamageMultiplier) ? _bossDamageMultiplier : 1f;

        [Tooltip("Optional boss damage multipliers by actual map floor. Later floors reuse the last entry. Missing or invalid entries use Boss Damage Multiplier.")]
        [SerializeField] float[] _bossDamageMultipliersByMapFloor = System.Array.Empty<float>();

        public float GetBossDamageMultiplier(int mapFloor)
        {
            if (_bossDamageMultipliersByMapFloor == null || _bossDamageMultipliersByMapFloor.Length == 0)
                return BossDamageMultiplier;

            int index = Mathf.Clamp(Mathf.Max(1, mapFloor) - 1, 0, _bossDamageMultipliersByMapFloor.Length - 1);
            float value = _bossDamageMultipliersByMapFloor[index];
            return value > 0f && !float.IsInfinity(value) ? value : BossDamageMultiplier;
        }

        [Tooltip("Visual / behaviour prefab for this enemy. Root must have EnemyBattleCharacter.")]
        [SerializeField] EnemyBattleCharacter _enemyCharacterPrefab;

        /// <summary>Prefab instantiated for this enemy in battle.</summary>
        public EnemyBattleCharacter EnemyCharacterPrefab => _enemyCharacterPrefab;

        [Tooltip("Fallback skill list used when Skill Unlocks is empty. Every entry unlocks on floor 1.")]
        [SerializeField] SkillDefinition[] _skills;

        [Tooltip("Skills that join the active pool from a minimum effective floor.")]
        [SerializeField] EnemySkillUnlock[] _skillUnlocks;

        [Tooltip("Rank labels such as Zwiadowca or Weteran. The highest qualifying floor wins.")]
        [SerializeField] EnemyRankThreshold[] _rankThresholds;

        [Tooltip("Visual prefab spawned in the battle world. Root should have WorldCharacter.")]
        [SerializeField] GameObject _enemyModelPrefab;

        public SkillDefinition[] Skills => _skills;

        public IReadOnlyList<EnemyRankThreshold> RankThresholds => _rankThresholds;

        public GameObject EnemyModelPrefab => _enemyModelPrefab;

        /// <summary>
        /// Authored unlocks, or the legacy <see cref="Skills"/> list treated as floor-1 unlocks.
        /// Does not mutate this asset.
        /// </summary>
        public void CollectSkillUnlocks(List<EnemySkillUnlock> results)
        {
            if (results == null)
                return;

            if (_skillUnlocks != null && _skillUnlocks.Length > 0)
            {
                for (int i = 0; i < _skillUnlocks.Length; i++)
                {
                    if (_skillUnlocks[i].IsValid)
                        results.Add(_skillUnlocks[i]);
                }

                return;
            }

            if (_skills == null)
                return;

            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] == null)
                    continue;

                results.Add(new EnemySkillUnlock
                {
                    Skill = _skills[i],
                    MinFloor = 1
                });
            }
        }
    }
}
