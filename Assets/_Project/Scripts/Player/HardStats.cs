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
    }
}
