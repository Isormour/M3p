using System.Collections.Generic;

namespace M3P
{
    /// <summary>
    /// What the current battle has earned but not yet banked. Rewards accumulate here so a defeat can
    /// forfeit them wholesale, which is what stops a losing fight from being worth farming.
    /// </summary>
    public sealed class BattleSessionRewards
    {
        readonly Dictionary<int, int> _shardsByTileType = new Dictionary<int, int>();

        /// <summary>Shards earned so far, keyed by runtime tile type id.</summary>
        public IReadOnlyDictionary<int, int> ShardsByTileType => _shardsByTileType;

        public bool HasShards => _shardsByTileType.Count > 0;

        public void Reset()
        {
            _shardsByTileType.Clear();
        }

        public void AddShards(int tileTypeId, int amount)
        {
            if (amount <= 0)
                return;

            _shardsByTileType[tileTypeId] = _shardsByTileType.TryGetValue(tileTypeId, out int current)
                ? current + amount
                : amount;
        }
    }
}
