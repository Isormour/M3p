namespace M3P
{
    /// <summary>One copy from the profile tile deck, ready for the board to spawn.</summary>
    public readonly struct TileSpawnSpec
    {
        public int TypeId { get; }
        public int[] UpgradeIds { get; }

        public TileSpawnSpec(int typeId, int[] upgradeIds)
        {
            TypeId = typeId;
            UpgradeIds = upgradeIds ?? System.Array.Empty<int>();
        }
    }
}
