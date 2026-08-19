using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Registry of every tile upgrade and the ids profiles store on owned copies. Ids are handed
    /// out once and never reused.
    /// </summary>
    [CreateAssetMenu(fileName = "TileUpgradeConfig", menuName = "M3P/Tile Upgrade Config", order = 26)]
    public class TileUpgradeConfig : ScriptableObject
    {
        public const int InvalidUpgradeId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans the upgrade in every profile that saved it.")]
            public int Id;
            public TileUpgradeDefinition Upgrade;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();
        [SerializeField, HideInInspector] int _nextUpgradeId = InvalidUpgradeId + 1;

        Dictionary<TileUpgradeDefinition, int> _idsByUpgrade;
        Dictionary<int, TileUpgradeDefinition> _upgradesById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        public int GetUpgradeId(TileUpgradeDefinition upgrade)
        {
            if (upgrade == null)
                return InvalidUpgradeId;

            EnsureLookups();
            return _idsByUpgrade.TryGetValue(upgrade, out int id) ? id : InvalidUpgradeId;
        }

        public bool TryGetUpgrade(int upgradeId, out TileUpgradeDefinition upgrade)
        {
            EnsureLookups();
            return _upgradesById.TryGetValue(upgradeId, out upgrade);
        }

        public TileUpgradeDefinition GetUpgrade(int upgradeId)
        {
            return TryGetUpgrade(upgradeId, out TileUpgradeDefinition upgrade) ? upgrade : null;
        }

        /// <summary>
        /// Lets upgrades such as Match Neighbor add extra tiles to a clear, using only the tiles
        /// that were already in the set as origins so the expansion does not chain.
        /// </summary>
        public void ExpandClears(Match3Board board, HashSet<Match3Tile> tiles)
        {
            if (board == null || tiles == null || tiles.Count == 0)
                return;

            Match3Tile[] origins = new Match3Tile[tiles.Count];
            tiles.CopyTo(origins);

            for (int i = 0; i < origins.Length; i++)
            {
                Match3Tile tile = origins[i];
                if (tile == null)
                    continue;

                ForEachLogic(tile.UpgradeIds, (logic, slot) => logic.CollectExtraClears(board, tile, slot, tiles));
            }
        }

        public void ApplyCleared(int[] upgradeIds, TileUpgradeContext context)
        {
            ForEachLogic(upgradeIds, (logic, _) => logic.OnCleared(context));
        }

        void ForEachLogic(int[] upgradeIds, Action<TileUpgradeLogic, int> apply)
        {
            if (upgradeIds == null || apply == null)
                return;

            for (int i = 0; i < upgradeIds.Length; i++)
            {
                if (!TryGetUpgrade(upgradeIds[i], out TileUpgradeDefinition upgrade))
                    continue;

                if (upgrade.Logic != null)
                    apply(upgrade.Logic, i);
            }
        }

        void EnsureLookups()
        {
            if (_idsByUpgrade != null)
                return;

            Entry[] entries = Entries;
            _idsByUpgrade = new Dictionary<TileUpgradeDefinition, int>(entries.Length);
            _upgradesById = new Dictionary<int, TileUpgradeDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Upgrade == null || entry.Id == InvalidUpgradeId)
                    continue;

                if (_upgradesById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(TileUpgradeConfig)} '{name}': upgrade id {entry.Id} is used twice. Give '{entry.Upgrade.name}' a unique id.",
                        this);
                    continue;
                }

                _idsByUpgrade[entry.Upgrade] = entry.Id;
                _upgradesById[entry.Id] = entry.Upgrade;
            }
        }

        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Length; i++)
                _nextUpgradeId = Mathf.Max(_nextUpgradeId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Upgrade == null || _entries[i].Id > InvalidUpgradeId)
                    continue;

                _entries[i].Id = _nextUpgradeId++;
            }
        }

        void OnEnable()
        {
            _idsByUpgrade = null;
            _upgradesById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsByUpgrade = null;
            _upgradesById = null;
        }
    }
}
