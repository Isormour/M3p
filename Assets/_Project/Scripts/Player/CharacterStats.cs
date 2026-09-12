using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class CharacterStats
    {
        StatProgressionConfig _progression;
        TalentBonuses _talentBonuses;
        IReadOnlyList<PerkTreePoints> _treePoints;

        public HardStats Hard;
        public SoftStats Soft;
        public TalentBonuses TalentBonuses => _talentBonuses;

        public CharacterStats(HardStats hard, StatProgressionConfig progression, TalentBonuses talentBonuses = default)
            : this(hard, progression, talentBonuses, null)
        {
        }

        public CharacterStats(
            HardStats hard,
            StatProgressionConfig progression,
            TalentBonuses talentBonuses,
            IReadOnlyList<PerkTreePoints> treePoints)
        {
            Hard = hard;
            _progression = progression ?? StatProgressionConfig.CreateDefault();
            _talentBonuses = talentBonuses;
            _treePoints = treePoints;
            Soft = new SoftStats(hard, _progression, _talentBonuses, _treePoints);
        }

        public int MaxHealth => Soft != null ? Soft.MaxHP : 1;

        public bool IsAlive => Soft != null && Soft.CurrentHealth > 0;

        public void RecalculateSoftStatsForBattle()
        {
            Soft?.RecalculateFromHard(Hard, _progression, _talentBonuses, _treePoints);
        }
    }
}
