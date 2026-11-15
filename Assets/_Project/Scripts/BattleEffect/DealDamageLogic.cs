using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class DealDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _amount;
        [Tooltip("Flat damage added after talent, attack and enemy multipliers, before rounding, hit bonuses and reductions.")]
        [Min(0), SerializeField] int _flatDamageAfterEnemyScaling;
        [Tooltip("When true, Strength scales the hit. When false, Intelligence scales it as a magic effect.")]
        [SerializeField] bool _physical;
        [Tooltip("Added as coefficient × the caster's property. Heavy Slash is 6 + 2 × Power.")]
        [SerializeField] SkillPropertyTerm[] _propertyTerms = Array.Empty<SkillPropertyTerm>();

        public int Amount => _amount;
        public bool Physical => _physical;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SkillCombat.DealScaledDamage(context, target, Magnitude(context), _physical, FlatDamage(context));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.PreviewScaledDamage(preview, target, Magnitude(preview), _physical,
                includeAttackBonus: true, flatDamageAfterEnemyScaling: FlatDamage(preview));
            if (preview.DirectHit && preview.Caster != null)
                amount += preview.Caster.GetPersistentOutgoingBonus();
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Deal {amount} damage"));
        }

        int FlatDamage(BattleEffectContext context)
        {
            return Mathf.Max(0, _flatDamageAfterEnemyScaling) * Mathf.Max(1, context.StatusStacks);
        }

        int Magnitude(BattleEffectContext context)
        {
            int scaled = SkillPropertyScaling.Apply(
                _amount,
                _propertyTerms,
                SkillPropertyScaling.Read(context));
            return scaled * Mathf.Max(1, context.StatusStacks);
        }
    }
}
