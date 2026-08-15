using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>Removes one negative tile, blockade or opponent-created piece. Pays no energy.</summary>
    [Serializable]
    public sealed class PurgeTileLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate) && board.CanPurgeTile(candidate.x, candidate.y);
        }

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 1)
            {
                yield break;
            }

            board.PurgeTile(targets[0]);
        }
    }

    /// <summary>
    /// Rotates four tiles in a 2×2 clockwise. The picked cell is the lower-left corner of the square.
    /// </summary>
    [Serializable]
    public sealed class Cycle2x2Logic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate))
            {
                return false;
            }

            return IsValidOrigin(board, candidate);
        }

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 1)
            {
                yield break;
            }

            Vector2Int origin = targets[0];
            Vector2Int[] cells =
            {
                origin,
                new Vector2Int(origin.x, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y)
            };
            yield return board.CycleCellsRoutine(cells);
        }

        static bool IsValidOrigin(Match3Board board, Vector2Int origin)
        {
            Vector2Int[] cells =
            {
                origin,
                new Vector2Int(origin.x, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y + 1),
                new Vector2Int(origin.x + 1, origin.y)
            };

            for (int i = 0; i < cells.Length; i++)
            {
                if (!board.CanMoveTile(cells[i].x, cells[i].y))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Rotates three corners of a 2×2. Pick order is the rotation: first → second → third → first.
    /// </summary>
    [Serializable]
    public sealed class TriangleRotationLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.Triple;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
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

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 3)
            {
                yield break;
            }

            yield return board.CycleCellsRoutine(targets);
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
    /// Sets gravity for the next resolve that actually removes tiles, then returns to the default.
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

        public override bool ResolvesMatchesAfterExecute => false;

        public override void CollectExtraChoices(Match3Board board, List<CardChoiceOption> destination)
        {
            destination.Clear();
            destination.AddRange(Directions);
        }

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            board.SetPendingGravity((BoardGravity)extraChoice);
            yield break;
        }
    }

    /// <summary>Randomly rearranges movable tiles. Accidental matches are not scored.</summary>
    [Serializable]
    public sealed class ChaoticShuffleLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.None;

        public override bool ResolvesMatchesAfterExecute => false;

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            yield return board.ShuffleMovableTilesRoutine();
        }
    }

    /// <summary>Restores the board to the layout from before the previously played card.</summary>
    [Serializable]
    public sealed class RewindBoardLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.None;

        public override bool ResolvesMatchesAfterExecute => false;

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            board.RestoreRewindSnapshot();
            yield break;
        }
    }
}
