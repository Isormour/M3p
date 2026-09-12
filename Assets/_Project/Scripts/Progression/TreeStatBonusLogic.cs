using System;
using UnityEngine;

namespace M3P
{
    /// <summary>How invested points in a perk tree turn into hard stats.</summary>
    [Serializable]
    public abstract class TreeStatBonusLogic
    {
        public abstract void Apply(int treeValue, ref HardStats hard);

        /// <summary>Tree level implied by authored hard stats, used by enemies that have no perk trees.</summary>
        public abstract int GetProgressionLevel(HardStats hard);

        public virtual bool TryGetGrantedStat(out EStatType stat)
        {
            stat = default;
            return false;
        }
    }

    [Serializable]
    public sealed class AddHardStatBonus : TreeStatBonusLogic
    {
        [SerializeField] EStatType _stat;
        [Min(0), SerializeField] int _amountPerPoint = 1;

        public AddHardStatBonus() { }

        public AddHardStatBonus(EStatType stat, int amountPerPoint = 1)
        {
            _stat = stat;
            _amountPerPoint = Mathf.Max(0, amountPerPoint);
        }

        public EStatType Stat => _stat;

        public override void Apply(int treeValue, ref HardStats hard)
        {
            if (treeValue <= 0 || _amountPerPoint <= 0)
                return;

            hard = hard.WithPointsAdded(_stat, treeValue * _amountPerPoint);
        }

        public override int GetProgressionLevel(HardStats hard)
        {
            return hard.Get(_stat);
        }

        public override bool TryGetGrantedStat(out EStatType stat)
        {
            stat = _stat;
            return true;
        }
    }
}
