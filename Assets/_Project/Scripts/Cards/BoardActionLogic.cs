using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>How many board cells a card needs picked before it can join the queue.</summary>
    public enum CardTargeting
    {
        /// <summary>Queues immediately, no board selection.</summary>
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
    /// The board-side behaviour of a card. A card does not touch the board when it is played: it
    /// compiles into <see cref="BoardOp"/> commands against the predicted board, and the real board
    /// replays those same commands at Resolve. Collapsing, refilling and scoring happen once per
    /// Resolve, after every card in the sequence has run.
    /// </summary>
    [Serializable]
    public abstract class BoardActionLogic
    {
        public abstract CardTargeting Targeting { get; }

        public virtual CardExtraChoice ExtraChoice => CardExtraChoice.None;

        /// <summary>
        /// A Finale card has to be the last one in the queue. Used by cards whose result the player
        /// cannot plan around, so nothing is queued behind an outcome the preview cannot show.
        /// </summary>
        public virtual bool IsFinale => false;

        /// <summary>
        /// Needs a random seed fixed the moment it enters the queue, so the preview and the Resolve
        /// produce the same board.
        /// </summary>
        public virtual bool NeedsSeed => false;

        /// <summary>
        /// False keeps the card out of the queue entirely. Used for designs parked until a later
        /// prototype, so their assets stay valid without the card being playable.
        /// </summary>
        public virtual bool IsAvailable => true;

        /// <summary>Why the card cannot be queued, shown to the player when <see cref="IsAvailable"/> is false.</summary>
        public virtual string UnavailableReason => string.Empty;

        /// <summary>
        /// Turns picked targets into board commands. Runs against the predicted board, which already
        /// includes every card queued earlier in the sequence.
        /// </summary>
        public abstract void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops);

        public virtual void CollectExtraChoices(SimBoard board, List<CardChoiceOption> destination)
        {
            destination.Clear();
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> may be added to the cells picked so far. Validated
        /// against the predicted board, so a card can target the result of the cards ahead of it.
        /// </summary>
        public virtual bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (board == null || !board.IsInside(candidate.x, candidate.y))
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
