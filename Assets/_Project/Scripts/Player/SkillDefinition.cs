using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "M3P/Skill Definition", order = 0)]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] int _skillId;

        public int SkillId => _skillId;
    }
}
