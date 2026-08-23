using System;

namespace M3P
{
    /// <summary>Per-floor growth for one enemy archetype. Fractional values accumulate, then floor.</summary>
    [Serializable]
    public struct StatGrowthPerFloor
    {
        public float Strength;
        public float Intelligence;
        public float Constitution;
        public float Agility;
    }
}
