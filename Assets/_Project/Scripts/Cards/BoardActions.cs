using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>Swaps two neighbouring tiles. Unlike a classic match-3 swap this is never undone.</summary>
    [Serializable]
    public sealed class SwapTilesLogic : BoardActionLogic
    {
        public override CardTargeting Targeting => CardTargeting.AdjacentPair;

        public override bool IsValidTarget(Match3Board board, IReadOnlyList<Vector2Int> picked, Vector2Int candidate)
        {
            if (!base.IsValidTarget(board, picked, candidate))
            {
                return false;
            }

            return picked.Count == 0 || Match3Board.AreAdjacent(picked[0], candidate);
        }

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets)
        {
            if (targets.Count < 2)
            {
                yield break;
            }

            yield return board.SwapRoutine(targets[0], targets[1]);
        }
    }

    /// <summary>Slides the row of the picked tile one cell sideways, wrapping around the edge.</summary>
    [Serializable]
    public sealed class ShiftRowLogic : BoardActionLogic
    {
        [Tooltip("Positive slides right, negative slides left.")]
        [SerializeField] int _direction = 1;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets)
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

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets)
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

    /// <summary>Recolours the picked tile, letting a build manufacture the exact mana a skill needs.</summary>
    [Serializable]
    public sealed class PaintTileLogic : BoardActionLogic
    {
        [SerializeField] Match3TileTypeDefinition _tileType;

        public Match3TileTypeDefinition TileType => _tileType;

        public override CardTargeting Targeting => CardTargeting.SingleTile;

        public override IEnumerator ExecuteRoutine(Match3Board board, IReadOnlyList<Vector2Int> targets)
        {
            if (targets.Count < 1 || _tileType == null)
            {
                yield break;
            }

            int typeId = board.GetTileTypeId(_tileType);
            if (typeId < 0)
            {
                Debug.LogError($"{nameof(PaintTileLogic)}: tile type '{_tileType.name}' is missing from the game config.");
                yield break;
            }

            board.SetTileType(targets[0].x, targets[0].y, typeId);
        }
    }
}
