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
        AdjacentPair = 2,
        Triple = 3
    }

    /// <summary>An extra prompt after tile targeting, used when a card needs a colour or direction.</summary>
    public enum CardExtraChoice
    {
        None = 0,
        TileColor = 1,
        GravityDirection = 2
    }

    /// <summary>One option in a <see cref="CardExtraChoice"/> prompt.</summary>
    public readonly struct CardChoiceOption
    {
        public readonly string Label;
        public readonly Color Color;
        public readonly int Value;

        public CardChoiceOption(string label, Color color, int value)
        {
            Label = label;
            Color = color;
            Value = value;
        }
    }

    /// <summary>
    /// The board-side behaviour of a card. Logic only mutates the board; collapsing, refilling and
    /// scoring matches is handled once by <see cref="Match3Board.ExecuteActionRoutine"/>.
    /// </summary>
    [Serializable]
    public abstract class BoardActionLogic
    {
        public abstract CardTargeting Targeting { get; }

        public virtual CardExtraChoice ExtraChoice => CardExtraChoice.None;

        /// <summary>
        /// When false, the board skips collapse, refill and match scoring after this action. Used by
        /// shuffle, rewind and delayed gravity so those cards cannot accidentally score.
        /// </summary>
        public virtual bool ResolvesMatchesAfterExecute => true;

        public abstract IEnumerator ExecuteRoutine(
            Match3Board board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice);

        public virtual void CollectExtraChoices(Match3Board board, List<CardChoiceOption> destination)
        {
            destination.Clear();
        }

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

            if (board.GetTile(candidate.x, candidate.y) == null)
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

        public virtual int RequiredTargetCount => (int)Targeting;
    }
}
