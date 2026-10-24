using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Battle-only state for conditional perks unlocked on a perk tree.
    /// Permanent bonuses stay on <see cref="SoftStats"/>; this tracks once-per-turn and once-per-battle uses.
    /// </summary>
    public sealed class PerkBattleRules
    {
        public int KindleBurn;
        public bool EmberLife;
        public bool RenewingFlame;
        public bool Fury;
        public bool Tenacity;
        public bool Faith;
        public bool Sanctuary;
        public bool StrikeSeries;
        public bool Breakthrough;
        public bool Focus;
        public bool Inspiration;
        public bool Fortitude;
        public bool SecondWind;
        public bool Accuracy;
        public bool Sprint;

        bool _kindleUsed;
        bool _emberReady;
        bool _emberArmAfterSkill;
        bool _emberArmedThisTurn;
        bool _flameUsed;
        int _flameTurns;
        bool _tenacityUsed;
        bool _tenacityPending;
        bool _faithReady;
        bool _faithArmAfterSkill;
        bool _faithArmedThisTurn;
        bool _sanctuaryUsed;
        bool _sanctuaryPending;
        bool _breakthroughReady;
        bool _breakthroughUsed;
        bool _focusReady;
        bool _focusUsed;
        bool _inspirationReady;
        bool _inspirationUsed;
        bool _fortitudeReady;
        bool _fortitudeUsed;
        bool _secondWindUsed;
        int _secondWindTurns;
        bool _accuracyReady;
        bool _accuracyUsed;
        bool _sprintReady;
        bool _sprintUsed;
        bool _castActive;
        bool _castPhysical;
        int _castHits;

        public void CopyFrom(SoftStatValues values)
        {
            KindleBurn = Mathf.Max(0, values.KindleBurn);
            EmberLife = values.EmberLife > 0;
            RenewingFlame = values.RenewingFlame > 0;
            Fury = values.Fury > 0;
            Tenacity = values.Tenacity > 0;
            Faith = values.Faith > 0;
            Sanctuary = values.Sanctuary > 0;
            StrikeSeries = values.StrikeSeries > 0;
            Breakthrough = values.Breakthrough > 0;
            Focus = values.Focus > 0;
            Inspiration = values.Inspiration > 0;
            Fortitude = values.Fortitude > 0;
            SecondWind = values.SecondWind > 0;
            Accuracy = values.Accuracy > 0;
            Sprint = values.Sprint > 0;
            ResetBattle();
        }

        public void ResetBattle()
        {
            _kindleUsed = false;
            _emberReady = false;
            _emberArmAfterSkill = false;
            _emberArmedThisTurn = false;
            _flameUsed = false;
            _flameTurns = 0;
            _tenacityUsed = false;
            _tenacityPending = false;
            _faithReady = false;
            _faithArmAfterSkill = false;
            _faithArmedThisTurn = false;
            _sanctuaryUsed = false;
            _sanctuaryPending = false;
            _breakthroughReady = false;
            _breakthroughUsed = false;
            _focusReady = false;
            _focusUsed = false;
            _inspirationReady = false;
            _inspirationUsed = false;
            _fortitudeReady = false;
            _fortitudeUsed = false;
            _secondWindUsed = false;
            _secondWindTurns = 0;
            _accuracyReady = false;
            _accuracyUsed = false;
            _sprintReady = false;
            _sprintUsed = false;
            _castActive = false;
            _castPhysical = false;
            _castHits = 0;
        }

        public int PowerBonus(int currentHealth, int maxHp)
        {
            if (!Fury || maxHp <= 0 || currentHealth * 2 > maxHp)
                return 0;

            return 1;
        }

        public void OnPlayerTurnStarted(SoftStats soft)
        {
            _kindleUsed = false;
            _emberReady = false;
            _emberArmAfterSkill = false;
            _emberArmedThisTurn = false;
            _tenacityUsed = false;
            _faithReady = false;
            _faithArmAfterSkill = false;
            _faithArmedThisTurn = false;
            _sanctuaryUsed = false;
            _breakthroughUsed = false;
            _breakthroughReady = false;
            _focusUsed = false;
            _focusReady = false;
            _inspirationUsed = false;
            _inspirationReady = false;
            _accuracyUsed = false;
            _accuracyReady = false;
            _sprintUsed = false;
            _sprintReady = false;
            _fortitudeUsed = false;
            _fortitudeReady = false;
            _castActive = false;
            _castPhysical = false;
            _castHits = 0;

            if (soft == null)
                return;

            if (_secondWindTurns > 0)
            {
                _secondWindTurns--;
                soft.Heal(3);
            }

            if (_flameTurns > 0)
            {
                _flameTurns--;
                soft.Heal(3);
            }

            if (_tenacityPending)
            {
                _tenacityPending = false;
                soft.Heal(3);
            }

            if (_sanctuaryPending)
            {
                _sanctuaryPending = false;
                soft.AddShield(6);
            }
        }

        public void OnSkillHit(
            SoftStats attacker,
            BattleCharacter defender,
            bool physical,
            bool dealtDamage,
            bool defenderHadBurn)
        {
            if (!physical && EmberLife && defenderHadBurn && !_emberArmedThisTurn)
                _emberArmAfterSkill = true;

            if (!physical && dealtDamage && KindleBurn > 0 && !_kindleUsed && defender != null)
            {
                StatusEffectDefinition burn = BurnStatus();
                if (burn != null)
                {
                    defender.ApplyStatusStacks(burn, null, KindleBurn, 0, 0);
                    _kindleUsed = true;
                }
            }

            if (physical && Tenacity && !_tenacityUsed && attacker != null &&
                attacker.MaxHP > 0 && attacker.CurrentHealth * 2 <= attacker.MaxHP)
            {
                _tenacityPending = true;
                _tenacityUsed = true;
            }
        }

        public int ModifySkillHeal(int amount)
        {
            if (!_emberReady || amount <= 0)
                return amount;

            _emberReady = false;
            return amount + 2;
        }

        public int ModifySkillShield(int amount)
        {
            if (!_faithReady || amount <= 0)
                return amount;

            _faithReady = false;
            return amount + 3;
        }

        public void OnSoulsSpent(int spent)
        {
            if (spent <= 0)
                return;

            if (Faith && !_faithArmedThisTurn)
                _faithArmAfterSkill = true;

            if (Sanctuary && !_sanctuaryUsed && spent >= 3)
            {
                _sanctuaryPending = true;
                _sanctuaryUsed = true;
            }
        }

        public void OnBurnStacksConsumed(int stacks)
        {
            if (!RenewingFlame || _flameUsed || stacks < 3)
                return;

            _flameUsed = true;
            _flameTurns = 3;
        }

        public void OnSkillFinished()
        {
            if (_emberArmAfterSkill)
            {
                _emberArmAfterSkill = false;
                _emberReady = true;
                _emberArmedThisTurn = true;
            }

            if (_faithArmAfterSkill)
            {
                _faithArmAfterSkill = false;
                _faithReady = true;
                _faithArmedThisTurn = true;
            }
        }

        /// <summary>
        /// Grants sequence statuses after Resolve settles. A match of 7 or more can grant both Focus and Inspiration.
        /// </summary>
        public void OnSequenceResolved(ResolveReport report)
        {
            if (report == null)
                return;

            if (Breakthrough && !_breakthroughUsed && report.MatchGroupCount >= 3)
            {
                _breakthroughReady = true;
                _breakthroughUsed = true;
            }

            if (Focus && !_focusUsed && report.LargestMatch >= 5)
            {
                _focusReady = true;
                _focusUsed = true;
            }

            if (Inspiration && !_inspirationUsed && report.LargestMatch >= 7)
            {
                _inspirationReady = true;
                _inspirationUsed = true;
            }

            if (Accuracy && !_accuracyUsed && report.CardsInSequence == 1 && report.HasMatch)
            {
                _accuracyReady = true;
                _accuracyUsed = true;
            }

            if (Sprint && !_sprintUsed && report.CardsInSequence >= 3)
            {
                _sprintReady = true;
                _sprintUsed = true;
            }
        }

        /// <summary>First basic attack of a sequence with at least two match groups deals +2.</summary>
        public int BasicAttackOpeningBonus(ResolveReport report)
        {
            if (!StrikeSeries || report == null || report.MatchGroupCount < 2)
                return 0;

            return 2;
        }

        public void OnSkillCastStarted(bool physical)
        {
            _castActive = true;
            _castPhysical = physical;
            _castHits = 0;
        }

        /// <summary>Flat bonus for the first direct hit of the skill currently being cast.</summary>
        public int ModifyOutgoingSkillHit()
        {
            if (!_castActive)
                return 0;

            _castHits++;
            if (_castHits != 1)
                return 0;

            int bonus = 0;
            if (_castPhysical && _breakthroughReady)
                bonus += 4;

            if (_castPhysical && _accuracyReady)
            {
                bonus += 2;
                _accuracyReady = false;
            }

            if (!_castPhysical && _focusReady)
            {
                bonus += 3;
                _focusReady = false;
            }

            return bonus;
        }

        /// <summary>
        /// Ends the skill cast. A physical skill spends Breakthrough. A magical skill spends Inspiration
        /// and reports that a card should be drawn.
        /// </summary>
        public bool OnSkillCastFinished(bool physical)
        {
            _castActive = false;
            _castPhysical = false;
            _castHits = 0;

            if (physical)
            {
                _breakthroughReady = false;
                return false;
            }

            if (!_inspirationReady)
                return false;

            _inspirationReady = false;
            return true;
        }

        public int PreviewCardCost(int cost)
        {
            if (cost <= 0 || !_sprintReady)
                return Mathf.Max(0, cost);

            return Mathf.Max(0, cost - 1);
        }

        public void ConsumeCardCostDiscount(int baseCost)
        {
            if (baseCost <= 0 || !_sprintReady)
                return;

            _sprintReady = false;
        }

        public void RestoreCardCostDiscount()
        {
            if (Sprint)
                _sprintReady = true;
        }

        /// <summary>Next direct hit deals 3 less damage. The hit spends the status.</summary>
        public int ApplyIncomingHitReduction(int amount)
        {
            if (!_fortitudeReady || amount <= 0)
                return Mathf.Max(0, amount);

            _fortitudeReady = false;
            return Mathf.Max(0, amount - 3);
        }

        public void OnDamageTaken(SoftStats self, int healthBefore)
        {
            if (self == null || self.MaxHP <= 0 || self.CurrentHealth <= 0)
                return;

            if (Fortitude && !_fortitudeUsed && self.CurrentHealth * 2 <= self.MaxHP)
            {
                _fortitudeReady = true;
                _fortitudeUsed = true;
            }

            if (SecondWind && !_secondWindUsed && healthBefore * 4 > self.MaxHP && self.CurrentHealth * 4 <= self.MaxHP)
            {
                _secondWindUsed = true;
                _secondWindTurns = 3;
            }
        }

        public static bool HasBurn(BattleCharacter character)
        {
            if (character == null)
                return false;

            var statuses = character.Statuses;
            for (int i = 0; i < statuses.Count; i++)
            {
                StatusEffectDefinition definition = statuses[i]?.Definition;
                if (definition != null && definition.StatusType == EStatusType.Burn && statuses[i].Stacks > 0)
                    return true;
            }

            return false;
        }

        static StatusEffectDefinition BurnStatus()
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            return config != null && config.StatProgression != null
                ? config.StatProgression.BurnStatus
                : null;
        }
    }
}
