using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "M3P/Enemy Definition", order = 2)]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] string _name;

        /// <remarks>Distinct from Unity’s inherited <see cref="Object.name"/> (asset filename).</remarks>
        public string Name => _name;

        [SerializeField] HardStats _hardStats = new HardStats(1, 1, 10, 1);

        public HardStats HardStats => _hardStats;

        [Tooltip("Experience the player banks for winning this fight. Nothing is paid out for a loss.")]
        [Min(0), SerializeField] int _experienceReward = 50;

        public int ExperienceReward => _experienceReward;

        [SerializeField] Sprite _icon;

        [Tooltip("Visual / behaviour prefab for this enemy. Root must have EnemyBattleCharacter.")]
        [SerializeField] EnemyBattleCharacter _enemyCharacterPrefab;

        /// <summary>Icon shown in battle UI.</summary>
        public Sprite Icon => _icon;

        /// <summary>Prefab instantiated for this enemy in battle.</summary>
        public EnemyBattleCharacter EnemyCharacterPrefab => _enemyCharacterPrefab;

        [SerializeField] SkillDefinition[] _skills;

        [Tooltip("Visual prefab spawned in the battle world. Root should have WorldCharacter.")]
        [SerializeField] GameObject _enemyModelPrefab;

        public SkillDefinition[] Skills => _skills;

        public GameObject EnemyModelPrefab => _enemyModelPrefab;
    }
}
