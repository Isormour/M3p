using UnityEngine;

namespace Match3
{
    public class Match3Tile : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int TypeId { get; private set; }

        Vector3 _baseScale;

        public void Initialize(Match3Board board, int x, int y, int typeId)
        {
            X = x;
            Y = y;
            TypeId = typeId;
            _baseScale = transform.localScale;
        }

        public void SetCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetType(int typeId)
        {
            TypeId = typeId;
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? _baseScale * 1.12f : _baseScale;
        }
    }
}
