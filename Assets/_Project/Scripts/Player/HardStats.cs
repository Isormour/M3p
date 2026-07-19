using System;

namespace M3P
{
    [Serializable]
    public struct HardStats
    {
        public int Strength;
        public int Intelligence;
        public int Constitution;

        public HardStats(int strength, int intelligence, int constitution)
        {
            Strength = strength;
            Intelligence = intelligence;
            Constitution = constitution;
        }
    }
}
