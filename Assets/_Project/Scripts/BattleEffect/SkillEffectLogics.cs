using Match3;
using System;
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
    public class ResolveScaledDamageLogic : BattleEffectLogic
    {
        [SerializeField] ResolveDamageFormula _formula;
        [SerializeField] int _amount;
        [SerializeField] int _bonus;
        [SerializeField] int _perStep;
        [SerializeField] int _max;
        [SerializeField] int _threshold;
        [SerializeField] bool _physical;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            switch (_formula)
            {
                case ResolveDamageFormula.HitsPerGroup:
                    int hits = Mathf.Min(Mathf.Max(1, _max), SkillCombat.LastResolveGroupCount());
                    SkillCombat.DealScaledHits(context, target, hits, _amount, _physical);
                    return;
                case ResolveDamageFormula.CardsInSequence:
                case ResolveDamageFormula.CardsInSequencePrecision:
                    int cards = SkillCombat.LastResolveCardsInSequence();
                    int fromCards = Mathf.Min(_max > 0 ? _max : int.MaxValue, cards * Mathf.Max(0, _perStep));
                    if (_formula == ResolveDamageFormula.CardsInSequencePrecision
                        && SkillCombat.LastResolveIsSingleMatchWithoutCascade())
                        fromCards += Mathf.Max(0, _bonus);
                    SkillCombat.DealScaledDamage(context, target, fromCards, _physical);
                    return;
            }

            SkillCombat.DealScaledDamage(context, target, ResolveAmount(context, target), _physical);
        }

        int ResolveAmount(BattleEffectContext context, EEffectTarget target)
        {
            switch (_formula)
            {
                case ResolveDamageFormula.PrecisionBonus:
                    int precision = _amount;
                    if (SkillCombat.LastResolveIsSingleMatchWithoutCascade())
                        precision += Mathf.Max(0, _bonus);
                    return precision;

                case ResolveDamageFormula.PreviousResolves:
                    int frenzy = _amount + Mathf.Max(0, _perStep) * SkillCombat.PreviousResolveCountThisTurn();
                    return _max > 0 ? Mathf.Min(_max, frenzy) : frenzy;

                case ResolveDamageFormula.ExecuteBelowHealth:
                    BattleCharacter defender = context.Resolve(target);
                    SoftStats soft = defender?.Stats?.Soft;
                    if (soft != null && soft.MaxHP > 0 && soft.CurrentHealth * 100 < soft.MaxHP * Mathf.Max(0, _threshold))
                        return _bonus > 0 ? _bonus : _amount;
                    return _amount;

                case ResolveDamageFormula.CascadeWaves:
                    int chain = _amount + Mathf.Max(0, _perStep) * SkillCombat.LastResolveExtraWaves();
                    return _max > 0 ? Mathf.Min(_max, chain) : chain;

                case ResolveDamageFormula.ColorsInResolve:
                    int colors = SkillCombat.LastResolveColorCount() * Mathf.Max(0, _perStep);
                    return _max > 0 ? Mathf.Min(_max, colors) : colors;

                case ResolveDamageFormula.IntentConditional:
                    return SkillCombat.IsEnemyTelegraphingOffensive(context.Resolve(target))
                        ? (_bonus > 0 ? _bonus : _amount)
                        : _amount;

                default:
                    return _amount;
            }
        }
    }

    [Serializable]
    public class ConsumeShieldDamageLogic : BattleEffectLogic
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
    }

    [Serializable]
    public class ResolveScaledShieldLogic : BattleEffectLogic
    {
        [SerializeField] int _amount;
        [SerializeField] int _perGroup;
        [SerializeField] int _max;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            int shield = _amount + Mathf.Max(0, _perGroup) * SkillCombat.LastResolveGroupCount();
            if (_max > 0)
                shield = Mathf.Min(_max, shield);

            soft.AddShield(SkillCombat.ScaleHealOrShield(context, shield));
        }
    }

    [Serializable]
    public class IncomingGuardLogic : BattleEffectLogic
    {
        [SerializeField] int _reduction;
        [SerializeField] int _counterDamage;
        [SerializeField] bool _counterPhysical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            character?.Modifiers.ArmIncomingReduction(_reduction, 1, _counterDamage, _counterPhysical);
        }
    }

    [Serializable]
    public class OutgoingHitBonusLogic : BattleEffectLogic
    {
        [SerializeField] int _bonus = 3;
        [SerializeField] int _hits = 3;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target);
            character?.Modifiers.ArmOutgoingHitBonus(_bonus, _hits);
        }
    }

    [Serializable]
    public class UnyieldingLogic : BattleEffectLogic
    {
        [SerializeField] int _shield = 12;
        [SerializeField] float _nextPhysicalMultiplier = 1.5f;
        [SerializeField] int _lowHealthPercent = 30;
        [SerializeField] int _actionPointsRestored = 1;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target) ?? context.Caster;
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            soft.AddShield(SkillCombat.ScaleHealOrShield(context, _shield));
            character.Modifiers.ArmNextPhysicalSkillBonus(_nextPhysicalMultiplier);

            if (_lowHealthPercent <= 0 || soft.MaxHP <= 0)
                return;

            if (soft.CurrentHealth * 100 < soft.MaxHP * _lowHealthPercent)
                soft.AddActionPoints(Mathf.Max(0, _actionPointsRestored));
        }
    }

    [Serializable]
    public class ConvergenceLogic : BattleEffectLogic
    {
        [SerializeField] int _damage = 15;
        [SerializeField] int _heal = 6;
        [SerializeField] int _shield = 6;
        [SerializeField] int _bonusColorCount = 4;
        [SerializeField] float _bonusMultiplier = 1.5f;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            float bonus = SkillCombat.LastResolveColorCount() >= _bonusColorCount ? _bonusMultiplier : 1f;
            int damage = Scale(bonus, _damage);
            int heal = Scale(bonus, _heal);
            int shield = Scale(bonus, _shield);

            SkillCombat.DealScaledDamage(context, target, damage, physical: false);

            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            if (casterSoft == null)
                return;

            casterSoft.Heal(SkillCombat.ScaleHealOrShield(context, heal));
            casterSoft.AddShield(SkillCombat.ScaleHealOrShield(context, shield));
        }

        static int Scale(float bonus, int amount)
        {
            return Mathf.Max(0, Mathf.RoundToInt(amount * bonus));
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
    public class MultiHitDamageLogic : BattleEffectLogic
    {
        [SerializeField] int _hits = 2;
        [SerializeField] int _damagePerHit = 4;
        [SerializeField] bool _physical = true;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SkillCombat.DealScaledHits(context, target, _hits, _damagePerHit, _physical);
        }
    }

    [Serializable]
    public class DrawCardLogic : BattleEffectLogic
    {
        [SerializeField] int _cards = 1;
        [SerializeField] int _bonusCards;
        [SerializeField] int _bonusIfGroupsAtLeast;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            CardPlayController cards = BattleManager.Instance != null ? BattleManager.Instance.CardPlay : null;
            if (cards == null)
                return;

            int draw = Mathf.Max(0, _cards);
            if (_bonusIfGroupsAtLeast > 0 && SkillCombat.LastResolveGroupCount() >= _bonusIfGroupsAtLeast)
                draw += Mathf.Max(0, _bonusCards);

            cards.DrawCards(draw);
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
            deck?.TryReturnFromDiscardToHand(context.ChoicePrimary);
        }
    }

    [Serializable]
    public class TransmuteManaLogic : BattleEffectLogic, ISkillCastRequirement
    {
        [SerializeField] int _maxAmount = 3;

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

            int moved = Mathf.Min(Mathf.Max(1, _maxAmount), soft.GetManaForTileType(fromId));
            if (moved <= 0)
                return;

            soft.SetManaForTileType(fromId, soft.GetManaForTileType(fromId) - moved);
            soft.SetManaForTileType(toId, soft.GetManaForTileType(toId) + moved);
        }
    }

    [Serializable]
    public class SacrificeHealthForApLogic : BattleEffectLogic
    {
        [SerializeField] int _healthLost = 5;
        [SerializeField] int _actionPointsGained = 2;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            BattleCharacter character = context.Resolve(target) ?? context.Caster;
            SoftStats soft = character?.Stats?.Soft;
            if (soft == null)
                return;

            soft.LoseHealthIgnoringShield(_healthLost);
            soft.AddActionPoints(_actionPointsGained);
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

            int spent = soft.ConsumeSouls(_maxSouls);
            SkillCombat.DealScaledHits(context, target, spent, _damagePerSoul, _physical);
        }
    }
}
