using System.Collections.Generic;
using UnityEngine;

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

        [Tooltip("Experience the player banks for winning a floor-1 normal fight. Scaled at runtime.")]
        [Min(0), SerializeField] int _experienceReward = 50;

        public int ExperienceReward => _experienceReward;

        [Tooltip("Added each effective floor after the first. Fractions accumulate, then floor.")]
        [SerializeField] StatGrowthPerFloor _statGrowthPerFloor;

        public StatGrowthPerFloor StatGrowthPerFloor => _statGrowthPerFloor;

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
