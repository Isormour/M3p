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
        public List<TileTypeMana> ManaByBrokenTileType = new List<TileTypeMana>();

        public event Action Changed;

        public static int CalculateMaxHP(HardStats hard) => Mathf.Max(1, hard.Constitution * 10);

        public static int CalculateMaxActionPoints(HardStats hard) => Mathf.Max(0, hard.Agility);

        public SoftStats(HardStats hard)
        {
            MaxHP = CalculateMaxHP(hard);
            CurrentHealth = MaxHP;
            MaxActionPoints = CalculateMaxActionPoints(hard);
            CurrentActionPoints = MaxActionPoints;
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

        public void RecalculateFromHard(HardStats hard)
        {
            MaxHP = CalculateMaxHP(hard);
            CurrentHealth = MaxHP;
            ResetActionPoints(hard);
            ResetMana();
        }

        public void ResetActionPoints(HardStats hard)
        {
            MaxActionPoints = CalculateMaxActionPoints(hard);
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

        public SoftStats Clone(HardStats hard)
        {
            return new SoftStats(hard)
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
