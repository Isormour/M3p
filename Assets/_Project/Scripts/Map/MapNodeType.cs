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
    }
}
