using System;

namespace M3P
{
    [Serializable]
    public struct HardStats
    {
        public int Strength;
        public int Intelligence;
        public int Constitution;
        public int Agility;

        public HardStats(int strength, int intelligence, int constitution, int agility)
        {
            Strength = strength;
            Intelligence = intelligence;
            Constitution = constitution;
            Agility = agility;
        }

        public int Get(EStatType stat)
        {
            switch (stat)
            {
                case EStatType.Strength:
                    return Strength;
                case EStatType.Intelligence:
                    return Intelligence;
                case EStatType.Constitution:
                    return Constitution;
                case EStatType.Agility:
                    return Agility;
                default:
                    return 0;
            }
        }

        /// <summary>Returns a copy with <paramref name="points"/> added to a single stat.</summary>
        public HardStats WithPointsAdded(EStatType stat, int points = 1)
        {
            HardStats result = this;

            switch (stat)
            {
                case EStatType.Strength:
                    result.Strength += points;
                    break;
                case EStatType.Intelligence:
                    result.Intelligence += points;
                    break;
                case EStatType.Constitution:
                    result.Constitution += points;
                    break;
                case EStatType.Agility:
                    result.Agility += points;
                    break;
            }

            return result;
        }
    }
}
