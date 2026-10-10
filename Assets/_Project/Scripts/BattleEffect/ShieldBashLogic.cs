using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Deals damage equal to the caster's current Shield, then clears that Shield.
    /// </summary>
    [Serializable]
    public class ShieldBashLogic : BattleEffectLogic, ITooltipPreview, ISkillCastRequirement
    {
        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            return caster?.Stats?.Soft != null && caster.Stats.Soft.CurrentShield > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            BattleCharacter character = context.Resolve(target);
            SoftStats targetSoft = character?.Stats?.Soft;
            if (casterSoft == null || targetSoft == null)
                return;

            int amount = casterSoft.CurrentShield;
            if (amount <= 0)
                return;

            casterSoft.ClearShield();
            SkillCombat.ApplyHit(context, character, amount, true);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = preview.Caster?.Stats?.Soft != null
                ? preview.Caster.Stats.Soft.CurrentShield
                : 0;
            if (amount > 0 && preview.DirectHit && preview.Caster != null)
                amount += preview.Caster.GetPersistentOutgoingBonus();
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {amount} obrażeń (aktualna tarcza)"));
            else
                lines.Add(new TooltipLine(TooltipLineKind.Other, "Zadaje obrażenia równe aktualnej tarczy"));
        }
    }
}
