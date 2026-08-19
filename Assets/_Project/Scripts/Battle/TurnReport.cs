using System.Collections.Generic;
using Match3;

namespace M3P
{
    /// <summary>
    /// What the player's turn has produced so far, across every sequence in it. Kept apart from the
    /// per-Resolve report because a turn may contain several Resolves, and some payouts care about the
    /// order of colours or the number of sequences rather than about one sequence's board result.
    /// </summary>
    public sealed class TurnReport
    {
        readonly List<int> _colorOrder = new List<int>();

        /// <summary>Sequences the player has resolved this turn.</summary>
        public int ResolveCount { get; private set; }

        public int TotalMatchGroups { get; private set; }

        public int LargestMatch { get; private set; }

        public int TotalCascadeWaves { get; private set; }

        /// <summary>Stamina handed back this turn, watched to keep turns from running forever.</summary>
        public int StaminaRefunded { get; private set; }

        /// <summary>Colours matched in the order their first match landed, for Spectrum-style payouts.</summary>
        public IReadOnlyList<int> ColorOrder => _colorOrder;

        public void BeginTurn()
        {
            _colorOrder.Clear();
            ResolveCount = 0;
            TotalMatchGroups = 0;
            LargestMatch = 0;
            TotalCascadeWaves = 0;
            StaminaRefunded = 0;
        }

        public void AddResolve(ResolveReport report, int staminaRefunded)
        {
            ResolveCount++;
            StaminaRefunded += staminaRefunded;

            if (report == null)
                return;

            TotalMatchGroups += report.MatchGroupCount;
            TotalCascadeWaves += report.MatchWaves;

            if (report.LargestMatch > LargestMatch)
                LargestMatch = report.LargestMatch;

            foreach (int typeId in report.MatchedTypeIds)
            {
                if (!_colorOrder.Contains(typeId))
                    _colorOrder.Add(typeId);
            }
        }
    }
}
