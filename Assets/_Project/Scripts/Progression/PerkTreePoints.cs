using System;

namespace M3P
{
    /// <summary>How many points a profile has put into one perk tree.</summary>
    [Serializable]
    public struct PerkTreePoints
    {
        public int TreeId;
        public int Value;

        public PerkTreePoints(int treeId, int value)
        {
            TreeId = treeId;
            Value = value;
        }
    }
}
