using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "BasicAttackSkill", menuName = "M3P/Basic Attack Skill", order = 1)]
    public class BasicAttackSkill : SkillDefinition
    {
        [SerializeField] int _damage;

        public int Damage => _damage;
    }
}
