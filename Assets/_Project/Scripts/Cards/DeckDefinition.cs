using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring format for a deck. Used to seed a new profile from <see cref="PlayerStartConfig"/>;
    /// battles draw from the copies stored on the profile, not from this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Deck", menuName = "M3P/Deck", order = 21)]
    public class DeckDefinition : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public BoardActionCardDefinition Card;
            [Min(1)] public int Copies;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Flattens the entry counts into one card per list slot.</summary>
        public void BuildCardList(List<BoardActionCardDefinition> destination)
        {
            destination.Clear();

            Entry[] entries = Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Card == null)
                {
                    continue;
                }

                int copies = Mathf.Max(1, entries[i].Copies);
                for (int copy = 0; copy < copies; copy++)
                {
                    destination.Add(entries[i].Card);
                }
            }
        }
    }
}
