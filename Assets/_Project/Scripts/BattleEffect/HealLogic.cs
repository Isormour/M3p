using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class HealLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _amount;
        [Tooltip("Added as coefficient × the caster's property, before the magic multiplier.")]
        [SerializeField] SkillPropertyTerm[] _propertyTerms = Array.Empty<SkillPropertyTerm>();

        public int Amount => _amount;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            SoftStats softStats = character?.Stats?.Soft;
            if (softStats == null)
                return;

            int amount = ResolveAmount(context);
            if (amount <= 0)
                return;

            softStats.HealFromSkill(amount);
            Sfx.Play(SfxId.Healing);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleHealOrShield(preview, RawAmount(preview));
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Heal, $"Leczy {amount}"));
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
            return progression.ScaleMagicEffect(casterHard, RawAmount(context), talents);
        }

        int RawAmount(BattleEffectContext context)
        {
            return SkillPropertyScaling.Apply(_amount, _propertyTerms, SkillPropertyScaling.Read(context));
        }
    }
}
