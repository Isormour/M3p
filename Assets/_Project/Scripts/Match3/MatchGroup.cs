using System.Collections.Generic;

namespace Match3
{
    public enum MatchOrientation
    {
        Horizontal = 0,
        Vertical = 1
    }

    /// <summary>
    /// One maximal straight run of three or more tiles sharing a type. An L or T shape is reported
    /// as two overlapping groups, one per axis, so each line scores on its own length.
    /// </summary>
    /// <remarks>
    /// Groups stay valid after their tiles are cleared, but <see cref="Tiles"/> then holds destroyed
    /// objects. Consumers reacting to a resolved wave should only read <see cref="TypeId"/> and
    /// <see cref="Size"/>.
    /// </remarks>
    public sealed class MatchGroup
    {
        readonly List<Match3Tile> _tiles;

        public MatchGroup(int typeId, MatchOrientation orientation, List<Match3Tile> tiles)
        {
            TypeId = typeId;
            Orientation = orientation;
            _tiles = tiles;
        }

        public int TypeId { get; }

        public MatchOrientation Orientation { get; }

        public IReadOnlyList<Match3Tile> Tiles => _tiles;

        public int Size => _tiles.Count;
    }
}
