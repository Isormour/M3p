using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>Removes one negative tile, blockade or opponent-created piece. Pays no mana.</summary>
    [Serializable]
    public sealed class PurgeTileLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate) && board.CanPurgeTile(candidate.x, candidate.y);
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            if (targets.Count < 1)
            {
                return;
            }

            ops.Add(BoardOp.Purge(targets[0]));
        }
    }

    /// <summary>
    /// Rotates four tiles in a 2×2 clockwise. The picked cell is the lower-left corner of the square.
    /// </summary>
    [Serializable]
    public sealed class Cycle2x2Logic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate))
            {
                return false;
            }

            Vector2Int[] cells = SquareCells(candidate);
            for (int i = 0; i < cells.Length; i++)
            {
                if (!board.CanMoveTile(cells[i].x, cells[i].y))
                {
                    return false;
                }
            }

            return true;
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            if (targets.Count < 1)
            {
                return;
            }

            ops.Add(BoardOp.Cycle(SquareCells(targets[0])));
        }

        static Vector2Int[] SquareCells(Vector2Int origin)
        {
            return new[]
            {
                origin,
                new Vector2Int(origin.x, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y)
            };
        }
    }

    /// <summary>
    /// Rotates three corners of a 2×2. Pick order is the rotation: first → second → third → first.
    /// </summary>
    [Serializable]
    public sealed class TriangleRotationLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.Triple;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate) || !board.CanMoveTile(candidate.x, candidate.y))
            {
                return false;
            }

            if (picked.Count == 0)
            {
                return true;
            }

            if (picked.Count == 1)
            {
                return ChebyshevDistance(picked[0], candidate) == 1;
            }

            return FormsTriangleCorners(picked[0], picked[1], candidate);
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            if (targets.Count < 3)
            {
                return;
            }

            ops.Add(BoardOp.Cycle(new[] { targets[0], targets[1], targets[2] }));
        }

        static bool FormsTriangleCorners(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            if (maxX - minX != 1 || maxY - minY != 1)
            {
                return false;
            }

            bool collinear = a.x == b.x && b.x == c.x || a.y == b.y && b.y == c.y;
            return !collinear;
        }

        static int ChebyshevDistance(Vector2Int first, Vector2Int second)
        {
            return Mathf.Max(Mathf.Abs(first.x - second.x), Mathf.Abs(first.y - second.y));
        }
    }

    /// <summary>
    /// Sets the direction tiles fall for the rest of the Resolve, including every cascade wave it sets off.
    /// </summary>
    [Serializable]
    public sealed class GravityShiftLogic : BoardActionLogic
    {
        static readonly CardChoiceOption[] Directions =
        {
            new CardChoiceOption("Góra", new Color(0.55f, 0.8f, 1f), (int)BoardGravity.Up),
            new CardChoiceOption("Dół", new Color(0.85f, 0.7f, 0.4f), (int)BoardGravity.Down),
            new CardChoiceOption("Lewo", new Color(0.7f, 0.85f, 0.5f), (int)BoardGravity.Left),
            new CardChoiceOption("Prawo", new Color(0.9f, 0.55f, 0.55f), (int)BoardGravity.Right)
        };

        public override CardTargeting Targeting => CardTargeting.None;

        public override CardExtraChoice ExtraChoice => CardExtraChoice.GravityDirection;

        public override void CollectExtraChoices(SimBoard board, List<CardChoiceOption> destination)
        {
            destination.Clear();
            destination.AddRange(Directions);
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            ops.Add(BoardOp.SetGravity((BoardGravity)extraChoice));
        }
    }

    /// <summary>
    /// Randomly rearranges movable tiles. Finale: the seed is fixed when the card enters the queue and
    /// nothing may be queued behind it, so the preview never promises a layout it cannot know.
    /// </summary>
    [Serializable]
    public sealed class ChaoticShuffleLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.None;

        public override bool IsFinale => true;

        public override bool NeedsSeed => true;

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            ops.Add(BoardOp.Shuffle(seed));
        }
    }

    /// <summary>
    /// Restored the board to the layout from before the previous card. Parked: with a free Undo of the
    /// last queued card the effect needs a new purpose, so the card cannot be queued yet.
    /// </summary>
    [Serializable]
    public sealed class RewindBoardLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.None;

        public override bool IsAvailable => false;

        public override string UnavailableReason => "Rewind wymaga przeprojektowania pod model kolejki.";

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
        }
    }
}
