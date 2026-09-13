using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Spends every stack of a status on the target and deals damage for each one consumed.
    /// Zapłon is the authored example: all Burn stacks, 4 magic damage per stack.
    /// </summary>
    [Serializable]
    public class ConsumeStatusDamageLogic : BattleEffectLogic, ITooltipPreview
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

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            string statusName = _status.DisplayName;
            int stacks = preview.Resolve(target)?.CountStatus(_status) ?? 0;
            if (stacks > 0)
            {
                int damage = SkillCombat.ScaleAmount(preview, stacks * DamagePerStack, _physical);
                if (damage > 0)
                    lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {damage} obrażeń ({stacks} × {statusName})"));
                return;
            }

            int perStack = SkillCombat.ScaleAmount(preview, DamagePerStack, _physical);
            if (perStack > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perStack} obrażeń za każdy stack: {statusName}"));
        }
    }
}
