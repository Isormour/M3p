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
        public int MaxActionPoints;
        public int CurrentActionPoints;
        public int MaxHandSize;
        public List<TileTypeMana> ManaByBrokenTileType = new List<TileTypeMana>();

        public event Action Changed;

        public SoftStats(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxHP = progression.CalculateMaxHp(hard, talents);
            CurrentHealth = MaxHP;
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

        public void RecalculateFromHard(HardStats hard, StatProgressionConfig progression, TalentBonuses talents = default)
        {
            progression ??= StatProgressionConfig.CreateDefault();
            MaxHP = progression.CalculateMaxHp(hard, talents);
            CurrentHealth = MaxHP;
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
