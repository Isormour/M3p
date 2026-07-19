using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class CharacterStats
    {
        public HardStats Hard;
        public SoftStats Soft;

        public CharacterStats(HardStats hard)
        {
            Hard = hard;
            Soft = new SoftStats(hard);
        }

        public int MaxHealth => Soft != null ? Soft.MaxHP : 1;

        public bool IsAlive => Soft != null && Soft.CurrentHealth > 0;

        public void RecalculateSoftStatsForBattle()
        {
            Soft?.RecalculateFromHard(Hard);
        }
    }
}
