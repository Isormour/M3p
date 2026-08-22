using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class ApplyStatusLogic : BattleEffectLogic
    {
        [SerializeField] StatusEffectDefinition _status;
        [Min(1), SerializeField] int _stacks = 1;

        public StatusEffectDefinition Status => _status;
        public int Stacks => Mathf.Max(1, _stacks);

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_status == null)
                return;

            BattleCharacter character = context.Resolve(target);
            if (character == null)
                return;

            int stacks = Stacks;
            for (int i = 0; i < stacks; i++)
                character.ApplyStatus(_status, context.Caster);
        }
    }
}
