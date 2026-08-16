using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One craftable upgrade that can sit on an owned tile. Icon is for UI; logic is what fires
    /// when that copy is cleared.
    /// </summary>
    [CreateAssetMenu(fileName = "TileUpgrade", menuName = "M3P/Tile Upgrade", order = 25)]
    public class TileUpgradeDefinition : ScriptableObject
    {
        [SerializeField] string _displayName;
        [TextArea, SerializeField] string _description;
        [SerializeField] Sprite _icon;
        [Tooltip("Base shards spent to craft this upgrade onto an owned tile. The actual cost is this amount times (existing upgrades + 1).")]
        [SerializeField] TileTypeShardCost[] _craftCost = Array.Empty<TileTypeShardCost>();
        [SerializeField, SerializeReference] TileUpgradeLogic _logic;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public string Description => _description;

        public Sprite Icon => _icon;

        public TileTypeShardCost[] CraftCost => _craftCost ?? Array.Empty<TileTypeShardCost>();

        public TileUpgradeLogic Logic => _logic;

        /// <summary>First upgrade costs 1×, second 2×, third 3×, fourth 4×.</summary>
        public static int GetCraftCostMultiplier(int existingUpgradeCount)
        {
            return Mathf.Max(1, existingUpgradeCount + 1);
        }

        /// <summary>Base craft cost scaled for a tile that already has <paramref name="existingUpgradeCount"/> upgrades.</summary>
        public TileTypeShardCost[] GetCraftCost(int existingUpgradeCount)
        {
            return ScaleCraftCost(CraftCost, GetCraftCostMultiplier(existingUpgradeCount));
        }

        public static TileTypeShardCost[] ScaleCraftCost(IReadOnlyList<TileTypeShardCost> costs, int multiplier)
        {
            multiplier = Mathf.Max(1, multiplier);
            if (costs == null || costs.Count == 0)
                return Array.Empty<TileTypeShardCost>();

            var scaled = new TileTypeShardCost[costs.Count];
            for (int i = 0; i < costs.Count; i++)
            {
                TileTypeShardCost cost = costs[i];
                scaled[i] = new TileTypeShardCost(cost.TileType, cost.Amount * multiplier);
            }

            return scaled;
        }

        public int GetCraftCostForTileType(Match3TileTypeDefinition tileType)
        {
            return GetCraftCostForTileType(tileType, 0);
        }

        public int GetCraftCostForTileType(Match3TileTypeDefinition tileType, int existingUpgradeCount)
        {
            if (tileType == null)
                return 0;

            TileTypeShardCost[] costs = GetCraftCost(existingUpgradeCount);
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].TileType == tileType)
                    return costs[i].Amount;
            }

            return 0;
        }
    }
}
