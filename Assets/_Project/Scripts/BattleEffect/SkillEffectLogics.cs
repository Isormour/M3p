using Match3;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public interface ISkillCastRequirement
    {
        bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill);
    }

    public enum ResolveDamageFormula
    {
        PrecisionBonus = 0,
        PreviousResolves = 1,
        HitsPerGroup = 2,
        ExecuteBelowHealth = 3,
        CascadeWaves = 4,
        ColorsInResolve = 5,
        CardsInSequence = 6,
        CardsInSequencePrecision = 7,
        IntentConditional = 8,
    }

    [Serializable]
    public class ResolveScaledDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] ResolveDamageFormula _formula;
        [SerializeField] int _amount;
        [SerializeField] int _bonus;
        [SerializeField] int _perStep;
        [SerializeField] int _max;
        [SerializeField] int _threshold;
        [SerializeField] bool _physical;
        [Tooltip("Added as coefficient × the caster's property on top of the formula result.")]
        [SerializeField] SkillPropertyTerm[] _propertyTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When set, the per-step value is multiplied by this property before the cap.")]
        [SerializeField] bool _scalePerStepByProperty;
        [SerializeField] PlayerProperty _perStepProperty;
        [Tooltip("Added to the hit count of Hits Per Group. Grad ciosów adds Tempo.")]
        [SerializeField] bool _addPropertyToHitCount;
        [SerializeField] PlayerProperty _hitCountProperty;
        [Tooltip("Added only when the formula's condition is met. Perfekcyjny Cios uses 4 × floor(Power / 2).")]
        [SerializeField] SkillPropertyTerm[] _bonusTerms = Array.Empty<SkillPropertyTerm>();

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            switch (_formula)
            {
                case ResolveDamageFormula.HitsPerGroup:
                    SkillCombat.DealScaledHits(
                        context,
                        target,
                        HitCount(context),
                        WithProperties(context, _amount),
                        _physical);
                    return;
                case ResolveDamageFormula.CardsInSequence:
                case ResolveDamageFormula.CardsInSequencePrecision:
                    SkillCombat.DealScaledDamage(context, target, CardSequenceDamage(context), _physical);
                    return;
                case ResolveDamageFormula.ExecuteBelowHealth:
                    int execute = WithProperties(context, _amount);
                    if (IsBelowHealthThreshold(context, target))
                        execute *= 2;
                    SkillCombat.DealScaledDamage(context, target, execute, _physical);
                    return;
                case ResolveDamageFormula.PrecisionBonus:
                    int precision = WithProperties(context, _amount);
                    if (SkillCombat.LastResolveIsSingleMatchWithoutCascade())
                        precision += ConditionalBonus(context);
                    SkillCombat.DealScaledDamage(context, target, precision, _physical);
                    return;
            }

            SkillCombat.DealScaledDamage(context, target, WithProperties(context, ResolveAmount(context)), _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            switch (_formula)
            {
                case ResolveDamageFormula.HitsPerGroup:
                    int hits = HitCount(preview);
                    int perHit = PreviewDamage(preview, WithProperties(preview, _amount));
                    if (perHit > 0 && hits > 0)
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perHit} obrażeń × {hits}"));
                    return;
                case ResolveDamageFormula.CardsInSequence:
                case ResolveDamageFormula.CardsInSequencePrecision:
                    int sequenceDamage = PreviewDamage(preview, CardSequenceDamage(preview));
                    if (sequenceDamage > 0)
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {sequenceDamage} obrażeń"));
                    return;
                case ResolveDamageFormula.ExecuteBelowHealth:
                    int executeBase = PreviewDamage(preview, WithProperties(preview, _amount));
                    if (executeBase > 0)
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {executeBase} obrażeń"));
                    int executeDoubled = PreviewDamage(preview, WithProperties(preview, _amount) * 2);
                    if (executeDoubled > 0)
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"{executeDoubled} gdy wróg poniżej {_threshold}% HP"));
                    return;
                case ResolveDamageFormula.PrecisionBonus:
                    int precisionBase = PreviewDamage(preview, WithProperties(preview, _amount));
                    if (precisionBase > 0)
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {precisionBase} obrażeń"));
                    int precisionBonus = ConditionalBonus(preview);
                    if (precisionBonus > 0)
                    {
                        int precisionTotal = PreviewDamage(preview, WithProperties(preview, _amount) + precisionBonus);
                        lines.Add(new TooltipLine(TooltipLineKind.Damage, $"{precisionTotal} gdy jeden match bez kaskady"));
                    }
                    return;
            }

            int baseAmount = PreviewDamage(preview, WithProperties(preview, _amount));
            if (baseAmount > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {baseAmount} obrażeń"));

            if (_formula == ResolveDamageFormula.IntentConditional && _bonus > 0 && _bonus != _amount)
            {
                int bonus = PreviewDamage(preview, WithProperties(preview, _bonus));
                if (bonus > 0)
                    lines.Add(new TooltipLine(TooltipLineKind.Damage, $"{bonus} gdy wróg zapowiada atak"));
                return;
            }

            int resolved = PreviewDamage(preview, WithProperties(preview, ResolveAmount(preview)));
            if (resolved > 0 && resolved != baseAmount)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Teraz: {resolved}"));
        }

        int ResolveAmount(BattleEffectContext context)
        {
            switch (_formula)
            {
                case ResolveDamageFormula.PreviousResolves:
                    int frenzy = _amount + ScaledPerStep(context) * SkillCombat.PreviousResolveCountThisTurn();
                    return _max > 0 ? Mathf.Min(_max, frenzy) : frenzy;

                case ResolveDamageFormula.CascadeWaves:
                    int chain = _amount + ScaledPerStep(context) * SkillCombat.LastResolveExtraWaves();
                    return _max > 0 ? Mathf.Min(_max, chain) : chain;

                case ResolveDamageFormula.ColorsInResolve:
                    int colorCount = SkillCombat.LastResolveColorCount();
                    int colors = _scalePerStepByProperty
                        ? (_amount + ScaledPerStep(context)) * colorCount
                        : _amount + Mathf.Max(0, _perStep) * colorCount;
                    return _max > 0 ? Mathf.Min(_max, colors) : colors;

                case ResolveDamageFormula.IntentConditional:
                    return SkillCombat.IsEnemyTelegraphingOffensive(context.Target)
                        ? (_bonus > 0 ? _bonus : _amount)
                        : _amount;

                default:
                    return _amount;
            }
        }

        int HitCount(BattleEffectContext context)
        {
            int hits = SkillCombat.LastResolveGroupCount();
            if (_addPropertyToHitCount)
                hits += Mathf.Max(0, SkillPropertyScaling.Read(context).Get(_hitCountProperty));
            if (_max > 0)
                hits = Mathf.Min(_max, hits);
            return Mathf.Max(0, hits);
        }

        int CardSequenceDamage(BattleEffectContext context)
        {
            int cards = SkillCombat.LastResolveCardsInSequence();
            int fromCards = cards * ScaledPerStep(context);
            if (_max > 0)
                fromCards = Mathf.Min(_max, fromCards);
            if (_formula == ResolveDamageFormula.CardsInSequencePrecision
                && SkillCombat.LastResolveIsSingleMatchWithoutCascade())
                fromCards += ConditionalBonus(context);
            return WithProperties(context, fromCards);
        }

        int ScaledPerStep(BattleEffectContext context)
        {
            int step = Mathf.Max(0, _perStep);
            if (!_scalePerStepByProperty)
                return step;

            return step * Mathf.Max(0, SkillPropertyScaling.Read(context).Get(_perStepProperty));
        }

        int ConditionalBonus(BattleEffectContext context)
        {
            if (_bonusTerms != null && _bonusTerms.Length > 0)
                return Mathf.Max(0, SkillPropertyScaling.Apply(0, _bonusTerms, SkillPropertyScaling.Read(context)));

            return Mathf.Max(0, _bonus);
        }

        bool IsBelowHealthThreshold(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Resolve(target)?.Stats?.Soft;
            return soft != null
                && soft.MaxHP > 0
                && soft.CurrentHealth * 100 < soft.MaxHP * Mathf.Max(0, _threshold);
        }

        int WithProperties(BattleEffectContext context, int amount)
        {
            return SkillPropertyScaling.Apply(amount, _propertyTerms, SkillPropertyScaling.Read(context));
        }

        int PreviewDamage(BattleEffectContext preview, int amount)
        {
            return SkillCombat.ScaleAmount(preview, amount, _physical, includeAttackBonus: true);
        }
    }

    [Serializable]
    public class ConsumeShieldDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _maxDamage = 20;
        [SerializeField] bool _physical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            if (casterSoft == null)
                return;

            int damage = casterSoft.CurrentShield;
            if (_maxDamage > 0)
                damage = Mathf.Min(_maxDamage, damage);

            casterSoft.ClearShield();
            SkillCombat.DealScaledDamage(context, target, damage, _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int damage = preview.Caster?.Stats?.Soft != null
                ? preview.Caster.Stats.Soft.CurrentShield
                : 0;
            if (_maxDamage > 0 && damage > 0)
                damage = Mathf.Min(_maxDamage, damage);

            if (damage > 0)
            {
                int scaled = SkillCombat.ScaleAmount(preview, damage, _physical, includeAttackBonus: true);
                if (scaled > 0)
                    lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {scaled} obrażeń (tarcza)"));
                return;
            }

            lines.Add(new TooltipLine(TooltipLineKind.Other, "Zadaje obrażenia równe aktualnej tarczy"));
        }
    }

    [Serializable]
    public class ResolveScaledShieldLogic : BattleEffectLogic
    {
        [SerializeField] int _amount;
        [SerializeField] int _perGroup;
        [SerializeField] int _max;
        [Tooltip("Added as coefficient × the caster's property on top of the formula result.")]
        [SerializeField] SkillPropertyTerm[] _propertyTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When set, the per-group value is multiplied by this property before the cap.")]
        [SerializeField] bool _scalePerGroupByProperty;
        [SerializeField] PlayerProperty _perGroupProperty;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            int perGroup = Mathf.Max(0, _perGroup);
            if (_scalePerGroupByProperty)
                perGroup *= Mathf.Max(0, SkillPropertyScaling.Read(context).Get(_perGroupProperty));

            int shield = _amount + perGroup * SkillCombat.LastResolveGroupCount();
            if (_max > 0)
                shield = Mathf.Min(_max, shield);

            shield = SkillPropertyScaling.Apply(shield, _propertyTerms, SkillPropertyScaling.Read(context));
            soft.AddShieldFromSkill(SkillCombat.ScaleHealOrShield(context, shield));
        }
    }

    [Serializable]
    public class IncomingGuardLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _reduction;
        [SerializeField] SkillPropertyTerm[] _reductionTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _counterDamage;
        [SerializeField] SkillPropertyTerm[] _counterTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] bool _counterPhysical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int reduction = SkillPropertyScaling.Apply(_reduction, _reductionTerms, properties);
            int counter = SkillPropertyScaling.Apply(_counterDamage, _counterTerms, properties);
            character?.Modifiers.ArmIncomingReduction(reduction, 1, counter, _counterPhysical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            int reduction = SkillPropertyScaling.Apply(_reduction, _reductionTerms, properties);
            if (reduction > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Następny atak: -{reduction} obrażeń"));

            int counter = SkillPropertyScaling.Apply(_counterDamage, _counterTerms, properties);
            if (counter > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Kontratak: {counter}"));
        }
    }

    [Serializable]
    public class OutgoingHitBonusLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _bonus = 3;
        [SerializeField] SkillPropertyTerm[] _bonusTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _hits = 3;
        [SerializeField] SkillPropertyTerm[] _hitTerms = Array.Empty<SkillPropertyTerm>();

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int bonus = SkillPropertyScaling.Apply(_bonus, _bonusTerms, properties);
            int hits = SkillPropertyScaling.Apply(_hits, _hitTerms, properties);
            character?.Modifiers.ArmOutgoingHitBonus(bonus, hits);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            int bonus = SkillPropertyScaling.Apply(_bonus, _bonusTerms, properties);
            int hits = Mathf.Max(0, SkillPropertyScaling.Apply(_hits, _hitTerms, properties));
            if (bonus <= 0 || hits <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Damage, $"+{bonus} obrażeń przy {hits} kolejnych trafieniach"));
        }
    }

    [Serializable]
    public class UnyieldingLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _shield = 12;
        [SerializeField] SkillPropertyTerm[] _shieldTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _bonusPercent = 25;
        [SerializeField] SkillPropertyTerm[] _percentTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _lowHealthPercent = 30;
        [SerializeField] int _actionPointsRestored = 1;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target) ?? context.Caster;
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int shield = SkillPropertyScaling.Apply(_shield, _shieldTerms, properties);
            soft.AddShieldFromSkill(SkillCombat.ScaleHealOrShield(context, shield));

            int percent = Mathf.Max(0, SkillPropertyScaling.Apply(_bonusPercent, _percentTerms, properties));
            character.Modifiers.ArmNextAttackBonus(1f + percent / 100f);

            if (_lowHealthPercent <= 0 || soft.MaxHP <= 0)
                return;

            if (soft.CurrentHealth * 100 < soft.MaxHP * _lowHealthPercent)
                soft.AddActionPoints(Mathf.Max(0, _actionPointsRestored));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            int shield = SkillCombat.ScaleHealOrShield(
                preview,
                SkillPropertyScaling.Apply(_shield, _shieldTerms, properties));
            if (shield > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Shield, $"Daje {shield} tarczy"));

            int percent = Mathf.Max(0, SkillPropertyScaling.Apply(_bonusPercent, _percentTerms, properties));
            if (percent > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Następny atak: +{percent}%"));
        }
    }

    [Serializable]
    public class ConvergenceLogic : BattleEffectLogic
    {
        [SerializeField] int _damage = 15;
        [SerializeField] SkillPropertyTerm[] _damageTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _heal = 6;
        [SerializeField] SkillPropertyTerm[] _healTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _shield = 6;
        [SerializeField] SkillPropertyTerm[] _shieldTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _bonusColorCount = 4;
        [SerializeField] float _bonusMultiplier = 1.5f;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(context);
            float bonus = SkillCombat.LastResolveColorCount() >= _bonusColorCount ? _bonusMultiplier : 1f;
            int damage = Scale(bonus, SkillPropertyScaling.Apply(_damage, _damageTerms, properties));
            int heal = Scale(bonus, SkillPropertyScaling.Apply(_heal, _healTerms, properties));
            int shield = Scale(bonus, SkillPropertyScaling.Apply(_shield, _shieldTerms, properties));

            SkillCombat.DealScaledDamage(context, target, damage, physical: false);

            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            if (casterSoft == null)
                return;

            casterSoft.HealFromSkill(SkillCombat.ScaleHealOrShield(context, heal));
            casterSoft.AddShieldFromSkill(SkillCombat.ScaleHealOrShield(context, shield));
        }

        static int Scale(float bonus, int amount)
        {
            if (amount <= 0)
                return 0;

            if (bonus <= 1f)
                return amount;

            return Mathf.FloorToInt(amount * bonus);
        }
    }

    [Serializable]
    public class TriggerStatusTickLogic : BattleEffectLogic
    {
        [SerializeField] StatusEffectDefinition _status;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            context.Resolve(target)?.TriggerStatusTick(_status);
        }
    }

    [Serializable]
    public class MultiHitDamageLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _hits = 2;
        [SerializeField] SkillPropertyTerm[] _hitTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _damagePerHit = 4;
        [SerializeField] SkillPropertyTerm[] _damageTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] bool _physical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int hits = SkillPropertyScaling.Apply(_hits, _hitTerms, properties);
            int damage = SkillPropertyScaling.Apply(_damagePerHit, _damageTerms, properties);
            SkillCombat.DealScaledHits(context, target, hits, damage, _physical);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            int hits = Mathf.Max(0, SkillPropertyScaling.Apply(_hits, _hitTerms, properties));
            int perHit = SkillCombat.ScaleAmount(
                preview,
                SkillPropertyScaling.Apply(_damagePerHit, _damageTerms, properties),
                _physical,
                includeAttackBonus: true);
            if (perHit <= 0 || hits <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Zadaje {perHit} obrażeń × {hits}"));
        }
    }

    [Serializable]
    public class DrawCardLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] int _cards = 1;
        [SerializeField] int _bonusCards;
        [SerializeField] int _bonusIfGroupsAtLeast;
        [SerializeField] int _bonusIfPropertyAtLeast;
        [SerializeField] PlayerProperty _bonusProperty;
        [SerializeField] int _propertyBonusCards = 1;
        [SerializeField] int _maxCards;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            CardPlayController cards = BattleManager.Instance != null ? BattleManager.Instance.CardPlay : null;
            if (cards == null)
                return;

            cards.DrawCards(DrawCount(context));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int draw = DrawCount(preview);
            if (draw <= 0 && _cards <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Draw, $"Dobiera {Mathf.Max(1, draw)} kart"));
        }

        int DrawCount(BattleEffectContext context)
        {
            int draw = Mathf.Max(0, _cards);
            if (_bonusIfGroupsAtLeast > 0 && SkillCombat.LastResolveGroupCount() >= _bonusIfGroupsAtLeast)
                draw += Mathf.Max(0, _bonusCards);

            if (_bonusIfPropertyAtLeast > 0
                && SkillPropertyScaling.Read(context).Get(_bonusProperty) >= _bonusIfPropertyAtLeast)
                draw += Mathf.Max(0, _propertyBonusCards);

            if (_maxCards > 0)
                draw = Mathf.Min(_maxCards, draw);

            return draw;
        }
    }

    [Serializable]
    public class ReturnDiscardCardLogic : BattleEffectLogic, ISkillCastRequirement
    {
        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            BattleDeck deck = BattleManager.Instance != null ? BattleManager.Instance.CardPlay?.Deck : null;
            return deck != null && deck.DiscardPileCount > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleDeck deck = BattleManager.Instance != null ? BattleManager.Instance.CardPlay?.Deck : null;
            if (deck != null && deck.TryReturnFromDiscardToHand(context.ChoicePrimary))
                Sfx.Play(SfxId.CardRecover);
        }
    }

    [Serializable]
    public class TransmuteManaLogic : BattleEffectLogic, ISkillCastRequirement
    {
        [SerializeField] int _maxAmount = 3;
        [SerializeField] int _cap;
        [SerializeField] SkillPropertyTerm[] _amountTerms = Array.Empty<SkillPropertyTerm>();

        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            SoftStats soft = caster?.Stats?.Soft;
            if (soft == null)
                return false;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return false;

            for (int i = 0; i < config.TileTypeCount; i++)
            {
                Match3TileTypeDefinition tileType = config.GetTileType(i);
                int cost = skill != null ? skill.GetManaCostForTileType(tileType) : 0;
                if (soft.GetManaForTileType(i) - cost > 0)
                    return true;
            }

            return false;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Caster?.Stats?.Soft;
            if (soft == null)
                return;

            int fromId = context.ChoicePrimary;
            int toId = context.ChoiceSecondary;
            if (fromId == toId || fromId < 0 || toId < 0)
                return;

            int limit = SkillPropertyScaling.Apply(_maxAmount, _amountTerms, SkillPropertyScaling.Read(context));
            if (_cap > 0)
                limit = Mathf.Min(_cap, limit);

            int moved = Mathf.Min(Mathf.Max(0, limit), soft.GetManaForTileType(fromId));
            if (moved <= 0)
                return;

            soft.SetManaForTileType(fromId, soft.GetManaForTileType(fromId) - moved);
            soft.SetManaForTileType(toId, soft.GetManaForTileType(toId) + moved);
            Sfx.Play(SfxId.ManaTransmute);
        }
    }

    [Serializable]
    public class SacrificeHealthForApLogic : BattleEffectLogic
    {
        [SerializeField] int _healthLost = 5;
        [SerializeField] SkillPropertyTerm[] _healthTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _actionPointsGained = 2;
        [SerializeField] SkillPropertyTerm[] _actionPointTerms = Array.Empty<SkillPropertyTerm>();

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target) ?? context.Caster;
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int healthLost = SkillPropertyScaling.Apply(_healthLost, _healthTerms, properties);
            int actionPoints = SkillPropertyScaling.Apply(_actionPointsGained, _actionPointTerms, properties);
            soft.LoseHealthIgnoringShield(healthLost);
            soft.AddActionPoints(actionPoints);
            Sfx.Play(SfxId.HpSacrifice);
        }
    }

    [Serializable]
    public class AddSoulsFromCardClearsLogic : BattleEffectLogic
    {
        [SerializeField] int _maxSouls = 6;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Caster?.Stats?.Soft;
            if (soft == null)
                return;

            int souls = SkillCombat.LastResolveTilesClearedByCards();
            if (_maxSouls > 0)
                souls = Mathf.Min(_maxSouls, souls);

            soft.AddSouls(souls);
            if (souls > 0)
                Sfx.Play(SfxId.SoulCollect);
        }
    }

    [Serializable]
    public class SpendSoulsHitsLogic : BattleEffectLogic, ISkillCastRequirement
    {
        [SerializeField] int _maxSouls = 4;
        [SerializeField] int _damagePerSoul = 4;
        [SerializeField] bool _physical = true;

        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            return caster?.Stats?.Soft != null && caster.Stats.Soft.CurrentSouls > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Caster?.Stats?.Soft;
            if (soft == null)
                return;

            int spent = _maxSouls > 0 ? soft.ConsumeSouls(_maxSouls) : soft.ConsumeSouls(int.MaxValue);
            SkillCombat.DealScaledHits(context, target, spent, _damagePerSoul, _physical);
        }
    }

    [Serializable]
    public class SpendSoulsHealLogic : BattleEffectLogic, ISkillCastRequirement, ITooltipPreview
    {
        [SerializeField] int _healPerSoul = 2;

        public bool CanCast(BattleCharacter caster, BattleCharacter target, SkillDefinition skill)
        {
            return caster?.Stats?.Soft != null && caster.Stats.Soft.CurrentSouls > 0;
        }

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats soft = context.Caster?.Stats?.Soft;
            if (soft == null || _healPerSoul <= 0)
                return;

            int spent = soft.ConsumeSouls(int.MaxValue);
            int heal = spent * _healPerSoul;
            if (heal <= 0)
                return;

            soft.HealFromSkill(SkillCombat.ScaleHealOrShield(context, heal));
            Sfx.Play(SfxId.Healing);
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            int souls = preview.Caster?.Stats?.Soft != null ? preview.Caster.Stats.Soft.CurrentSouls : 0;
            int healPerSoul = Mathf.Max(0, _healPerSoul);
            if (souls > 0 && healPerSoul > 0)
            {
                int heal = SkillCombat.ScaleHealOrShield(preview, souls * healPerSoul);
                if (heal > 0)
                    lines.Add(new TooltipLine(TooltipLineKind.Heal, $"Leczy {heal} ({souls} Dusz)"));
                return;
            }

            if (healPerSoul > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Heal, $"Leczy {healPerSoul} HP za każdą Duszę"));
        }
    }
}
