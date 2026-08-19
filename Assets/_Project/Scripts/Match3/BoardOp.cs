using System;
using UnityEngine;

namespace Match3
{
    public enum BoardOpKind
    {
        Swap = 0,
        Cycle = 1,
        Recolor = 2,
        MarkCracked = 3,
        Purge = 4,
        SetGravity = 5,
        Shuffle = 6
    }

    /// <summary>
    /// One primitive board command. Cards compile into ops when they enter the queue, so the predicted
    /// board and the real board run the same sequence at Resolve instead of each interpreting a card.
    /// </summary>
    public readonly struct BoardOp
    {
        static readonly Vector2Int[] NoCells = Array.Empty<Vector2Int>();

        public readonly BoardOpKind Kind;

        /// <summary>Cells the op works on. <see cref="BoardOpKind.Cycle"/> reads them in rotation order.</summary>
        public readonly Vector2Int[] Cells;

        /// <summary>Tile type for a recolour, <see cref="BoardGravity"/> for a gravity change, seed for a shuffle.</summary>
        public readonly int Value;

        BoardOp(BoardOpKind kind, Vector2Int[] cells, int value)
        {
            Kind = kind;
            Cells = cells ?? NoCells;
            Value = value;
        }

        public static BoardOp Swap(Vector2Int first, Vector2Int second)
        {
            return new BoardOp(BoardOpKind.Swap, new[] { first, second }, 0);
        }

        public static BoardOp Cycle(Vector2Int[] cells)
        {
            return new BoardOp(BoardOpKind.Cycle, cells, 0);
        }

        public static BoardOp Recolor(Vector2Int cell, int typeId)
        {
            return new BoardOp(BoardOpKind.Recolor, new[] { cell }, typeId);
        }

        public static BoardOp MarkCracked(Vector2Int cell)
        {
            return new BoardOp(BoardOpKind.MarkCracked, new[] { cell }, 0);
        }

        public static BoardOp Purge(Vector2Int cell)
        {
            return new BoardOp(BoardOpKind.Purge, new[] { cell }, 0);
        }

        public static BoardOp SetGravity(BoardGravity gravity)
        {
            return new BoardOp(BoardOpKind.SetGravity, null, (int)gravity);
        }

        public static BoardOp Shuffle(int seed)
        {
            return new BoardOp(BoardOpKind.Shuffle, null, seed);
        }
    }
}
