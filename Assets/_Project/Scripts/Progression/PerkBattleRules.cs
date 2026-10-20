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

        public void CopyFrom(SoftStatValues values)
        {
            KindleBurn = Mathf.Max(0, values.KindleBurn);
            EmberLife = values.EmberLife > 0;
            RenewingFlame = values.RenewingFlame > 0;
            Fury = values.Fury > 0;
            Tenacity = values.Tenacity > 0;
            Faith = values.Faith > 0;
            Sanctuary = values.Sanctuary > 0;
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

            if (soft == null)
                return;

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
