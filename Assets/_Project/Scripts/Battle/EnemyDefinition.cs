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

        [SerializeField] Sprite _icon;

        [Tooltip("Visual / behaviour prefab for this enemy. Root must have EnemyBattleCharacter.")]
        [SerializeField] EnemyBattleCharacter _enemyCharacterPrefab;

        /// <summary>Icon shown in battle UI.</summary>
        public Sprite Icon => _icon;

        /// <summary>Prefab instantiated for this enemy in battle.</summary>
        public EnemyBattleCharacter EnemyCharacterPrefab => _enemyCharacterPrefab;

        [SerializeField] SkillDefinition[] _skills;

        public SkillDefinition[] Skills => _skills;
        public GameObject EnemyModelPrefab;
    }
}
