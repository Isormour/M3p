using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>Tile type lookups that card logic needs while the player is still planning.</summary>
    public interface IBoardTypeCatalog
    {
        int TileTypeCount { get; }

        string GetTileTypeName(int typeId);

        Color GetTileTypeColor(int typeId);

        int GetTileTypeId(Match3TileTypeDefinition tileType);
    }

    /// <summary>
    /// A tile as plain data. Identity travels with the tile through swaps and cycles, which is what
    /// lets a Cracked mark follow the tile it was placed on rather than staying on a cell.
    /// </summary>
    public sealed class SimTile
    {
        public int Id;
        public int TypeId;
        public int[] UpgradeIds = Array.Empty<int>();
        public bool IsLocked;
        public bool IsNegative;
        public bool IsBlockade;
        public bool IsEnemyElement;
        public bool AllowsColorChange = true;
        public bool CanDestroy = true;
        public bool IsCracked;

        public bool CanRecolor => AllowsColorChange;

        public bool IsPurgeable => IsNegative || IsBlockade || IsEnemyElement;

        public bool CanMove => !IsLocked && !IsBlockade;

        /// <summary>Upgrade ids are shared, never mutated in place, so a shallow copy is safe.</summary>
        public SimTile Clone()
        {
            return (SimTile)MemberwiseClone();
        }
    }

    /// <summary>One straight run of same-type tiles found on a simulated board.</summary>
    public readonly struct SimMatch
    {
        public readonly int TypeId;
        public readonly MatchOrientation Orientation;
        public readonly Vector2Int[] Cells;

        public SimMatch(int typeId, MatchOrientation orientation, Vector2Int[] cells)
        {
            TypeId = typeId;
            Orientation = orientation;
            Cells = cells;
        }

        public int Size => Cells != null ? Cells.Length : 0;
    }

    /// <summary>
    /// The board as pure data. Used for the predicted board the player plans against: it applies the
    /// same <see cref="BoardOp"/> list the real board replays at Resolve, so both agree on the outcome.
    /// Refills and cascades are deliberately not simulated — the preview must not promise random results.
    /// </summary>
    public sealed class SimBoard
    {
        readonly SimTile[,] _cells;

        public SimBoard(int width, int height, IBoardTypeCatalog catalog)
        {
            Width = Mathf.Max(0, width);
            Height = Mathf.Max(0, height);
            Catalog = catalog;
            _cells = new SimTile[Width, Height];
        }

        public int Width { get; }

        public int Height { get; }

        public IBoardTypeCatalog Catalog { get; }

        /// <summary>Gravity a queued Gravity Shift asked for, or null while the default applies.</summary>
        public BoardGravity? PendingGravity { get; set; }

        public int TileTypeCount => Catalog != null ? Catalog.TileTypeCount : 0;

        public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public SimTile GetTile(int x, int y) => IsInside(x, y) ? _cells[x, y] : null;

        public SimTile GetTile(Vector2Int cell) => GetTile(cell.x, cell.y);

        public void SetTile(int x, int y, SimTile tile)
        {
            if (IsInside(x, y))
                _cells[x, y] = tile;
        }

        public SimBoard Clone()
        {
            SimBoard copy = new SimBoard(Width, Height, Catalog) { PendingGravity = PendingGravity };

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    SimTile tile = _cells[x, y];
                    copy._cells[x, y] = tile?.Clone();
                }
            }

            return copy;
        }

        public bool CanMoveTile(int x, int y)
        {
            SimTile tile = GetTile(x, y);
            return tile != null && tile.CanMove;
        }

        public bool CanRecolorTile(int x, int y)
        {
            SimTile tile = GetTile(x, y);
            return tile != null && tile.CanRecolor;
        }

        public bool CanDestroyTile(int x, int y)
        {
            SimTile tile = GetTile(x, y);
            return tile != null && tile.CanDestroy;
        }

        public bool CanPurgeTile(int x, int y)
        {
            SimTile tile = GetTile(x, y);
            return tile != null && tile.IsPurgeable;
        }

        public bool IsCracked(int x, int y)
        {
            SimTile tile = GetTile(x, y);
            return tile != null && tile.IsCracked;
        }

        public string GetTileTypeName(int typeId) => Catalog != null ? Catalog.GetTileTypeName(typeId) : string.Empty;

        public Color GetTileTypeColor(int typeId) => Catalog != null ? Catalog.GetTileTypeColor(typeId) : Color.white;

        public int GetTileTypeId(Match3TileTypeDefinition tileType) => Catalog != null ? Catalog.GetTileTypeId(tileType) : -1;

        public void Apply(IReadOnlyList<BoardOp> ops)
        {
            if (ops == null)
                return;

            for (int i = 0; i < ops.Count; i++)
                Apply(ops[i]);
        }

        public void Apply(BoardOp op)
        {
            switch (op.Kind)
            {
                case BoardOpKind.Swap:
                    ApplySwap(op.Cells);
                    return;
                case BoardOpKind.Cycle:
                    ApplyCycle(op.Cells);
                    return;
                case BoardOpKind.Recolor:
                    ApplyRecolor(op.Cells, op.Value);
                    return;
                case BoardOpKind.MarkCracked:
                    ApplyMarkCracked(op.Cells);
                    return;
                case BoardOpKind.Purge:
                    ApplyPurge(op.Cells);
                    return;
                case BoardOpKind.SetGravity:
                    PendingGravity = (BoardGravity)op.Value;
                    return;
                case BoardOpKind.Shuffle:
                    ApplyShuffle(op.Value);
                    return;
            }
        }

        void ApplySwap(Vector2Int[] cells)
        {
            if (cells == null || cells.Length < 2)
                return;

            Vector2Int first = cells[0];
            Vector2Int second = cells[1];
            if (!IsInside(first.x, first.y) || !IsInside(second.x, second.y))
                return;

            (_cells[first.x, first.y], _cells[second.x, second.y]) =
                (_cells[second.x, second.y], _cells[first.x, first.y]);
        }

        void ApplyCycle(Vector2Int[] cells)
        {
            if (cells == null || cells.Length < 2)
                return;

            SimTile[] moving = new SimTile[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                if (!IsInside(cells[i].x, cells[i].y))
                    return;

                moving[i] = _cells[cells[i].x, cells[i].y];
            }

            for (int i = 0; i < cells.Length; i++)
            {
                Vector2Int destination = cells[(i + 1) % cells.Length];
                _cells[destination.x, destination.y] = moving[i];
            }
        }

        void ApplyRecolor(Vector2Int[] cells, int typeId)
        {
            if (cells == null || cells.Length < 1)
                return;

            SimTile tile = GetTile(cells[0]);
            if (tile != null)
                tile.TypeId = typeId;
        }

        void ApplyMarkCracked(Vector2Int[] cells)
        {
            if (cells == null)
                return;

            for (int i = 0; i < cells.Length; i++)
            {
                SimTile tile = GetTile(cells[i]);
                if (tile != null)
                    tile.IsCracked = true;
            }
        }

        void ApplyPurge(Vector2Int[] cells)
        {
            if (cells == null || cells.Length < 1)
                return;

            Vector2Int cell = cells[0];
            SimTile tile = GetTile(cell);
            if (tile == null || !tile.IsPurgeable)
                return;

            bool overlayOnly = tile.IsBlockade && !tile.IsNegative && !tile.IsEnemyElement;
            if (overlayOnly)
            {
                tile.IsNegative = false;
                tile.IsBlockade = false;
                tile.IsEnemyElement = false;
                tile.IsLocked = false;
                tile.AllowsColorChange = true;
                tile.CanDestroy = true;
                return;
            }

            _cells[cell.x, cell.y] = null;
        }

        /// <summary>
        /// Rearranges movable tiles from a fixed seed. The real board runs the identical permutation,
        /// which is what lets a random card stay honest in the preview.
        /// </summary>
        public void ApplyShuffle(int seed)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            List<SimTile> movable = new List<SimTile>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    SimTile tile = _cells[x, y];
                    if (tile == null || !tile.CanMove)
                        continue;

                    cells.Add(new Vector2Int(x, y));
                    movable.Add(tile);
                }
            }

            if (movable.Count < 2)
                return;

            ShuffleDeterministic(movable, seed);

            for (int i = 0; i < cells.Count; i++)
                _cells[cells[i].x, cells[i].y] = movable[i];
        }

        /// <summary>
        /// Fisher-Yates over a fixed seed. Kept here so the real board can reuse the exact algorithm.
        /// </summary>
        public static void ShuffleDeterministic<T>(IList<T> values, int seed)
        {
            System.Random random = new System.Random(seed);
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        /// <summary>
        /// Every maximal run of <see cref="Match3Board.MinimumMatchSize"/> or more same-type tiles, one
        /// entry per line, so a tile on an L or T intersection belongs to two runs.
        /// </summary>
        public List<SimMatch> FindMatches()
        {
            List<SimMatch> matches = new List<SimMatch>();

            for (int y = 0; y < Height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= Width; x++)
                {
                    if (x < Width && ContinuesRun(_cells[x, y], _cells[x - 1, y]))
                        continue;

                    int runLength = x - runStart;
                    if (runLength >= Match3Board.MinimumMatchSize)
                    {
                        Vector2Int[] cells = new Vector2Int[runLength];
                        for (int i = 0; i < runLength; i++)
                            cells[i] = new Vector2Int(runStart + i, y);

                        matches.Add(new SimMatch(_cells[runStart, y].TypeId, MatchOrientation.Horizontal, cells));
                    }

                    runStart = x;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= Height; y++)
                {
                    if (y < Height && ContinuesRun(_cells[x, y], _cells[x, y - 1]))
                        continue;

                    int runLength = y - runStart;
                    if (runLength >= Match3Board.MinimumMatchSize)
                    {
                        Vector2Int[] cells = new Vector2Int[runLength];
                        for (int i = 0; i < runLength; i++)
                            cells[i] = new Vector2Int(x, runStart + i);

                        matches.Add(new SimMatch(_cells[x, runStart].TypeId, MatchOrientation.Vertical, cells));
                    }

                    runStart = y;
                }
            }

            return matches;
        }

        static bool ContinuesRun(SimTile current, SimTile previous)
        {
            return current != null && previous != null && current.TypeId == previous.TypeId;
        }
    }
}
