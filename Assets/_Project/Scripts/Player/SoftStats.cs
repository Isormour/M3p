using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public struct TileTypeMana
    {
        public int TileTypeId;
        public int Amount;

        public TileTypeMana(int tileTypeId, int amount = 0)
        {
            TileTypeId = tileTypeId;
            Amount = amount;
        }
    }

    [Serializable]
    public class SoftStats
    {
        public int BasicAttackDamage;
        public int MaxHP;
        public int CurrentHealth;
        public int CurrentShield;

        /// <summary>Damage buffer stolen and spent by frenzy. Absorbed after flat reduction and before Shield.</summary>
        public int CurrentArmor;

        public int MaxActionPoints;
        public int CurrentActionPoints;
        public int MaxHandSize;

        public int Power;
        public int Arcane;
        public int Guard;
        public int Recovery;
        public int Precision;
        public int Tempo;
        public int CurrentSouls;
        public List<TileTypeMana> ManaByBrokenTileType = new List<TileTypeMana>();
        public PerkBattleRules PerkRules = new PerkBattleRules();

        public event Action Changed;

        /// <summary>Health, armor and shield actually lost by the last <see cref="TakeDamage"/>.</summary>
        public event Action<int> Damaged;

        /// <summary>Health actually gained by the last <see cref="Heal"/>.</summary>
        public event Action<int> Healed;

        public SoftStats(
            HardStats hard,
            StatProgressionConfig progression,
            TalentBonuses talents = default,
            IReadOnlyList<PerkTreePoints> treePoints = null)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            SoftStatValues calculated = treePoints != null
                ? progression.CalculateSoftStats(treePoints, talents)
                : progression.CalculateSoftStats(hard, talents);
            BasicAttackDamage = calculated.BasicAttackDamage;
            MaxHP = calculated.MaxHP;
            CurrentHealth = MaxHP;
            CurrentShield = 0;
            CurrentArmor = 0;
            CurrentSouls = Mathf.Max(0, calculated.StartingSouls);
            MaxActionPoints = calculated.MaxActionPoints;
            CurrentActionPoints = MaxActionPoints;
            MaxHandSize = calculated.MaxHandSize;
            CopyCombatProperties(calculated);
            PerkRules ??= new PerkBattleRules();
            PerkRules.CopyFrom(calculated);
        }

        /// <summary>Capacities and combat stats a skill formula reads. Current health is not one of them.</summary>
        public PlayerProperties Properties => new PlayerProperties
        {
            Health = MaxHP,
            ActionPoints = MaxActionPoints,
            HandSize = MaxHandSize,
            Power = Power + (PerkRules != null ? PerkRules.PowerBonus(CurrentHealth, MaxHP) : 0),
            Arcane = Arcane,
            Guard = Guard,
            Recovery = Recovery,
            Precision = Precision,
            Tempo = Tempo,
        };

        void CopyCombatProperties(SoftStatValues calculated)
        {
            Power = calculated.Power;
            Arcane = calculated.Arcane;
            Guard = calculated.Guard;
            Recovery = calculated.Recovery;
            Precision = calculated.Precision;
            Tempo = calculated.Tempo;
        }

        public int GetManaForTileType(int tileTypeId)
        {
            for (int i = 0; i < ManaByBrokenTileType.Count; i++)
            {
                if (ManaByBrokenTileType[i].TileTypeId == tileTypeId)
                    return ManaByBrokenTileType[i].Amount;
            }

            return 0;
        }

        public void SetManaForTileType(int tileTypeId, int amount)
        {
            amount = Math.Max(0, amount);

            for (int i = 0; i < ManaByBrokenTileType.Count; i++)
            {
                if (ManaByBrokenTileType[i].TileTypeId != tileTypeId)
                    continue;

                TileTypeMana entry = ManaByBrokenTileType[i];
                entry.Amount = amount;
                ManaByBrokenTileType[i] = entry;
                NotifyChanged();
                return;
            }

            ManaByBrokenTileType.Add(new TileTypeMana(tileTypeId, amount));
            NotifyChanged();
        }

        public void AddManaFromBrokenTiles(int tileTypeId, int amount)
        {
            if (amount <= 0)
                return;

            SetManaForTileType(tileTypeId, GetManaForTileType(tileTypeId) + amount);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;

            int healthBefore = CurrentHealth;
            int armorBefore = CurrentArmor;
            int shieldBefore = CurrentShield;

            // Flat reduction is applied by the hit. Armor is spent before Shield.
            if (CurrentArmor > 0)
            {
                int absorbed = Math.Min(CurrentArmor, amount);
                CurrentArmor -= absorbed;
                amount -= absorbed;
            }

            if (CurrentShield > 0)
            {
                int absorbed = Math.Min(CurrentShield, amount);
                CurrentShield -= absorbed;
                amount -= absorbed;
            }

            if (amount > 0)
                CurrentHealth = Math.Max(0, CurrentHealth - amount);

            NotifyChanged();

            int dealt = (armorBefore - CurrentArmor)
                + (shieldBefore - CurrentShield)
                + (healthBefore - CurrentHealth);
            if (dealt > 0)
                Damaged?.Invoke(dealt);

            PerkRules?.OnDamageTaken(this, healthBefore);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            int healthBefore = CurrentHealth;
            CurrentHealth = Math.Min(MaxHP, CurrentHealth + amount);
            NotifyChanged();

            int healed = CurrentHealth - healthBefore;
            if (healed > 0)
                Healed?.Invoke(healed);
        }

        public void AddArmor(int amount)
        {
            if (amount <= 0)
                return;

            CurrentArmor += amount;
            NotifyChanged();
        }

        public void ClearArmor()
        {
            if (CurrentArmor <= 0)
                return;

            CurrentArmor = 0;
            NotifyChanged();
        }

        /// <summary>Removes up to <paramref name="amount"/> armor and returns how much was actually taken.</summary>
        public int RemoveArmor(int amount)
        {
            if (amount <= 0 || CurrentArmor <= 0)
                return 0;

            int removed = Math.Min(CurrentArmor, amount);
            CurrentArmor -= removed;
            NotifyChanged();
            return removed;
        }

        /// <summary>Shield, or armor marked with A when the character has armor.</summary>
        public string FormatDefenseText()
        {
            if (CurrentShield > 0 && CurrentArmor > 0)
                return $"{CurrentShield} {CurrentArmor}A";

            if (CurrentArmor > 0)
                return $"{CurrentArmor}A";

            return CurrentShield.ToString();
        }

        /// <summary>Changes when either shield or armor changes, so one indicator can show both.</summary>
        public int DefenseIndicatorValue => CurrentShield + CurrentArmor * 10000;

        public void AddShield(int amount)
        {
            if (amount <= 0)
                return;

            CurrentShield += amount;
            NotifyChanged();
        }

        public void ClearShield()
        {
            if (CurrentShield <= 0)
                return;

            CurrentShield = 0;
            NotifyChanged();
        }

        /// <summary>Removes up to <paramref name="amount"/> shield and returns how much was actually taken.</summary>
        public int RemoveShield(int amount)
        {
            if (amount <= 0 || CurrentShield <= 0)
                return 0;

            int removed = Math.Min(CurrentShield, amount);
            CurrentShield -= removed;
            NotifyChanged();
            return removed;
        }

        /// <summary>
        /// Direct HP loss that ignores Armor and Shield. Used by sacrifice skills. Never drops below
        /// <paramref name="minimumHealth"/>, so the skill cannot kill the bearer.
        /// </summary>
        public int LoseHealthIgnoringShield(int amount, int minimumHealth = 1)
        {
            if (amount <= 0)
                return 0;

            int floor = Mathf.Max(0, minimumHealth);
            int lost = Mathf.Min(amount, Mathf.Max(0, CurrentHealth - floor));
            if (lost <= 0)
                return 0;

            CurrentHealth -= lost;
            NotifyChanged();
            return lost;
        }

        public void AddSouls(int amount)
        {
            if (amount <= 0)
                return;

            CurrentSouls += amount;
            NotifyChanged();
        }

        /// <summary>Consumes up to <paramref name="amount"/> souls and returns how many were spent.</summary>
        public int ConsumeSouls(int amount)
        {
            if (amount <= 0 || CurrentSouls <= 0)
                return 0;

            int spent = Mathf.Min(amount, CurrentSouls);
            CurrentSouls -= spent;
            PerkRules?.OnSoulsSpent(spent);
            NotifyChanged();
            return spent;
        }

        public void ClearSouls()
        {
            if (CurrentSouls <= 0)
                return;

            CurrentSouls = 0;
            NotifyChanged();
        }

        /// <summary>Scales max and current HP after CON has already been applied. Elite fights use 1.25.</summary>
        public void ScaleMaxHealth(float multiplier)
        {
            if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f))
                return;

            MaxHP = Mathf.Max(1, Mathf.RoundToInt(MaxHP * multiplier));
            CurrentHealth = MaxHP;
            NotifyChanged();
        }

        /// <summary>Flat max HP on top of constitution perks and the encounter health multiplier.</summary>
        public void AddBonusMaxHealth(int amount)
        {
            if (amount == 0)
                return;

            MaxHP = Mathf.Max(1, MaxHP + amount);
            CurrentHealth = MaxHP;
            NotifyChanged();
        }

        public void RecalculateFromHard(
            HardStats hard,
            StatProgressionConfig progression,
            TalentBonuses talents = default,
            IReadOnlyList<PerkTreePoints> treePoints = null)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            SoftStatValues calculated = treePoints != null
                ? progression.CalculateSoftStats(treePoints, talents)
                : progression.CalculateSoftStats(hard, talents);
            BasicAttackDamage = calculated.BasicAttackDamage;
            MaxHP = calculated.MaxHP;
            CurrentHealth = MaxHP;
            CurrentShield = 0;
            CurrentArmor = 0;
            CurrentSouls = Mathf.Max(0, calculated.StartingSouls);
            MaxHandSize = calculated.MaxHandSize;
            MaxActionPoints = calculated.MaxActionPoints;
            CurrentActionPoints = MaxActionPoints;
            CopyCombatProperties(calculated);
            PerkRules ??= new PerkBattleRules();
            PerkRules.CopyFrom(calculated);
            ResetMana();
        }

        public void HealFromSkill(int amount)
        {
            if (PerkRules != null)
                amount = PerkRules.ModifySkillHeal(amount);

            Heal(amount);
        }

        public void AddShieldFromSkill(int amount)
        {
            if (PerkRules != null)
                amount = PerkRules.ModifySkillShield(amount);

            AddShield(amount);
        }

        public void NotifyPerkSkillHit(BattleCharacter defender, bool physical, bool dealtDamage, bool defenderHadBurn)
        {
            PerkRules?.OnSkillHit(this, defender, physical, dealtDamage, defenderHadBurn);
        }

        public void NotifyPerkSkillFinished()
        {
            PerkRules?.OnSkillFinished();
        }

        public void NotifyPerkTurnStarted()
        {
            PerkRules?.OnPlayerTurnStarted(this);
        }

        public void ResetActionPoints(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxActionPoints = progression.CalculateMaxActionPoints(hard, talents);
            CurrentActionPoints = MaxActionPoints;
            NotifyChanged();
        }

        public int PreviewCardActionPointCost(int cost)
        {
            if (cost <= 0)
                return 0;

            return PerkRules != null ? PerkRules.PreviewCardCost(cost) : cost;
        }

        public void ConsumeCardCostDiscount(int baseCost)
        {
            PerkRules?.ConsumeCardCostDiscount(baseCost);
        }

        public void RestoreCardCostDiscount()
        {
            PerkRules?.RestoreCardCostDiscount();
        }

        public bool HasActionPoints(int cost = 1) => CurrentActionPoints >= cost;

        public bool TrySpendActionPoint(int cost = 1)
        {
            if (cost <= 0)
                return true;

            if (CurrentActionPoints < cost)
                return false;

            CurrentActionPoints -= cost;
            NotifyChanged();
            return true;
        }

        public void AddActionPoints(int amount)
        {
            if (amount <= 0)
                return;

            CurrentActionPoints += amount;
            NotifyChanged();
        }

        /// <summary>Removes up to <paramref name="amount"/> action points and returns how many were lost.</summary>
        public int RemoveActionPoints(int amount)
        {
            if (amount <= 0 || CurrentActionPoints <= 0)
                return 0;

            int removed = Math.Min(CurrentActionPoints, amount);
            CurrentActionPoints -= removed;
            NotifyChanged();
            return removed;
        }

        public void ResetMana()
        {
            ManaByBrokenTileType.Clear();
            NotifyChanged();
        }

        public SoftStats Clone(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            return new SoftStats(hard, progression, talents)
            {
                CurrentHealth = CurrentHealth,
                CurrentShield = CurrentShield,
                CurrentArmor = CurrentArmor,
                CurrentSouls = CurrentSouls,
                CurrentActionPoints = CurrentActionPoints,
                Power = Power,
                Arcane = Arcane,
                Guard = Guard,
                Recovery = Recovery,
                Precision = Precision,
                Tempo = Tempo,
                ManaByBrokenTileType = ManaByBrokenTileType != null
                    ? new List<TileTypeMana>(ManaByBrokenTileType)
                    : new List<TileTypeMana>(),
            };
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
