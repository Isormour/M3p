namespace M3P
{
    /// <summary>Flat bonuses from every talent the profile has unlocked.</summary>
    public struct TalentBonuses
    {
        public static readonly TalentBonuses None = default;

        public int MaxHp;
        public int MaxActionPoints;
        public int MaxHandSize;
        public float PhysicalDamagePercent;
        public float MagicEffectPercent;

        public static TalentBonuses Combine(TalentBonuses first, TalentBonuses second)
        {
            return new TalentBonuses
            {
                MaxHp = first.MaxHp + second.MaxHp,
                MaxActionPoints = first.MaxActionPoints + second.MaxActionPoints,
                MaxHandSize = first.MaxHandSize + second.MaxHandSize,
                PhysicalDamagePercent = first.PhysicalDamagePercent + second.PhysicalDamagePercent,
                MagicEffectPercent = first.MagicEffectPercent + second.MagicEffectPercent,
            };
        }
    }
}
