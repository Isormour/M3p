using System.Collections.Generic;
using System.Text;
using Match3;
using UnityEngine;

namespace M3P
{
    public static class TooltipBuilder
    {
        public static TooltipContent FromSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            var content = new TooltipContent();
            if (skill == null)
                return content;

            content.Title = skill.DisplayName;
            content.Description = skill.Description;
            content.Icon = skill.Artwork;
            content.Meta = BuildSkillMeta(skill, caster);

            var context = new BattleEffectContext(caster, target, directHit: true);
            AppendEffects(skill.Effects, context, content.Lines);
            return content;
        }

        public static TooltipContent FromCard(BoardActionCardDefinition card)
        {
            var content = new TooltipContent();
            if (card == null)
                return content;

            content.Title = card.DisplayName;
            content.Description = card.Description;
            content.Icon = card.Artwork;
            content.Meta = card.ActionPointCost > 0
                ? $"Koszt: {card.ActionPointCost} AP"
                : "Bez kosztu AP";

            string targeting = TargetingLabel(card.Targeting);
            if (!string.IsNullOrEmpty(targeting))
                content.Lines.Add(new TooltipLine(TooltipLineKind.Other, targeting));

            return content;
        }

        public static TooltipContent FromStatus(StatusInstance status, BattleCharacter bearer, int stacks = 1)
        {
            var content = new TooltipContent();
            if (status?.Definition == null)
                return content;

            StatusEffectDefinition definition = status.Definition;
            int count = Mathf.Max(1, stacks);
            content.Title = definition.DisplayName;
            content.Description = definition.Description;
            content.Icon = definition.Icon;
            content.Meta = BuildStatusMeta(status, count);

            AppendStatModifiers(definition, count, content.Lines);

            var context = new BattleEffectContext(
                status.Source != null ? status.Source : bearer,
                bearer,
                statusStacks: count);
            AppendEffects(definition.OnTurnEffects, context, content.Lines, forceTarget: EEffectTarget.Opponent);
            return content;
        }

        static void AppendEffects(
            BattleEffect[] effects,
            BattleEffectContext context,
            List<TooltipLine> lines,
            EEffectTarget? forceTarget = null)
        {
            if (effects == null)
                return;

            for (int i = 0; i < effects.Length; i++)
            {
                BattleEffect effect = effects[i];
                if (effect?.Logic is not ITooltipPreview preview)
                    continue;

                EEffectTarget target = forceTarget ?? effect.Target;
                preview.AppendTooltipLines(context, target, lines);
            }
        }

        static string BuildSkillMeta(SkillDefinition skill, BattleCharacter caster)
        {
            var parts = new List<string>(4);

            string mana = FormatManaCosts(skill);
            if (!string.IsNullOrEmpty(mana))
                parts.Add(mana);

            int remaining = caster != null ? caster.GetRemainingCooldown(skill) : 0;
            if (remaining > 0)
                parts.Add($"Gotowe za {remaining} tur");
            else if (skill.Cooldown > 0)
                parts.Add($"Odnowienie: {skill.Cooldown}");

            if (skill.OncePerTurn)
                parts.Add("Raz na turę");

            return string.Join("  ·  ", parts);
        }

        static string FormatManaCosts(SkillDefinition skill)
        {
            TileTypeManaCost[] costs = skill.ManaCosts;
            var builder = new StringBuilder();
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0 || costs[i].TileType == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(costs[i].Amount);
                builder.Append(' ');
                builder.Append(costs[i].TileType.name);
            }

            if (skill.DistinctColorManaCost > 0)
            {
                if (builder.Length > 0)
                    builder.Append(", ");
                builder.Append(skill.DistinctColorManaCost);
                builder.Append(" różnych kolorów");
            }

            return builder.Length > 0 ? $"Mana: {builder}" : string.Empty;
        }

        static string BuildStatusMeta(StatusInstance status, int stacks)
        {
            var parts = new List<string>(3);
            parts.Add($"Tury: {Mathf.Max(0, status.RemainingTurns)}");
            if (stacks > 1)
                parts.Add($"Stacki: {stacks}");
            if (status.Definition.CanStack)
                parts.Add("Stosuje się");
            return string.Join("  ·  ", parts);
        }

        static void AppendStatModifiers(StatusEffectDefinition definition, int stacks, List<TooltipLine> lines)
        {
            StatusStatModifier[] modifiers = definition.StatModifiers;
            for (int i = 0; i < modifiers.Length; i++)
            {
                StatusStatModifier modifier = modifiers[i];
                if (modifier.Amount == 0 || modifier.Stat == EStatType.Constitution)
                    continue;

                int amount = modifier.Amount * stacks;
                string sign = amount > 0 ? "+" : string.Empty;
                lines.Add(new TooltipLine(
                    TooltipLineKind.Other,
                    $"{StatLabel(modifier.Stat)} {sign}{amount}"));
            }
        }

        static string TargetingLabel(CardTargeting targeting)
        {
            return targeting switch
            {
                CardTargeting.SingleTile => "Wybierz kafelek",
                CardTargeting.AdjacentPair => "Wybierz dwa sąsiednie kafelki",
                CardTargeting.Triple => "Wybierz trzy kafelki",
                _ => string.Empty
            };
        }

        static string StatLabel(EStatType stat)
        {
            return stat switch
            {
                EStatType.Strength => "Siła",
                EStatType.Intelligence => "Inteligencja",
                EStatType.Constitution => "Kondycja",
                EStatType.Agility => "Zręczność",
                _ => stat.ToString()
            };
        }
    }
}
