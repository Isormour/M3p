using System.Collections.Generic;
using UnityEngine;

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
    /// objects. Consumers reacting to a resolved wave should only read <see cref="TypeId"/>,
    /// <see cref="Size"/> and <see cref="Center"/>.
    /// </remarks>
    public sealed class MatchGroup
    {
        readonly List<Match3Tile> _tiles;

        public MatchGroup(int typeId, MatchOrientation orientation, List<Match3Tile> tiles)
        {
            TypeId = typeId;
            Orientation = orientation;
            _tiles = tiles;
            Center = CalculateCenter(tiles);
        }

        public int TypeId { get; }

        public MatchOrientation Orientation { get; }

        public IReadOnlyList<Match3Tile> Tiles => _tiles;

        public int Size => _tiles.Count;

        /// <summary>
        /// Where the run sat in the world. Captured up front because everything that reacts to a match
        /// runs after its tiles have been destroyed, when their transforms can no longer be read.
        /// </summary>
        public Vector3 Center { get; }

        static Vector3 CalculateCenter(List<Match3Tile> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < tiles.Count; i++)
            {
                sum += tiles[i].transform.position;
            }

            return sum / tiles.Count;
        }
    }
}
