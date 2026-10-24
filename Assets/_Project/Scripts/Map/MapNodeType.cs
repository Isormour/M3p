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
                case MapNodeType.Shop: return "SKLEP";
                case MapNodeType.Forge: return "KUŹNIA";
                case MapNodeType.Chest: return "SKRZYNIA";
                case MapNodeType.Elite: return "ELITA";
                case MapNodeType.Boss: return "BOSS";
                case MapNodeType.Start: return "START";
                default: return "WALKA";
            }
        }

        /// <summary>Body copy when an encounter has no authored description.</summary>
        public static string DefaultDescription(this MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Forge:
                    return "Kuźnia. Połóż kafelek na kowadle i wzmocnij go ulepszeniami.";
                case MapNodeType.Shop:
                    return "Sklep kart. Wydaj odłamki i wykuj nową kartę do talii.";
                case MapNodeType.Chest:
                    return "Zamknięta skrzynia. W środku czeka nagroda.";
                case MapNodeType.Start:
                    return "Początek drogi.";
                case MapNodeType.Elite:
                    return "Elitarny przeciwnik.";
                case MapNodeType.Boss:
                    return "Boss piętra.";
                default:
                    return "Walka z przeciwnikiem.";
            }
        }
    }
}
