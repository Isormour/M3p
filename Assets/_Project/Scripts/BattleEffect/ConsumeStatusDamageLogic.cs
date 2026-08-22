using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Spends every stack of a status on the target and deals damage for each one consumed.
    /// Zapłon is the authored example: all Burn stacks, 4 magic damage per stack.
    /// </summary>
    [Serializable]
    public class ConsumeStatusDamageLogic : BattleEffectLogic
    {
        [SerializeField] StatusEffectDefinition _status;
        [Min(1), SerializeField] int _damagePerStack = 4;
        [SerializeField] bool _physical;

        public StatusEffectDefinition Status => _status;
        public int DamagePerStack => Mathf.Max(1, _damagePerStack);
        public bool Physical => _physical;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            if (character == null || _status == null || _damagePerStack <= 0)
                return;

            int stacks = character.ConsumeStatus(_status);
            SkillCombat.DealScaledDamage(context, target, stacks * DamagePerStack, _physical);
        }
    }
}
