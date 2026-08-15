using System;
using System.Collections;
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

    /// <summary>Swaps two neighbouring tiles. Unlike a classic match-3 swap this is never undone.</summary>
    [Serializable]
    public sealed class SwapTilesLogic : BoardActionLogic
    {
        [SerializeField] SwapRelation _relation = SwapRelation.Orthogonal;

        public override CardTargeting Targeting => CardTargeting.AdjacentPair;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
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

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 2)
            {
                yield break;
            }

            yield return board.SwapRoutine(targets[0], targets[1]);
        }

        bool HasValidPartner(Match3Board board, Vector2Int origin)
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

    /// <summary>Slides the row of the picked tile one cell sideways, wrapping around the edge.</summary>
    [Serializable]
    public sealed class ShiftRowLogic : BoardActionLogic
    {
        [Tooltip("Positive slides right, negative slides left.")]
        [SerializeField] int _direction = 1;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 1)
            {
                yield break;
            }

            yield return board.ShiftRowRoutine(targets[0].y, _direction);
        }
    }

    /// <summary>
    /// Destroys the picked tile plus an optional cross around it. Grants mana but no basic attack,
    /// because nothing here forms a match.
    /// </summary>
    [Serializable]
    public sealed class DestroyTilesLogic : BoardActionLogic
    {
        [Tooltip("When set, also destroys the four orthogonal neighbours of the picked tile.")]
        [SerializeField] bool _includeNeighbours;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate) && board.CanDestroyTile(candidate.x, candidate.y);
        }

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 1)
            {
                yield break;
            }

            Vector2Int origin = targets[0];
            List<Vector2Int> cells = new List<Vector2Int> { origin };

            if (_includeNeighbours)
            {
                cells.Add(new Vector2Int(origin.x + 1, origin.y));
                cells.Add(new Vector2Int(origin.x - 1, origin.y));
                cells.Add(new Vector2Int(origin.x, origin.y + 1));
                cells.Add(new Vector2Int(origin.x, origin.y - 1));
            }

            board.DestroyTiles(cells);
            yield break;
        }
    }

    /// <summary>Destroys two neighbouring tiles at once. Each still pays its colour's energy.</summary>
    [Serializable]
    public sealed class DestroyPairLogic : BoardActionLogic
    {
        [SerializeField] bool _vertical;

        public override CardTargeting Targeting => CardTargeting.AdjacentPair;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate) || !board.CanDestroyTile(candidate.x, candidate.y))
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

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 2)
            {
                yield break;
            }

            board.DestroyTiles(targets);
            yield break;
        }

        bool HasValidPartner(Match3Board board, Vector2Int origin)
        {
            Vector2Int[] neighbours = _vertical
                ? new[] { new Vector2Int(origin.x, origin.y + 1), new Vector2Int(origin.x, origin.y - 1) }
                : new[] { new Vector2Int(origin.x + 1, origin.y), new Vector2Int(origin.x - 1, origin.y) };

            for (int i = 0; i < neighbours.Length; i++)
            {
                if (board.CanDestroyTile(neighbours[i].x, neighbours[i].y))
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

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            return base.IsValidTarget(board, picked, candidate) && board.CanRecolorTile(candidate.x, candidate.y);
        }

        public override void CollectExtraChoices(Match3Board board, List<CardChoiceOption> destination)
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

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets, int extraChoice)
        {
            if (targets.Count < 1)
            {
                yield break;
            }

            int typeId = _tileType != null ? board.GetTileTypeId(_tileType) : extraChoice;
            if (typeId < 0)
            {
                Debug.LogError($"{nameof(PaintTileLogic)}: tile type is missing from the game config.");
                yield break;
            }

            board.SetTileType(targets[0].x, targets[0].y, typeId);
        }
    }
}
