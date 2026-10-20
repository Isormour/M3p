using Match3;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Shared combat helpers used by player skill logics: last Resolve, scaled hits,
    /// incoming-guard consumption and counter-attacks.
    /// </summary>
    public static class SkillCombat
    {
        public static ResolveReport LastResolve =>
            BattleManager.Instance != null ? BattleManager.Instance.ActiveBoard?.LastResolveReport : null;

        public static TurnReport CurrentTurn =>
            BattleManager.Instance != null ? BattleManager.Instance.TurnReport : null;

        public static bool LastResolveIsSingleMatchWithoutCascade()
        {
            ResolveReport report = LastResolve;
            return report != null && report.MatchGroupCount == 1 && report.ExtraWaves <= 0;
        }

        public static int LastResolveGroupCount()
        {
            ResolveReport report = LastResolve;
            return report != null ? report.MatchGroupCount : 0;
        }

        public static int LastResolveExtraWaves()
        {
            ResolveReport report = LastResolve;
            return report != null ? report.ExtraWaves : 0;
        }

        public static int LastResolveColorCount()
        {
            ResolveReport report = LastResolve;
            return report != null ? report.MatchedTypeIds.Count : 0;
        }

        public static int LastResolveCardsInSequence()
        {
            ResolveReport report = LastResolve;
            return report != null ? report.CardsInSequence : 0;
        }

        public static int LastResolveTilesClearedByCards()
        {
            ResolveReport report = LastResolve;
            return report != null ? report.TilesClearedByCards : 0;
        }

        public static int PreviousResolveCountThisTurn()
        {
            TurnReport turn = CurrentTurn;
            return turn != null ? turn.ResolveCount : 0;
        }

        public static bool IsEnemyTelegraphingOffensive(BattleCharacter enemy)
        {
            return enemy is EnemyBattleCharacter enemyCharacter && enemyCharacter.IsTelegraphingOffensive;
        }

        public static int ScaleAmount(
            BattleEffectContext context,
            int amount,
            bool physical,
            bool includeAttackBonus = false,
            bool applyContextMultiplier = true)
        {
            if (amount <= 0)
                return 0;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            StatProgressionConfig progression = config != null
                ? config.StatProgression
                : StatProgressionConfig.CreateDefault();

            HardStats casterHard = context.Caster != null
                ? context.Caster.GetEffectiveHard()
                : default;
            TalentBonuses talents = context.Caster?.Stats?.TalentBonuses ?? TalentBonuses.None;
            int scaled = physical
                ? progression.ScalePhysicalEffect(casterHard, amount, talents)
                : progression.ScaleMagicEffect(casterHard, amount, talents);

            float multiplier = 1f;
            if (applyContextMultiplier)
                multiplier = context.SkillDamageMultiplier > 0f ? context.SkillDamageMultiplier : 1f;
            if (includeAttackBonus
                && context.DirectHit
                && context.Caster != null
                && Mathf.Approximately(multiplier, 1f))
                multiplier = context.Caster.Modifiers.PeekNextAttackBonus();

            if (!Mathf.Approximately(multiplier, 1f))
                scaled = Mathf.Max(1, Mathf.RoundToInt(scaled * multiplier));

            return scaled;
        }

        public static int ScaleHealOrShield(BattleEffectContext context, int amount)
        {
            return ScaleAmount(context, amount, physical: false, applyContextMultiplier: false);
        }

        /// <summary>
        /// Applies a skill hit: outgoing bonuses, incoming reduction from a guard status or
        /// combat modifier, then optional counter. Status ticks pass
        /// <see cref="BattleEffectContext.DirectHit"/> as false and skip those modifiers.
        /// </summary>
        public static void ApplyHit(BattleEffectContext context, BattleCharacter defender, int amount, bool physical)
        {
            if (defender?.Stats?.Soft == null)
                return;

            if (!context.DirectHit)
            {
                if (amount > 0)
                    defender.Stats.Soft.TakeDamage(amount);
                return;
            }

            if (defender.EvadesDirectHits())
                return;

            BattleCharacter attacker = context.Caster;
            if (attacker != null)
            {
                amount += attacker.Modifiers.ConsumeOutgoingHitBonus();
                amount += attacker.GetPersistentOutgoingBonus();
                amount = Mathf.Max(0, amount - attacker.GetPersistentOutgoingPenalty());
            }

            int reduction = 0;
            int counterDamage = 0;
            bool counterPhysical = false;
            if (!defender.TryConsumeIncomingGuard(out reduction, out counterDamage, out counterPhysical))
                defender.Modifiers.TryConsumeIncoming(out reduction, out counterDamage, out counterPhysical);
            amount = Mathf.Max(0, amount - reduction);
            amount = Mathf.Max(0, amount - defender.GetPersistentIncomingReduction());

            bool hadBurn = PerkBattleRules.HasBurn(defender);
            if (amount > 0)
                defender.Stats.Soft.TakeDamage(amount);

            if (attacker != null && attacker.IsPlayerControlled)
                attacker.Stats?.Soft?.NotifyPerkSkillHit(defender, physical, amount > 0, hadBurn);

            attacker?.GrantSoulsForOutgoingHit();
            defender.NotifyDirectHitReceived(attacker);

            if (counterDamage <= 0 || attacker?.Stats?.Soft == null || attacker == defender)
                return;

            int scaledCounter = ScaleAmount(
                new BattleEffectContext(defender, attacker, directHit: false),
                counterDamage,
                counterPhysical);
            if (scaledCounter > 0)
                attacker.Stats.Soft.TakeDamage(scaledCounter);
        }

        /// <summary>
        /// Lands one board attack. Applies lasting outgoing penalties and incoming reduction,
        /// then armor and shield inside <see cref="SoftStats.TakeDamage"/>.
        /// </summary>
        public static void ApplyBoardAttack(BattleCharacter attacker, BattleCharacter defender, int amount)
        {
            if (defender?.Stats?.Soft == null || defender.EvadesDirectHits())
                return;

            if (attacker != null)
                amount = Mathf.Max(0, amount - attacker.GetPersistentOutgoingPenalty());

            amount = Mathf.Max(0, amount - defender.GetPersistentIncomingReduction());
            if (amount > 0)
                defender.Stats.Soft.TakeDamage(amount);

            attacker?.GrantSoulsForOutgoingHit();
            defender.NotifyDirectHitReceived(attacker);
        }

        public static void DealScaledHits(
            BattleEffectContext context,
            EEffectTarget target,
            int hits,
            int damagePerHit,
            bool physical)
        {
            BattleCharacter defender = context.Resolve(target);
            if (defender == null || hits <= 0 || damagePerHit <= 0)
                return;

            int scaled = ScaleAmount(context, damagePerHit, physical, includeAttackBonus: true);
            for (int i = 0; i < hits; i++)
            {
                if (i > 0)
                    Sfx.Play(SfxId.MultiHitTick);
                ApplyHit(context, defender, scaled, physical);
            }
        }

        public static void DealScaledDamage(
            BattleEffectContext context,
            EEffectTarget target,
            int amount,
            bool physical)
        {
            if (amount <= 0)
                return;

            BattleCharacter defender = context.Resolve(target);
            ApplyHit(context, defender, ScaleAmount(context, amount, physical, includeAttackBonus: true), physical);
        }

        public static bool TryCollectDistinctColorSpendOrder(
            SoftStats softStats,
            SkillDefinition skill,
            List<int> destination,
            bool subtractAuthoredCosts = true)
        {
            destination.Clear();
            if (softStats == null || skill == null || skill.DistinctColorManaCost <= 0)
                return true;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return false;

            var remaining = new List<(int typeId, int amount)>();
            for (int i = 0; i < config.TileTypeCount; i++)
            {
                Match3TileTypeDefinition tileType = config.GetTileType(i);
                if (tileType == null)
                    continue;

                int have = softStats.GetManaForTileType(i);
                if (subtractAuthoredCosts)
                    have -= skill.GetManaCostForTileType(tileType);
                if (have >= skill.ManaPerDistinctColor)
                    remaining.Add((i, have));
            }

            remaining.Sort((a, b) => b.amount.CompareTo(a.amount));
            int needed = skill.DistinctColorManaCost;
            if (remaining.Count < needed)
                return false;

            for (int i = 0; i < needed; i++)
                destination.Add(remaining[i].typeId);

            return true;
        }
    }
}
