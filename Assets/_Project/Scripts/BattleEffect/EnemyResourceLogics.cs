using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Picks one mana colour the target actually holds and removes up to the requested amount.</summary>
    public static class ManaDrain
    {
        public static bool HasPositiveMana(SoftStats soft)
        {
            return CollectPositiveTypes(soft).Count > 0;
        }

        public static int DrainRandomPositive(SoftStats soft, int requested)
        {
            if (soft == null || requested <= 0)
                return 0;

            List<int> types = CollectPositiveTypes(soft);
            if (types.Count == 0)
                return 0;

            int pick = BattleRoll.NextIndex(types.Count);
            if (pick < 0 || pick >= types.Count)
                return 0;

            int typeId = types[pick];
            int have = soft.GetManaForTileType(typeId);
            int removed = Mathf.Min(have, requested);
            if (removed <= 0)
                return 0;

            soft.SetManaForTileType(typeId, have - removed);
            return removed;
        }

        static List<int> CollectPositiveTypes(SoftStats soft)
        {
            var types = new List<int>();
            if (soft?.ManaByBrokenTileType == null)
                return types;

            List<TileTypeMana> mana = soft.ManaByBrokenTileType;
            for (int i = 0; i < mana.Count; i++)
            {
                if (mana[i].Amount > 0)
                    types.Add(mana[i].TileTypeId);
            }

            types.Sort();
            return types;
        }
    }

    /// <summary>Armor equal to a coefficient times the caster's floor.</summary>
    [Serializable]
    public class FloorScaledArmorLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 10;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            int amount = SkillCombat.ScaleHealOrShield(context, ArmorAmount(context));
            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            if (soft == null || amount <= 0)
                return;

            soft.AddArmor(amount);
            Sfx.Play(SfxId.ShieldApply);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleHealOrShield(preview, ArmorAmount(preview));
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Daje {amount} pancerza"));
        }

        int ArmorAmount(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>
    /// Takes the target's armor, then shield, up to the floor cap, and stores the stolen
    /// amount as the caster's armor. The player builds shield, so armor-only theft steals nothing.
    /// </summary>
    [Serializable]
    public class StealArmorLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 5;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats victim = context.Resolve(target)?.Stats?.Soft;
            SoftStats thief = context.Caster?.Stats?.Soft;
            int requested = Requested(context);
            if (victim == null || thief == null || requested <= 0)
                return;

            int stolen = victim.RemoveArmor(requested);
            stolen += victim.RemoveShield(requested - stolen);
            if (stolen <= 0)
                return;

            thief.AddArmor(stolen);
            Sfx.Play(SfxId.ShieldApply);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int requested = Requested(preview);
            if (requested > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Shield, $"Kradnie do {requested} tarczy lub pancerza"));
        }

        int Requested(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Deals damage equal to the caster's current Armor, then clears that Armor.</summary>
    [Serializable]
    public class ArmorBashLogic : BattleEffectLogic, ITooltipPreview, ISkillCastRequirement
    {
        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            return caster?.Stats?.Soft != null && caster.Stats.Soft.CurrentArmor > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            BattleCharacter character = context.Resolve(target);
            if (casterSoft == null || character?.Stats?.Soft == null)
                return;

            int amount = casterSoft.CurrentArmor;
            if (amount <= 0)
                return;

            casterSoft.ClearArmor();
            SkillCombat.ApplyHit(context, character, amount, true);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = preview.Caster?.Stats?.Soft != null
                ? preview.Caster.Stats.Soft.CurrentArmor
                : 0;
            if (amount > 0 && preview.DirectHit && preview.Caster != null)
                amount += preview.Caster.GetPersistentOutgoingBonus();
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {amount} obrażeń (aktualny pancerz)"));
            else
                lines.Add(new TooltipLine(TooltipLineKind.Other, "Zadaje obrażenia równe aktualnemu pancerzowi"));
        }
    }

    /// <summary>Removes mana from one random colour the target still holds. Optionally heals the caster 1:1.</summary>
    [Serializable]
    public class DrainRandomManaLogic : BattleEffectLogic, ITooltipPreview, ISkillCastRequirement
    {
        [SerializeField] int _perFloor = 2;
        [SerializeField] bool _requirePositiveMana;
        [SerializeField] bool _healCasterForRemoved;

        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            if (!_requirePositiveMana)
                return true;

            return ManaDrain.HasPositiveMana(target?.Stats?.Soft);
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats victim = context.Resolve(target)?.Stats?.Soft;
            int removed = ManaDrain.DrainRandomPositive(victim, Requested(context));
            if (removed <= 0 || !_healCasterForRemoved)
                return;

            context.Caster?.Stats?.Soft?.Heal(removed);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int requested = Requested(preview);
            if (requested <= 0)
                return;

            string text = _healCasterForRemoved
                ? $"Usuwa do {requested} many losowego koloru i leczy o tyle samo"
                : $"Usuwa do {requested} many losowego koloru";
            lines.Add(new TooltipLine(TooltipLineKind.Other, text));
        }

        int Requested(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>
    /// While the caster carries the authored status, Enemy Strike also drains one random mana colour.
    /// The drain cap is the status magnitude captured when the status was applied.
    /// </summary>
    [Serializable]
    public class ArcaneHungerStrikeLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] StatusEffectDefinition _status;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            int magnitude = DrainCap(context);
            if (magnitude <= 0)
                return;

            ManaDrain.DrainRandomPositive(context.Resolve(target)?.Stats?.Soft, magnitude);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int magnitude = DrainCap(preview);
            if (magnitude <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Other, $"Usuwa do {magnitude} many losowego koloru"));
        }

        int DrainCap(BattleEffectContext context)
        {
            if (_status == null || context.Caster == null)
                return 0;

            return context.Caster.GetStatusMagnitude(_status);
        }
    }

    /// <summary>Pays HP that ignores Armor and Shield. The bearer is left with at least 1 HP.</summary>
    [Serializable]
    public class PayScaledHealthLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 3;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Caster?.Stats?.Soft;
            int cost = Cost(context);
            if (soft == null || cost <= 0)
                return;

            soft.LoseHealthIgnoringShield(cost, 1);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int cost = Cost(preview);
            if (cost > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Kosztuje {cost} HP"));
        }

        int Cost(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Deals floor-scaled damage and heals the caster for a percent of the HP the target actually lost.</summary>
    [Serializable]
    public class DevourLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _amount = 6;
        [SerializeField] bool _scaleFloor = true;
        [SerializeField] bool _scaleRank;
        [SerializeField] bool _physical = true;
        [SerializeField, Range(0, 100)] int _healPercent = 50;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            SoftStats victim = character?.Stats?.Soft;
            SoftStats caster = context.Caster?.Stats?.Soft;
            int amount = Damage(context);
            if (victim == null || amount <= 0)
                return;

            int healthBefore = victim.CurrentHealth;
            SkillCombat.DealScaledDamage(context, target, amount, _physical);
            int lost = Mathf.Max(0, healthBefore - victim.CurrentHealth);
            int heal = lost * Mathf.Clamp(_healPercent, 0, 100) / 100;
            if (heal > 0 && caster != null)
                caster.Heal(heal);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int scaled = SkillCombat.ScaleAmount(preview, Damage(preview), _physical);
            int bonus = preview.Caster != null ? preview.Caster.GetPersistentOutgoingBonus() : 0;
            int shown = scaled + bonus;
            if (shown > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {shown} obrażeń"));

            int percent = Mathf.Clamp(_healPercent, 0, 100);
            if (percent > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Heal, $"Leczy {percent}% utraconego HP"));
        }

        int Damage(BattleEffectContext context)
        {
            int amount = Mathf.Max(0, _amount);
            if (_scaleFloor)
                amount *= EnemySkillScale.Floor(context.Caster);
            if (_scaleRank)
                amount *= EnemySkillScale.Rank(context.Caster);
            return amount;
        }
    }
}
