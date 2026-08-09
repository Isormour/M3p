using System;

namespace M3P
{
    /// <summary>
    /// Shards of one colour. Stored against the tile type's asset name rather than its runtime id, so
    /// reordering the tile types in <see cref="GameConfig"/> cannot silently repaint a player's wallet.
    /// </summary>
    [Serializable]
    public struct ShardAmount
    {
        public string TileType;
        public int Amount;

        public ShardAmount(string tileType, int amount)
        {
            TileType = tileType;
            Amount = amount;
        }
    }
}
