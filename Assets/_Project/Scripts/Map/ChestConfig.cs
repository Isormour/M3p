using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Loot paid out when a chest node is opened. Experience and coloured shards are both optional.
    /// </summary>
    [CreateAssetMenu(fileName = "ChestConfig", menuName = "M3P/Chest Config", order = 22)]
    public class ChestConfig : ScriptableObject
    {
        [Tooltip("Experience granted when this chest is taken.")]
        [Min(0), SerializeField] int _experience;

        [Tooltip("Shards granted when this chest is taken. Each entry is one colour.")]
        [SerializeField] TileTypeShardCost[] _shards = Array.Empty<TileTypeShardCost>();

        public int Experience => Mathf.Max(0, _experience);

        public IReadOnlyList<TileTypeShardCost> Shards => _shards ?? Array.Empty<TileTypeShardCost>();

        public bool HasRewards
        {
            get
            {
                if (Experience > 0)
                    return true;

                IReadOnlyList<TileTypeShardCost> shards = Shards;
                for (int i = 0; i < shards.Count; i++)
                {
                    if (shards[i].Amount > 0 && shards[i].TileType != null)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Popup copy listing every positive reward, or that the chest is empty.</summary>
        public string DescribeRewards()
        {
            List<string> parts = new List<string>();
            if (Experience > 0)
                parts.Add($"{Experience} EXP");

            IReadOnlyList<TileTypeShardCost> shards = Shards;
            for (int i = 0; i < shards.Count; i++)
            {
                TileTypeShardCost shard = shards[i];
                if (shard.Amount <= 0 || shard.TileType == null)
                    continue;

                parts.Add($"{shard.Amount} {shard.TileType.name} shards");
            }

            if (parts.Count == 0)
                return "The chest is empty.";

            return "You find " + string.Join(", ", parts) + ".";
        }
    }
}
