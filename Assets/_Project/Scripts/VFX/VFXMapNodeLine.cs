using UnityEngine;

namespace M3P
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(PillarHighlight))]
    public sealed class VFXMapNodeLine : MonoBehaviour
    {
        [SerializeField] LineRenderer _line;
        [SerializeField] PillarHighlight _highlight;
        [Tooltip("How far from each node center the straight edge starts and ends.")]
        [SerializeField] float _endpointDistance = 0.55f;
        [SerializeField] float _heightOffset = 0.05f;

        bool _fromHighlighted;
        bool _toHighlighted;

        public string FromId { get; private set; }
        public string ToId { get; private set; }

        public void Configure(string fromId, string toId, Vector3 from, Vector3 to)
        {
            FromId = fromId;
            ToId = toId;
            _fromHighlighted = false;
            _toHighlighted = false;
            EnsureLine();
            EnsureHighlight();
            Place(from, to);
            ApplyHighlight();
        }

        public void SetEndpointHighlighted(string nodeId, bool highlighted)
        {
            if (nodeId == FromId)
                _fromHighlighted = highlighted;
            else if (nodeId == ToId)
                _toHighlighted = highlighted;
            else
                return;

            ApplyHighlight();
        }

        void ApplyHighlight()
        {
            EnsureHighlight();
            if (_highlight != null)
                _highlight.SetHighlighted(_fromHighlighted || _toHighlighted);
        }

        void EnsureLine()
        {
            if (_line == null)
                _line = GetComponent<LineRenderer>();
        }

        void EnsureHighlight()
        {
            if (_highlight == null)
                _highlight = GetComponent<PillarHighlight>();
        }

        void Place(Vector3 from, Vector3 to)
        {
            if (_line == null)
                return;

            Vector3 lift = Vector3.up * _heightOffset;
            from += lift;
            to += lift;

            _line.useWorldSpace = true;
            _line.loop = false;
            _line.positionCount = 2;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            float inset = Mathf.Max(0f, _endpointDistance);
            if (length < 0.0001f)
            {
                _line.SetPosition(0, from);
                _line.SetPosition(1, to);
                return;
            }

            Vector3 dir = delta / length;
            float clamped = Mathf.Min(inset, length * 0.5f);
            _line.SetPosition(0, from + dir * clamped);
            _line.SetPosition(1, to - dir * clamped);
        }
    }
}
