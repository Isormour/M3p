using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    /// <summary>
    /// Arc drawn between a tile's current cell and the cell a queued card will move it to.
    /// Colour is written onto the line material so it matches the tile type.
    /// </summary>
    public sealed class TileMoveIndicator : MonoBehaviour
    {
        static readonly int ColId = Shader.PropertyToID("_Col");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [FormerlySerializedAs("line")]
        [SerializeField] LineRenderer _line;
        [SerializeField] int _arcPoints = 16;
        [SerializeField] float _arcHeight = 0.45f;
        [Tooltip("Pulls the arc toward the camera so it sits in front of the tiles.")]
        [SerializeField] float _depthOffset = 0.15f;

        Material _lineMaterial;

        void Awake()
        {
            if (_line == null)
                _line = GetComponentInChildren<LineRenderer>(true);

            if (_line != null)
                _lineMaterial = _line.material;
        }

        public void Present(Vector3 from, Vector3 to, Color color)
        {
            if (_line == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            BuildArc(from, to);
            ApplyColor(color);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void BuildArc(Vector3 from, Vector3 to)
        {
            Vector3 depth = Vector3.back * _depthOffset;
            from += depth;
            to += depth;

            int points = Mathf.Max(2, _arcPoints);
            _line.useWorldSpace = true;
            _line.positionCount = points;

            Vector3 along = to - from;
            Vector3 mid = (from + to) * 0.5f;
            Vector3 side = Vector3.Cross(Vector3.forward, along);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.up;
            else
                side.Normalize();

            float height = Mathf.Clamp(along.magnitude * 0.3f, _arcHeight, _arcHeight * 3f);
            Vector3 control = mid + side * height;

            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                _line.SetPosition(i, EvaluateQuadratic(from, control, to, t));
            }
        }

        void ApplyColor(Color color)
        {
            if (_lineMaterial == null)
                return;

            if (_lineMaterial.HasProperty(ColId))
                _lineMaterial.SetColor(ColId, color);

            if (_lineMaterial.HasProperty(ColorId))
                _lineMaterial.SetColor(ColorId, color);
        }

        static Vector3 EvaluateQuadratic(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * start
                + 2f * oneMinusT * t * control
                + t * t * end;
        }

        void OnDestroy()
        {
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }
    }
}
