using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public enum SwapRelation
    {
        Orthogonal = 0,
        Horizontal = 1,
        Vertical = 2,
        Diagonal = 3,
        Distant = 4
    }

    /// <summary>Swaps two tiles. Unlike a classic match-3 swap this is never undone once it resolves.</summary>
    [Serializable]
    public sealed class SwapTilesLogic : BoardActionLogic
    {
        [SerializeField] SwapRelation _relation = SwapRelation.Orthogonal;

        public override CardTargeting Targeting => CardTargeting.AdjacentPair;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate) || !board.CanMoveTile(candidate.x, candidate.y))
            {
                return false;
            }

            if (picked.Count == 0)
            {
                return HasValidPartner(board, candidate);
            }

            return MatchesRelation(picked[0], candidate);
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            if (targets.Count < 2)
            {
                return;
            }

            ops.Add(BoardOp.Swap(targets[0], targets[1]));
        }

        bool HasValidPartner(SimBoard board, Vector2Int origin)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (candidate == origin || !board.CanMoveTile(x, y))
                    {
                        continue;
                    }

                    if (MatchesRelation(origin, candidate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool MatchesRelation(Vector2Int first, Vector2Int second)
        {
            switch (_relation)
            {
                case SwapRelation.Horizontal:
                    return Match3Board.AreHorizontallyAdjacent(first, second);
                case SwapRelation.Vertical:
                    return Match3Board.AreVerticallyAdjacent(first, second);
                case SwapRelation.Diagonal:
                    return Match3Board.AreDiagonallyAdjacent(first, second);
                case SwapRelation.Distant:
                    return Match3Board.AreDistantLineNeighbors(first, second);
                default:
                    return Match3Board.AreAdjacent(first, second);
            }
        }
    }

    /// <summary>
    /// Slides the row of the picked tile one cell sideways, wrapping around the edge. Parked: whole-row
    /// and whole-column movement is out of the progression design, so the card cannot be queued.
    /// </summary>
    [Serializable]
    public sealed class ShiftRowLogic : BoardActionLogic
    {
        [Tooltip("Positive slides right, negative slides left.")]
        [SerializeField] int _direction = 1;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsAvailable => false;

        public override string UnavailableReason => "Przesuwanie rzędów jest poza zakresem designu.";

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

            int y = targets[0].y;
            int step = _direction > 0 ? 1 : -1;
            Vector2Int[] cells = new Vector2Int[board.Width];
            for (int i = 0; i < board.Width; i++)
            {
                int x = step > 0 ? i : board.Width - 1 - i;
                cells[i] = new Vector2Int(x, y);
            }

            ops.Add(BoardOp.Cycle(cells));
        }
    }

    /// <summary>
    /// Marks the picked tile Cracked, plus an optional cross around it. Cracked tiles leave the board
    /// after the whole sequence has run, so later cards can still move and match them.
    /// </summary>
    [Serializable]
    public sealed class DestroyTilesLogic : BoardActionLogic
    {
        [Tooltip("When set, also cracks the four orthogonal neighbours of the picked tile.")]
        [SerializeField] bool _includeNeighbours;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate)
                && board.CanDestroyTile(candidate.x, candidate.y)
                && !board.IsCracked(candidate.x, candidate.y);
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

            Vector2Int origin = targets[0];
            ops.Add(BoardOp.MarkCracked(origin));

            if (!_includeNeighbours)
            {
                return;
            }

            AddCrackIfDestroyable(board, ops, new Vector2Int(origin.x + 1, origin.y));
            AddCrackIfDestroyable(board, ops, new Vector2Int(origin.x - 1, origin.y));
            AddCrackIfDestroyable(board, ops, new Vector2Int(origin.x, origin.y + 1));
            AddCrackIfDestroyable(board, ops, new Vector2Int(origin.x, origin.y - 1));
        }

        static void AddCrackIfDestroyable(SimBoard board, List<BoardOp> ops, Vector2Int cell)
        {
            if (board.CanDestroyTile(cell.x, cell.y))
                ops.Add(BoardOp.MarkCracked(cell));
        }
    }

    /// <summary>Cracks two neighbouring tiles at once. Each still pays its colour's mana on removal.</summary>
    [Serializable]
    public sealed class DestroyPairLogic : BoardActionLogic
    {
        [SerializeField] bool _vertical;

        public override CardTargeting Targeting => CardTargeting.AdjacentPair;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate)
                || !board.CanDestroyTile(candidate.x, candidate.y)
                || board.IsCracked(candidate.x, candidate.y))
            {
                return false;
            }

            if (picked.Count == 0)
            {
                return HasValidPartner(board, candidate);
            }

            return _vertical
                ? Match3Board.AreVerticallyAdjacent(picked[0], candidate)
                : Match3Board.AreHorizontallyAdjacent(picked[0], candidate);
        }

        public override void BuildOps(
            SimBoard board,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice,
            int seed,
            List<BoardOp> ops)
        {
            if (targets.Count < 2)
            {
                return;
            }

            ops.Add(BoardOp.MarkCracked(targets[0]));
            ops.Add(BoardOp.MarkCracked(targets[1]));
        }

        bool HasValidPartner(SimBoard board, Vector2Int origin)
        {
            Vector2Int[] neighbours = _vertical
                ? new[] { new Vector2Int(origin.x, origin.y + 1), new Vector2Int(origin.x, origin.y - 1) }
                : new[] { new Vector2Int(origin.x + 1, origin.y), new Vector2Int(origin.x - 1, origin.y) };

            for (int i = 0; i < neighbours.Length; i++)
            {
                if (board.CanDestroyTile(neighbours[i].x, neighbours[i].y) && !board.IsCracked(neighbours[i].x, neighbours[i].y))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Recolours the picked tile, letting a build manufacture the exact mana a skill needs.</summary>
    [Serializable]
    public sealed class PaintTileLogic : BoardActionLogic
    {
        [SerializeField] Match3TileTypeDefinition _tileType;

        public Match3TileTypeDefinition TileType => _tileType;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override CardExtraChoice ExtraChoice =>
            _tileType == null ? CardExtraChoice.TileColor : CardExtraChoice.None;

        public override bool IsValidTarget(SimBoard board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate) && board.CanRecolorTile(candidate.x, candidate.y);
        }

        public override void CollectExtraChoices(SimBoard board, List<CardChoiceOption> destination)
        {
            destination.Clear();
            if (board == null)
            {
                return;
            }

            for (int i = 0; i < board.TileTypeCount; i++)
            {
                string label = board.GetTileTypeName(i);
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                destination.Add(new CardChoiceOption(label, board.GetTileTypeColor(i), i));
            }
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

            int typeId = _tileType != null ? board.GetTileTypeId(_tileType) : extraChoice;
            if (typeId < 0)
            {
                Debug.LogError($"{nameof(PaintTileLogic)}: tile type is missing from the game config.");
                return;
            }

            ops.Add(BoardOp.Recolor(targets[0], typeId));
        }
    }
}
