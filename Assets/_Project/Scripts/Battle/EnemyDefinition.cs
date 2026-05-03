using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "M3P/Enemy Definition", order = 2)]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] string _name;

        /// <remarks>Distinct from Unity’s inherited <see cref="Object.name"/> (asset filename).</remarks>
        public string Name => _name;

        public int maxHP = 100;

        [SerializeField] SkillDefinition[] _skills;

        public SkillDefinition[] Skills => _skills;
    }
}
