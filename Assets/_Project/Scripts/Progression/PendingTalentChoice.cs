using System;

namespace M3P
{
    /// <summary>A milestone the player reached but has not picked a talent for yet.</summary>
    [Serializable]
    public struct PendingTalentChoice
    {
        public EStatType Stat;
        public int MilestoneTier;

        public PendingTalentChoice(EStatType stat, int milestoneTier)
        {
            Stat = stat;
            MilestoneTier = milestoneTier;
        }

        public bool IsValid => MilestoneTier > 0;
    }
}
