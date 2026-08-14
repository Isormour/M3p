using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class ApplyStatusLogic : BattleEffectLogic
    {
        [SerializeField] StatusEffectDefinition _status;

        public StatusEffectDefinition Status => _status;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_status == null)
                return;

            BattleCharacter character = context.Resolve(target);
            character?.ApplyStatus(_status, context.Caster);
        }
    }
}
