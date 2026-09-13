using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class ApplyStatusLogic : BattleEffectLogic, ITooltipPreview
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

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            string name = _status.DisplayName;
            int duration = _status.DurationTurns;
            int stacks = Stacks;
            string text = stacks > 1
                ? $"Nakłada {stacks} stacki: {name} ({duration} tury)"
                : $"Nakłada {name} ({duration} tury)";
            lines.Add(new TooltipLine(TooltipLineKind.Status, text));
        }
    }
}
