using System;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring-side shard cost. References the tile type asset directly so reordering
    /// <see cref="GameConfig.TileTypes"/> cannot silently remap a cost onto another colour.
    /// </summary>
    [Serializable]
    public struct TileTypeShardCost
    {
        public Match3TileTypeDefinition TileType;
        public int Amount;

        public TileTypeShardCost(Match3TileTypeDefinition tileType, int amount = 0)
        {
            TileType = tileType;
            Amount = amount;
        }
    }

    /// <summary>
    /// A card the player plays to reshape the board. Cards never deal damage directly — damage comes
    /// from the matches they set up and from skills paid for with the mana those matches produce.
    /// </summary>
    [CreateAssetMenu(fileName = "Card", menuName = "M3P/Board Action Card", order = 20)]
    public class BoardActionCardDefinition : ScriptableObject
    {
        [SerializeField] string _displayName = "Card";
        [TextArea, SerializeField] string _description;
        [SerializeField] Sprite _artwork;

        [Tooltip("UI prefab for this card in the hand. Root must have UIBoardActionCard.")]
        [SerializeField] UIBoardActionCard _cardPrefab;

        [Tooltip("Action points spent when this card is played.")]
        [Min(0), SerializeField] int _actionPointCost = 1;

        [Tooltip("Shards spent to craft a copy of this card. Each entry is one colour.")]
        [SerializeField] TileTypeShardCost[] _craftCost = Array.Empty<TileTypeShardCost>();

        [SerializeReference] BoardActionLogic _logic;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public string Description => _description;

        public Sprite Artwork => _artwork;

        public UIBoardActionCard CardPrefab => _cardPrefab;

        public int ActionPointCost => _actionPointCost;

        public TileTypeShardCost[] CraftCost => _craftCost ?? Array.Empty<TileTypeShardCost>();

        public BoardActionLogic Logic => _logic;

        public CardTargeting Targeting => _logic != null ? _logic.Targeting : CardTargeting.None;

        public int GetCraftCostForTileType(Match3TileTypeDefinition tileType)
        {
            if (tileType == null)
                return 0;

            TileTypeShardCost[] costs = CraftCost;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].TileType == tileType)
                    return costs[i].Amount;
            }

            return 0;
        }
    }
}
