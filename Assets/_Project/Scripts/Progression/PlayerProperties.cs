namespace M3P
{
    /// <summary>
    /// Values a skill formula can read. Perk trees grant them; a skill never reads a tree level.
    /// Health, action points and hand size are the character's capacities. The rest scale skill effects.
    /// </summary>
    public enum PlayerProperty
    {
        Health = 0,
        ActionPoints = 1,
        HandSize = 2,
        Power = 3,
        Arcane = 4,
        Guard = 5,
        Recovery = 6,
        Precision = 7,
        Tempo = 8,
    }

    /// <summary>
    /// One snapshot of every <see cref="PlayerProperty"/>. Built from base values plus unlocked perks.
    /// </summary>
    public struct PlayerProperties
    {
        public static readonly PlayerProperty[] All =
        {
            PlayerProperty.Health,
            PlayerProperty.ActionPoints,
            PlayerProperty.HandSize,
            PlayerProperty.Power,
            PlayerProperty.Arcane,
            PlayerProperty.Guard,
            PlayerProperty.Recovery,
            PlayerProperty.Precision,
            PlayerProperty.Tempo,
        };

        public int Health;
        public int ActionPoints;
        public int HandSize;
        public int Power;
        public int Arcane;
        public int Guard;
        public int Recovery;
        public int Precision;
        public int Tempo;

        public int Get(PlayerProperty property)
        {
            switch (property)
            {
                case PlayerProperty.Health:
                    return Health;
                case PlayerProperty.ActionPoints:
                    return ActionPoints;
                case PlayerProperty.HandSize:
                    return HandSize;
                case PlayerProperty.Power:
                    return Power;
                case PlayerProperty.Arcane:
                    return Arcane;
                case PlayerProperty.Guard:
                    return Guard;
                case PlayerProperty.Recovery:
                    return Recovery;
                case PlayerProperty.Precision:
                    return Precision;
                case PlayerProperty.Tempo:
                    return Tempo;
                default:
                    return 0;
            }
        }

        public void Add(PlayerProperty property, int amount)
        {
            if (amount == 0)
                return;

            switch (property)
            {
                case PlayerProperty.Health:
                    Health += amount;
                    break;
                case PlayerProperty.ActionPoints:
                    ActionPoints += amount;
                    break;
                case PlayerProperty.HandSize:
                    HandSize += amount;
                    break;
                case PlayerProperty.Power:
                    Power += amount;
                    break;
                case PlayerProperty.Arcane:
                    Arcane += amount;
                    break;
                case PlayerProperty.Guard:
                    Guard += amount;
                    break;
                case PlayerProperty.Recovery:
                    Recovery += amount;
                    break;
                case PlayerProperty.Precision:
                    Precision += amount;
                    break;
                case PlayerProperty.Tempo:
                    Tempo += amount;
                    break;
            }
        }

        public static string DisplayName(PlayerProperty property)
        {
            switch (property)
            {
                case PlayerProperty.Health:
                    return "Health";
                case PlayerProperty.ActionPoints:
                    return "Action Points";
                case PlayerProperty.HandSize:
                    return "Hand size";
                case PlayerProperty.Power:
                    return "Power";
                case PlayerProperty.Arcane:
                    return "Arcane";
                case PlayerProperty.Guard:
                    return "Guard";
                case PlayerProperty.Recovery:
                    return "Recovery";
                case PlayerProperty.Precision:
                    return "Precision";
                case PlayerProperty.Tempo:
                    return "Tempo";
                default:
                    return property.ToString();
            }
        }
    }
}
