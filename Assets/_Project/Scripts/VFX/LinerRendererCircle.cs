using UnityEngine;

namespace M3P
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LinerRendererCircle : MonoBehaviour
    {
        [SerializeField] float _radius = 1f;
        [SerializeField] int _segments = 32;

        LineRenderer _line;

        void OnEnable()
        {
            EnsureLine();
            Rebuild();
        }

        void OnValidate()
        {
            _radius = Mathf.Max(0f, _radius);
            _segments = Mathf.Max(3, _segments);
            Rebuild();
        }

        void EnsureLine()
        {
            if (_line == null)
                _line = GetComponent<LineRenderer>();
        }

        void Rebuild()
        {
            EnsureLine();
            if (_line == null)
                return;

            int segments = Mathf.Max(3, _segments);
            float radius = Mathf.Max(0f, _radius);

            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = segments;

            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = step * i;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
