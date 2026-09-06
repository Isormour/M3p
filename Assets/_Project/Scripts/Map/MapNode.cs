using TMPro;
using UnityEngine;

namespace M3P
{
    public class MapNode : MonoBehaviour
    {
        [SerializeField] float _labelHeight = 0.85f;
        [SerializeField] bool _tintRenderers = true;
        [field: SerializeField] public Transform camTarget { get; private set; }


        Renderer[] _renderers;
        Material[] _materials;
        TextMeshPro _label;
        Color _baseColor = Color.white;
        Vector3 _baseScale = Vector3.one;
        bool _reachable;
        bool _isCurrent;
        bool _cleared;
        bool _configured;
        bool _highlighted;
        [SerializeField] PillarHighlight[] _highlights;

        public string NodeId { get; private set; }
        [field: SerializeField] public MapNodeType NodeType { get; private set; }
        public EncounterConfig Encounter { get; private set; }
        public bool IsCurrent => _isCurrent;
        public bool IsCleared => _cleared;
        public bool ShouldHighlight => _highlighted;
        [field: SerializeField] public Light NodeLight { get; private set; }

        void Awake()
        {
            ApplyLightState();
        }

        public virtual void Configure(string nodeId, EncounterConfig encounter, MapNodeType type, Color color)
        {
            NodeId = nodeId;
            Encounter = encounter;
            NodeType = type;
            _baseColor = color;
            _baseScale = transform.localScale;
            _configured = true;

            EnsureCollider();
            CacheRenderers();
            ApplyVisualState();
        }

        public virtual void SetState(bool isCurrent, bool reachable, bool cleared, bool highlighted)
        {
            _isCurrent = isCurrent;
            _reachable = reachable;
            _cleared = cleared;
            _highlighted = highlighted;
            if (_configured)
                ApplyVisualState();
        }

        void EnsureCollider()
        {
            if (GetComponentInChildren<Collider>() != null)
                return;

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
        }

        void CacheRenderers()
        {
            if (!_tintRenderers)
                return;

            _renderers = GetComponentsInChildren<Renderer>(true);
            if (_renderers == null || _renderers.Length == 0)
                return;

            _materials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                _materials[i] = _renderers[i].material;
            }
        }

        void ApplyVisualState()
        {
            if (_label != null)
            {
                string suffix = _cleared && NodeType != MapNodeType.Start ? " ✓" : string.Empty;
                _label.text = NodeType.DisplayName() + suffix;
            }

            float scaleMul = _isCurrent ? 1.25f : _reachable ? 1.1f : 1f;
            transform.localScale = _baseScale * scaleMul;

            ApplyHighlightState();

            if (!_tintRenderers || _materials == null)
                return;
        }

        void ApplyHighlightState()
        {
            if (_highlights == null || _highlights.Length == 0)
                _highlights = GetComponentsInChildren<PillarHighlight>(true);

            for (int i = 0; i < _highlights.Length; i++)
            {
                if (_highlights[i] != null)
                    _highlights[i].SetHighlighted(_highlighted);
            }

            ApplyLightState();
        }

        bool KeepsLightOn =>
            NodeType == MapNodeType.Shop ||
            NodeType == MapNodeType.Forge ||
            NodeType == MapNodeType.Chest;

        void ApplyLightState()
        {
            if (NodeLight == null)
                NodeLight = GetComponentInChildren<Light>(true);

            if (NodeLight == null)
                return;

            if (KeepsLightOn)
            {
                NodeLight.enabled = true;
                NodeLight.intensity = _highlighted ? 15f : 7f;
                return;
            }

            if (_reachable)
            {
                NodeLight.enabled = true;
                NodeLight.intensity = 7f;
                return;
            }

            NodeLight.enabled = _highlighted;
        }

        void OnDestroy()
        {
            if (_materials == null)
                return;

            for (int i = 0; i < _materials.Length; i++)
            {
                if (_materials[i] != null)
                    Destroy(_materials[i]);
            }
        }
    }
}
