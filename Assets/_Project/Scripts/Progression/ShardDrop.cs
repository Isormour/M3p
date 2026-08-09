using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Shards a single match just produced, and where on the board it happened. Raised while the battle
    /// is still running so effects can play at the match, even though the shards are only banked on a win.
    /// </summary>
    public readonly struct ShardDrop
    {
        public readonly Vector3 WorldPosition;
        public readonly int TileTypeId;
        public readonly int Amount;

        public ShardDrop(Vector3 worldPosition, int tileTypeId, int amount)
        {
            WorldPosition = worldPosition;
            TileTypeId = tileTypeId;
            Amount = amount;
        }
    }
}
