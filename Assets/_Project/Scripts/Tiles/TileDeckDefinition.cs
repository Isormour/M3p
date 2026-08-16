using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring format for a tile deck. Used to seed a new profile from <see cref="PlayerStartConfig"/>;
    /// battles draw from the copies stored on the profile, not from this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "TileDeck", menuName = "M3P/Tile Deck", order = 23)]
    public class TileDeckDefinition : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Match3TileTypeDefinition Tile;
            [Min(1)] public int Copies;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Flattens the entry counts into one tile type per list slot.</summary>
        public void BuildTileList(List<Match3TileTypeDefinition> destination)
        {
            destination.Clear();

            Entry[] entries = Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Tile == null)
                    continue;

                int copies = Mathf.Max(1, entries[i].Copies);
                for (int copy = 0; copy < copies; copy++)
                    destination.Add(entries[i].Tile);
            }
        }
    }
}
