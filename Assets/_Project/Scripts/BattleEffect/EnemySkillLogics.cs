using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Damage of <c>base + amount × floor × rank</c>, optionally as several hits.
    /// The per-hit amount is the value before strength talents and lasting attack bonuses.
    /// </summary>
    [Serializable]
    public class FloorScaledDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [Tooltip("Flat damage added after floor and rank scaling.")]
        [Min(0), SerializeField] int _baseAmount;
        [SerializeField] int _amount = 1;
        [SerializeField] bool _scaleFloor = true;
        [SerializeField] bool _scaleRank;
        [SerializeField] int _hitCount = 1;
        [SerializeField] bool _scaleHitsByFloor;
        [SerializeField] bool _physical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            int hits = HitCount(context);
            int amount = DamagePerHit(context);
            if (hits <= 1)
            {
                SkillCombat.DealScaledDamage(context, target, amount, _physical);
                return;
            }

            SkillCombat.DealScaledHits(context, target, hits, amount, _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int hits = HitCount(preview);
            int scaled = SkillCombat.ScaleAmount(preview, DamagePerHit(preview), _physical);
            int bonus = preview.Caster != null ? preview.Caster.GetPersistentOutgoingBonus() : 0;
            int perHit = scaled + bonus;
            if (perHit <= 0)
                return;

            if (hits > 1)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perHit} obrażeń × {hits}"));
            else
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perHit} obrażeń"));
        }

        int DamagePerHit(BattleEffectContext context)
        {
            int amount = Mathf.Max(0, _amount);
            if (_scaleFloor)
                amount *= EnemySkillScale.Floor(context.Caster);
            if (_scaleRank)
                amount *= EnemySkillScale.Rank(context.Caster);
            return Mathf.Max(0, _baseAmount) + amount;
        }

        int HitCount(BattleEffectContext context)
        {
            int hits = Mathf.Max(1, _hitCount);
            if (_scaleHitsByFloor)
                hits *= EnemySkillScale.Floor(context.Caster);
            return hits;
        }
    }

    /// <summary>Applies several stacks of a status, scaled by the caster's floor.</summary>
    [Serializable]
    public class ApplyBurnStacksLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] StatusEffectDefinition _status;
        [Min(1), SerializeField] int _stacksPerFloor = 1;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_status == null)
                return;

            BattleCharacter character = context.Resolve(target);
            if (character == null)
                return;

            int stacks = StackCount(context);
            character.ApplyStatusStacks(_status, context.Caster, stacks, 0, 0);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            int stacks = StackCount(preview);
            string name = _status.DisplayName;
            lines.Add(new TooltipLine(
                TooltipLineKind.Status,
                stacks > 1 ? $"Nakłada {stacks} stacki: {name}" : $"Nakłada {name}"));
        }

        int StackCount(BattleEffectContext context)
        {
            return Mathf.Max(1, _stacksPerFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>
    /// Applies a status whose duration and payload scale with floor or rank.
    /// Reapplying a non-stacking status refreshes the duration and the stored magnitude.
    /// </summary>
    [Serializable]
    public class ApplyScaledStatusLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] StatusEffectDefinition _status;
        [SerializeField] int _duration = 1;
        [SerializeField] EnemyScaleAxis _durationAxis;
        [Tooltip("Added after scaling. Use 1 when the status must still be active on the bearer's action after the start-of-turn tick.")]
        [SerializeField] int _extraDuration;
        [SerializeField] int _magnitude;
        [SerializeField] EnemyScaleAxis _magnitudeAxis;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_status == null)
                return;

            BattleCharacter character = context.Resolve(target);
            if (character == null)
                return;

            character.ApplyStatusStacks(
                _status,
                context.Caster,
                1,
                Duration(context),
                Magnitude(context));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            int duration = Duration(preview);
            lines.Add(new TooltipLine(
                TooltipLineKind.Status,
                $"Nakłada {_status.DisplayName} ({duration} tury)"));

            int magnitude = Magnitude(preview);
            _status.AppendPersistentModifierTooltip(lines, magnitude);
        }

        int Duration(BattleEffectContext context)
        {
            int scaled = EnemySkillScale.Scale(Mathf.Max(1, _duration), _durationAxis, context.Caster);
            return Mathf.Max(1, scaled + _extraDuration);
        }

        int Magnitude(BattleEffectContext context)
        {
            return EnemySkillScale.Scale(_magnitude, _magnitudeAxis, context.Caster);
        }
    }

    /// <summary>Heals the bearer for a flat amount times the source's floor. Used by regen ticks.</summary>
    [Serializable]
    public class FloorScaledHealLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 2;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            int amount = HealAmount(context);
            if (amount <= 0)
                return;

            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            if (soft == null)
                return;

            soft.Heal(SkillCombat.ScaleHealOrShield(context, amount));
            Sfx.Play(SfxId.Healing);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleHealOrShield(preview, HealAmount(preview));
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Heal, $"Leczy {amount}"));
        }

        int HealAmount(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Shield equal to a coefficient times the caster's floor.</summary>
    [Serializable]
    public class FloorScaledShieldLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 10;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            int amount = SkillCombat.ScaleHealOrShield(context, ShieldAmount(context));
            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            if (soft == null || amount <= 0)
                return;

            soft.AddShield(amount);
            Sfx.Play(SfxId.ShieldApply);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = SkillCombat.ScaleHealOrShield(preview, ShieldAmount(preview));
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Shield, $"Daje {amount} tarczy"));
        }

        int ShieldAmount(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>
    /// Moves shield from the target onto the caster. The stolen amount is what the target
    /// actually had, capped by the floor formula.
    /// </summary>
    [Serializable]
    public class StealShieldLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 5;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats victim = context.Resolve(target)?.Stats?.Soft;
            SoftStats thief = context.Caster?.Stats?.Soft;
            int requested = Requested(context);
            if (victim == null || thief == null || requested <= 0)
                return;

            int stolen = victim.RemoveShield(requested);
            if (stolen > 0)
                thief.AddShield(stolen);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int requested = Requested(preview);
            if (requested > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Shield, $"Kradnie do {requested} tarczy"));
        }

        int Requested(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Removes a floor-scaled amount of mana from every colour the target holds.</summary>
    [Serializable]
    public class DrainAllManaLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 3;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            int drain = DrainAmount(context);
            if (soft == null || config == null || drain <= 0)
                return;

            for (int i = 0; i < config.TileTypeCount; i++)
            {
                int have = soft.GetManaForTileType(i);
                if (have <= 0)
                    continue;

                soft.SetManaForTileType(i, Mathf.Max(0, have - drain));
            }
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int drain = DrainAmount(preview);
            if (drain > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Odbiera {drain} many każdego koloru"));
        }

        int DrainAmount(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Removes action points from the bearer. A stored status drain wins; otherwise the source's floor scales the amount.</summary>
    [Serializable]
    public class DrainActionPointsLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 1;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            int amount = DrainAmount(context);
            if (soft == null || amount <= 0)
                return;

            soft.RemoveActionPoints(amount);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int amount = DrainAmount(preview);
            if (amount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Odbiera {amount} AP"));
        }

        int DrainAmount(BattleEffectContext context)
        {
            if (context.ActionPointDrain > 0)
                return context.ActionPointDrain;

            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }

    /// <summary>Roots random movable tiles. A rooted tile stays in its cell until a match destroys it.</summary>
    [Serializable]
    public class RootRandomTilesLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _perFloor = 3;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            Match3.Match3Board board = BattleManager.Instance != null ? BattleManager.Instance.ActiveBoard : null;
            if (board == null)
                return;

            board.LockRandomTiles(TileCount(context));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int count = TileCount(preview);
            if (count > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Zakorzenia {count} kafelków"));
        }

        int TileCount(BattleEffectContext context)
        {
            return Mathf.Max(0, _perFloor) * EnemySkillScale.Floor(context.Caster);
        }
    }
}
