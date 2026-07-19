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
        public List<TileTypeMana> ManaByBrokenTileType = new List<TileTypeMana>();

        public event Action Changed;

        public static int CalculateMaxHP(HardStats hard) => Mathf.Max(1, hard.Constitution * 10);

        public SoftStats(HardStats hard)
        {
            MaxHP = CalculateMaxHP(hard);
            CurrentHealth = MaxHP;
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
            ResetMana();
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
