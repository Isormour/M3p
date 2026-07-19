using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class DealDamageLogic : BattleEffectLogic
    {
        [SerializeField] int _amount;

        public int Amount => _amount;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_amount <= 0)
                return;

            BattleCharacter character = context.Resolve(target);
            character?.Stats?.Soft?.TakeDamage(_amount);
        }
    }
}
