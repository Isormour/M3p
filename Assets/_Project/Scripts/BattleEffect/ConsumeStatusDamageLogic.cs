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
    public class ConsumeStatusDamageLogic : BattleEffectLogic, ITooltipPreview, ISkillCastRequirement
    {
        [SerializeField] StatusEffectDefinition _status;
        [Min(1), SerializeField] int _damagePerStack = 4;
        [SerializeField] bool _physical;
        [Tooltip("Added after damage per stack. Zapłon adds Arcane.")]
        [SerializeField] SkillPropertyTerm[] _propertyTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When true, the skill is illegal unless the target already has at least one stack.")]
        [SerializeField] bool _requireStacks;

        public StatusEffectDefinition Status => _status;
        public int DamagePerStack => Mathf.Max(1, _damagePerStack);
        public bool Physical => _physical;

        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            if (!_requireStacks || _status == null)
                return true;

            return target != null && target.CountStatus(_status) > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            if (character == null || _status == null || _damagePerStack <= 0)
                return;

            int stacks = character.ConsumeStatus(_status);
            if (stacks <= 0)
                return;

            if (_status.StatusType == EStatusType.Burn)
                context.Caster?.Stats?.Soft?.PerkRules?.OnBurnStacksConsumed(stacks);

            int damage = stacks * DamagePerStack;
            damage = SkillPropertyScaling.Apply(damage, _propertyTerms, SkillPropertyScaling.Read(context));
            SkillCombat.DealScaledDamage(context, target, damage, _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            string statusName = _status.DisplayName;
            int stacks = preview.Resolve(target)?.CountStatus(_status) ?? 0;
            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            if (stacks > 0)
            {
                int raw = SkillPropertyScaling.Apply(stacks * DamagePerStack, _propertyTerms, properties);
                int damage = SkillCombat.ScaleAmount(preview, raw, _physical, includeAttackBonus: true);
                if (damage > 0)
                    lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {damage} obrażeń ({stacks} × {statusName})"));
                return;
            }

            int perStack = SkillCombat.ScaleAmount(preview, DamagePerStack, _physical, includeAttackBonus: true);
            if (perStack > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perStack} obrażeń za każdy stack: {statusName}"));
        }
    }
}
