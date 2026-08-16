using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The registry of every tile type in the game and the one place tile ids come from. Ids are
    /// handed out once and never reused, so a tile referenced by a saved profile survives the list
    /// being reordered or trimmed.
    /// </summary>
    [CreateAssetMenu(fileName = "TileConfig", menuName = "M3P/Tile Config", order = 24)]
    public class TileConfig : ScriptableObject
    {
        /// <summary>Id of a tile type that this config does not know about.</summary>
        public const int InvalidTileId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans the tile in every profile that saved it.")]
            public int Id;
            public Match3TileTypeDefinition Tile;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Only ever counts up, so removing a tile type does not free its id for the next one.</summary>
        [SerializeField, HideInInspector] int _nextTileId = InvalidTileId + 1;

        Dictionary<Match3TileTypeDefinition, int> _idsByTile;
        Dictionary<int, Match3TileTypeDefinition> _tilesById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Id of a registered tile type, or <see cref="InvalidTileId"/> when it is not in this config.</summary>
        public int GetTileId(Match3TileTypeDefinition tile)
        {
            if (tile == null)
                return InvalidTileId;

            EnsureLookups();
            return _idsByTile.TryGetValue(tile, out int id) ? id : InvalidTileId;
        }

        public bool TryGetTile(int tileId, out Match3TileTypeDefinition tile)
        {
            EnsureLookups();
            return _tilesById.TryGetValue(tileId, out tile);
        }

        public Match3TileTypeDefinition GetTile(int tileId)
        {
            return TryGetTile(tileId, out Match3TileTypeDefinition tile) ? tile : null;
        }

        /// <summary>Turns owned copies back into the tile type assets a board can spawn.</summary>
        public void ResolveOwnedTiles(IReadOnlyList<OwnedTile> owned, List<Match3TileTypeDefinition> destination)
        {
            destination.Clear();
            AppendOwnedTiles(owned, destination);
        }

        /// <summary>Turns the profile's current tile deck into the type assets a board draws from.</summary>
        public void ResolveDeck(PlayerProfile profile, List<Match3TileTypeDefinition> destination)
        {
            destination.Clear();

            if (profile?.Tiles == null)
                return;

            IReadOnlyList<int> deck = profile.GetTileDeckIndices();
            for (int i = 0; i < deck.Count; i++)
            {
                int ownedIndex = deck[i];
                if (ownedIndex < 0 || ownedIndex >= profile.Tiles.Count)
                    continue;

                AppendTile(profile.Tiles[ownedIndex].TileId, destination);
            }
        }

        void AppendOwnedTiles(IReadOnlyList<OwnedTile> owned, List<Match3TileTypeDefinition> destination)
        {
            if (owned == null)
                return;

            for (int i = 0; i < owned.Count; i++)
                AppendTile(owned[i].TileId, destination);
        }

        void AppendTile(int tileId, List<Match3TileTypeDefinition> destination)
        {
            if (TryGetTile(tileId, out Match3TileTypeDefinition tile))
                destination.Add(tile);
            else
                Debug.LogWarning(
                    $"{nameof(TileConfig)} '{name}': profile references tile id {tileId}, which is missing from this registry.",
                    this);
        }

        void EnsureLookups()
        {
            if (_idsByTile != null)
                return;

            Entry[] entries = Entries;
            _idsByTile = new Dictionary<Match3TileTypeDefinition, int>(entries.Length);
            _tilesById = new Dictionary<int, Match3TileTypeDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Tile == null || entry.Id == InvalidTileId)
                    continue;

                if (_tilesById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(TileConfig)} '{name}': tile id {entry.Id} is used twice. Give '{entry.Tile.name}' a unique id.",
                        this);
                    continue;
                }

                _idsByTile[entry.Tile] = entry.Id;
                _tilesById[entry.Id] = entry.Tile;
            }
        }

        /// <summary>
        /// Hands the next free id to every newly added tile type. Existing ids are left alone so
        /// nothing already written to a save changes meaning.
        /// </summary>
        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Length; i++)
                _nextTileId = Mathf.Max(_nextTileId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Tile == null || _entries[i].Id > InvalidTileId)
                    continue;

                _entries[i].Id = _nextTileId++;
            }
        }

        void OnEnable()
        {
            _idsByTile = null;
            _tilesById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsByTile = null;
            _tilesById = null;
        }
    }
}
