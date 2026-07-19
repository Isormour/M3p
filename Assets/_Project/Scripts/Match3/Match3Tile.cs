using UnityEngine;

namespace Match3
{
    public class Match3Tile : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        public int X { get; private set; }
        public int Y { get; private set; }
        public int TypeId { get; private set; }

        Vector3 _baseScale;
        Renderer _meshRenderer;
        MaterialPropertyBlock _propertyBlock;

        public void Initialize(Match3Board board, int x, int y, int typeId)
        {
            X = x;
            Y = y;
            TypeId = typeId;
            _baseScale = transform.localScale;
        }

        public void ApplyMeshColor(Color color)
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponentInChildren<Renderer>();

            if (_meshRenderer == null)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(LegacyColorId, color);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
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
