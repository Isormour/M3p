namespace M3P
{
    public enum MapNodeType
    {
        Start = 0,
        Battle = 1,
        Shop = 2,
        Chest = 3,
        Elite = 4,
        Forge = 5,
        Boss = 6
    }

    public static class MapNodeTypes
    {
        public static bool IsCombat(this MapNodeType type)
        {
            return type == MapNodeType.Battle || type == MapNodeType.Elite || type == MapNodeType.Boss;
        }

        public static bool IsService(this MapNodeType type)
        {
            return type == MapNodeType.Shop || type == MapNodeType.Forge;
        }

        public static bool IsRevisitable(this MapNodeType type)
        {
            return type == MapNodeType.Shop || type == MapNodeType.Forge;
        }

        public static bool IsMajorReward(this MapNodeType type)
        {
            return type == MapNodeType.Chest ||
                   type == MapNodeType.Shop ||
                   type == MapNodeType.Forge ||
                   type == MapNodeType.Elite;
        }

        public static string DisplayName(this MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Shop: return "Card Shop";
                case MapNodeType.Forge: return "Forge";
                case MapNodeType.Elite: return "Elite";
                case MapNodeType.Boss: return "Boss";
                default: return type.ToString();
            }
        }

        /// <summary>Title on the walk-to-node panel when the stop is not a fight.</summary>
        public static string PreviewTitle(this MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Shop: return "SHOP";
                case MapNodeType.Forge: return "FORGE";
                case MapNodeType.Chest: return "CHEST";
                case MapNodeType.Elite: return "ELITE";
                case MapNodeType.Boss: return "BOSS";
                case MapNodeType.Start: return "START";
                default: return "BATTLE";
            }
        }

        /// <summary>Body copy when an encounter has no authored description.</summary>
        public static string DefaultDescription(this MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Forge:
                    return "Forge. Place a tile on the anvil and enhance it with upgrades.";
                case MapNodeType.Shop:
                    return "Card shop. Spend shards to forge a new card for your deck.";
                case MapNodeType.Chest:
                    return "A locked chest. A reward awaits inside.";
                case MapNodeType.Start:
                    return "The start of your journey.";
                case MapNodeType.Elite:
                    return "An elite enemy.";
                case MapNodeType.Boss:
                    return "The boss of this floor.";
                default:
                    return "A battle against an enemy.";
            }
        }
    }
}
