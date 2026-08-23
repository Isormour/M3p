using System;
using UnityEngine;

namespace M3P
{
    /// <summary>Display rank that applies once the effective floor reaches <see cref="MinFloor"/>.</summary>
    [Serializable]
    public struct EnemyRankThreshold
    {
        [Min(1)] public int MinFloor;
        public string RankName;
    }
}
