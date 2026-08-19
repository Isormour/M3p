using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// What one Resolve produced. Reward runes read this instead of "the card that was played", so a
    /// sequence of several cards can cooperate on a single payout.
    /// </summary>
    public sealed class ResolveReport
    {
        readonly HashSet<int> _matchedTypeIds = new HashSet<int>();
        readonly List<int> _matchSizes = new List<int>();

        /// <summary>Cards executed in the sequence that produced this report.</summary>
        public int CardsInSequence { get; internal set; }

        /// <summary>Match groups cleared across every wave of the Resolve.</summary>
        public int MatchGroupCount => _matchSizes.Count;

        /// <summary>Longest single run cleared, used by super match payouts.</summary>
        public int LargestMatch { get; private set; }

        /// <summary>Waves that cleared at least one match. The first wave is the planned one.</summary>
        public int MatchWaves { get; private set; }

        /// <summary>Waves beyond the first, which is what the Cascade rune rewards.</summary>
        public int ExtraWaves => Mathf.Max(0, MatchWaves - 1);

        /// <summary>Tiles removed by card commands (Cracked and Purge) rather than by matching.</summary>
        public int TilesClearedByCards { get; internal set; }

        public int TilesClearedByMatches { get; private set; }

        public IReadOnlyCollection<int> MatchedTypeIds => _matchedTypeIds;

        public IReadOnlyList<int> MatchSizes => _matchSizes;

        public bool HasMatch => _matchSizes.Count > 0;

        /// <summary>True when exactly one run of four or more cleared, which is what Precision asks for.</summary>
        public bool IsSingleLargeMatch => _matchSizes.Count == 1 && _matchSizes[0] >= 4;

        public int CountMatchesOfAtLeast(int size)
        {
            int count = 0;
            for (int i = 0; i < _matchSizes.Count; i++)
            {
                if (_matchSizes[i] >= size)
                    count++;
            }

            return count;
        }

        public void Reset()
        {
            _matchedTypeIds.Clear();
            _matchSizes.Clear();
            CardsInSequence = 0;
            LargestMatch = 0;
            MatchWaves = 0;
            TilesClearedByCards = 0;
            TilesClearedByMatches = 0;
        }

        internal void AddWave(IReadOnlyList<MatchGroup> groups)
        {
            if (groups == null || groups.Count == 0)
                return;

            MatchWaves++;

            for (int i = 0; i < groups.Count; i++)
            {
                MatchGroup group = groups[i];
                _matchSizes.Add(group.Size);
                _matchedTypeIds.Add(group.TypeId);
                TilesClearedByMatches += group.Size;

                if (group.Size > LargestMatch)
                    LargestMatch = group.Size;
            }
        }
    }
}
