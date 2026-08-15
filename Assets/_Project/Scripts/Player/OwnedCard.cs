using System;

namespace M3P
{
    /// <summary>
    /// One card a profile owns. <see cref="CardId"/> is the definition in <see cref="CardConfig"/>;
    /// <see cref="UpgradeIds"/> is empty until that copy is upgraded.
    /// </summary>
    [Serializable]
    public struct OwnedCard
    {
        public int CardId;
        public int[] UpgradeIds;

        public OwnedCard(int cardId, int[] upgradeIds = null)
        {
            CardId = cardId;
            UpgradeIds = CloneUpgrades(upgradeIds);
        }

        public OwnedCard Clone()
        {
            return new OwnedCard(CardId, UpgradeIds);
        }

        /// <summary>Replaces a missing upgrade list so old saves and default structs stay usable.</summary>
        public OwnedCard Normalized()
        {
            if (UpgradeIds != null)
                return this;

            return new OwnedCard(CardId, Array.Empty<int>());
        }

        static int[] CloneUpgrades(int[] upgradeIds)
        {
            if (upgradeIds == null || upgradeIds.Length == 0)
                return Array.Empty<int>();

            int[] copy = new int[upgradeIds.Length];
            Array.Copy(upgradeIds, copy, upgradeIds.Length);
            return copy;
        }
    }
}
