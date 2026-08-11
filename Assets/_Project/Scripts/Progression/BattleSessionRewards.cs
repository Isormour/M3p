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

        /// <summary>Experience pending for a win, including the enemy's base payout and any in-battle bonuses.</summary>
        public int Experience { get; private set; }

        /// <summary>Shards earned so far, keyed by runtime tile type id.</summary>
        public IReadOnlyDictionary<int, int> ShardsByTileType => _shardsByTileType;

        public bool HasShards => _shardsByTileType.Count > 0;

        public bool HasRewards => Experience > 0 || HasShards;

        /// <summary>Clears every pending reward from the previous battle.</summary>
        public void Reset()
        {
            Experience = 0;
            _shardsByTileType.Clear();
        }

        /// <summary>Starts a new session and seeds it with what the chosen enemy pays on a win.</summary>
        public void Begin(EnemyDefinition enemy)
        {
            Reset();
            Experience = enemy != null ? System.Math.Max(0, enemy.ExperienceReward) : 0;
        }

        /// <summary>Adds bonus experience earned during the fight. Only committed after a win.</summary>
        public void AddExperience(int amount)
        {
            if (amount > 0)
                Experience += amount;
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
