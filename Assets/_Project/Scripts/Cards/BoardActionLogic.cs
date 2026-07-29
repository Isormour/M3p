using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>How many board cells a card needs picked before it can be played.</summary>
    public enum CardTargeting
    {
        /// <summary>Plays immediately, no board selection.</summary>
        None = 0,
        SingleTile = 1,
        AdjacentPair = 2
    }

    /// <summary>
    /// The board-side behaviour of a card. Logic only mutates the board; collapsing, refilling and
    /// scoring matches is handled once by <see cref="Match3Board.ExecuteActionRoutine"/>.
    /// </summary>
    [Serializable]
    public abstract class BoardActionLogic
    {
        public abstract CardTargeting Targeting { get; }

        public abstract IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets);

        /// <summary>
        /// Whether <paramref name="candidate"/> may be added to the cells picked so far. Used to reject
        /// clicks during targeting, before any AP is spent.
        /// </summary>
        public virtual bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (board == null || !board.IsInsideBoard(candidate.x, candidate.y))
            {
                return false;
            }

            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] == candidate)
                {
                    return false;
                }
            }

            return true;
        }

        public int RequiredTargetCount => (int)Targeting;
    }
}
