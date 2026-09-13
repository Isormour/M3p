using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class DealDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _amount;
        [Tooltip("When true, Strength scales the hit. When false, Intelligence scales it as a magic effect.")]
        [SerializeField] bool _physical;

        public int Amount => _amount;
        public bool Physical => _physical;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SkillCombat.DealScaledDamage(context, target, _amount * context.StatusStacks, _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleAmount(preview, _amount * preview.StatusStacks, _physical);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {amount} obrażeń"));
        }
    }
}
