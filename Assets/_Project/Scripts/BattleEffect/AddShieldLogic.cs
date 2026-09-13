using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class AddShieldLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _amount;

        public int Amount => _amount;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_amount <= 0)
                return;

            BattleCharacter character = context.Resolve(target);
            SoftStats softStats = character?.Stats?.Soft;
            if (softStats == null)
                return;

            int amount = ResolveAmount(context);
            softStats.AddShield(amount);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleHealOrShield(preview, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Shield, $"Daje {amount} tarczy"));
        }

        int ResolveAmount(BattleEffectContext context)
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            StatProgressionConfig progression = config != null
                ? config.StatProgression
                : StatProgressionConfig.CreateDefault();

            HardStats casterHard = context.Caster != null
                ? context.Caster.GetEffectiveHard()
                : default;
            TalentBonuses talents = context.Caster?.Stats?.TalentBonuses ?? TalentBonuses.None;
            return progression.ScaleMagicEffect(casterHard, _amount, talents);
        }
    }
}
