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
        public int MaxHP;
        public int CurrentHealth;
        public int CurrentShield;
        public int MaxActionPoints;
        public int CurrentActionPoints;
        public int MaxHandSize;
        public int CurrentSouls;
        public List<TileTypeMana> ManaByBrokenTileType = new List<TileTypeMana>();

        public event Action Changed;

        public SoftStats(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxHP = progression.CalculateMaxHp(hard, talents);
            CurrentHealth = MaxHP;
            CurrentShield = 0;
            CurrentSouls = 0;
            MaxActionPoints = progression.CalculateMaxActionPoints(hard, talents);
            CurrentActionPoints = MaxActionPoints;
            MaxHandSize = progression.CalculateMaxHandSize(hard, talents);
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

            if (CurrentShield > 0)
            {
                int absorbed = Math.Min(CurrentShield, amount);
                CurrentShield -= absorbed;
                amount -= absorbed;
            }

            if (amount > 0)
                CurrentHealth = Math.Max(0, CurrentHealth - amount);

            NotifyChanged();
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            CurrentHealth = Math.Min(MaxHP, CurrentHealth + amount);
            NotifyChanged();
        }

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

        /// <summary>
        /// Direct HP loss that ignores Shield. Used by sacrifice skills. Never drops below
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

        public void RecalculateFromHard(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxHP = progression.CalculateMaxHp(hard, talents);
            CurrentHealth = MaxHP;
            CurrentShield = 0;
            CurrentSouls = 0;
            MaxHandSize = progression.CalculateMaxHandSize(hard, talents);
            ResetActionPoints(hard, progression, talents);
            ResetMana();
        }

        public void ResetActionPoints(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxActionPoints = progression.CalculateMaxActionPoints(hard, talents);
            CurrentActionPoints = MaxActionPoints;
            NotifyChanged();
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
                CurrentSouls = CurrentSouls,
                CurrentActionPoints = CurrentActionPoints,
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
