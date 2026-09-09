namespace M3P
{
    /// <summary>
    /// One basic attack earned by a match and banked while the board was still cascading. The whole
    /// bank is thrown as a single flurry once the board settles, so a Resolve reads as one combo
    /// instead of a hit per cascade wave.
    /// </summary>
    public readonly struct PendingBasicAttack
    {
        public PendingBasicAttack(int typeId, int matchSize, int damage, int waveIndex, bool isLethal = false)
        {
            TypeId = typeId;
            MatchSize = matchSize;
            Damage = damage;
            WaveIndex = waveIndex;
            IsLethal = isLethal;
        }

        /// <summary>Tile type of the match that earned the attack. Colours the projectile.</summary>
        public int TypeId { get; }

        /// <summary>Length of that match, or of the wave's longest match for a cascade bonus hit.</summary>
        public int MatchSize { get; }

        public int Damage { get; }

        /// <summary>Cascade wave the match landed on, counting from 1.</summary>
        public int WaveIndex { get; }

        /// <summary>True when this is the swing that brought the target down.</summary>
        public bool IsLethal { get; }

        /// <summary>
        /// Copy tagged as lethal or not. Only known at launch time, once the damage has landed.
        /// </summary>
        public PendingBasicAttack WithLethal(bool isLethal)
        {
            return new PendingBasicAttack(TypeId, MatchSize, Damage, WaveIndex, isLethal);
        }
    }
}
