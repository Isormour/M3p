using System;

namespace M3P
{
    /// <summary>
    /// One tile a profile owns. <see cref="TileId"/> is the type in <see cref="TileConfig"/>;
    /// <see cref="UpgradeIds"/> holds up to <see cref="MaxUpgradeCount"/> crafted upgrades.
    /// </summary>
    [Serializable]
    public struct OwnedTile
    {
        public const int MaxUpgradeCount = 4;

        public int TileId;
        public int[] UpgradeIds;

        public OwnedTile(int tileId, int[] upgradeIds = null)
        {
            TileId = tileId;
            UpgradeIds = ClampUpgrades(upgradeIds);
        }

        public int UpgradeCount => UpgradeIds != null ? UpgradeIds.Length : 0;

        public bool CanAcceptUpgrade => UpgradeCount < MaxUpgradeCount;

        public OwnedTile Clone()
        {
            return new OwnedTile(TileId, UpgradeIds);
        }

        /// <summary>Replaces a missing upgrade list so old saves and default structs stay usable.</summary>
        public OwnedTile Normalized()
        {
            return new OwnedTile(TileId, UpgradeIds);
        }

        public bool TryAddUpgrade(int upgradeId, out OwnedTile result)
        {
            result = this;
            if (upgradeId == TileUpgradeConfig.InvalidUpgradeId || !CanAcceptUpgrade)
                return false;

            int count = UpgradeCount;
            int[] next = new int[count + 1];
            if (count > 0)
                Array.Copy(UpgradeIds, next, count);

            next[count] = upgradeId;
            result = new OwnedTile(TileId, next);
            return true;
        }

        public OwnedTile WithUpgradeAt(int slot, int upgradeId)
        {
            int[] current = ClampUpgrades(UpgradeIds);
            if (slot < 0 || slot >= current.Length)
                return this;

            int[] next = new int[current.Length];
            Array.Copy(current, next, current.Length);
            next[slot] = upgradeId;
            return new OwnedTile(TileId, next);
        }

        public OwnedTile WithoutUpgradeAt(int slot)
        {
            int[] current = ClampUpgrades(UpgradeIds);
            if (slot < 0 || slot >= current.Length)
                return this;

            if (current.Length == 1)
                return new OwnedTile(TileId, Array.Empty<int>());

            int[] next = new int[current.Length - 1];
            if (slot > 0)
                Array.Copy(current, 0, next, 0, slot);
            if (slot < current.Length - 1)
                Array.Copy(current, slot + 1, next, slot, current.Length - slot - 1);

            return new OwnedTile(TileId, next);
        }

        static int[] ClampUpgrades(int[] upgradeIds)
        {
            if (upgradeIds == null || upgradeIds.Length == 0)
                return Array.Empty<int>();

            int count = Math.Min(upgradeIds.Length, MaxUpgradeCount);
            int[] copy = new int[count];
            Array.Copy(upgradeIds, copy, count);
            return copy;
        }
    }
}
