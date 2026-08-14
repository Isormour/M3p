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

        public string NodeId { get; private set; }
        public MapNodeType NodeType { get; private set; }
        public EncounterConfig Encounter { get; private set; }

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
            EnsureLabel();
            ApplyVisualState();
        }

        public void SetState(bool isCurrent, bool reachable, bool cleared)
        {
            _isCurrent = isCurrent;
            _reachable = reachable;
            _cleared = cleared;
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

        void EnsureLabel()
        {
            if (_label != null)
                return;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, _labelHeight, 0f);

            _label = labelObject.AddComponent<TextMeshPro>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 3.2f;
            _label.color = Color.white;
            _label.text = NodeType.ToString();
            _label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            labelObject.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
        }

        void ApplyVisualState()
        {
            if (_label != null)
            {
                string suffix = _cleared && NodeType != MapNodeType.Start ? " ✓" : string.Empty;
                _label.text = NodeType + suffix;
            }

            float scaleMul = _isCurrent ? 1.25f : _reachable ? 1.1f : 1f;
            transform.localScale = _baseScale * scaleMul;

            if (!_tintRenderers || _materials == null)
                return;

            Color color = _baseColor;
            if (_cleared && !_isCurrent)
                color *= 0.45f;
            else if (_reachable)
                color = Color.Lerp(color, Color.white, 0.35f);

            float emission = _isCurrent ? 0.55f : _reachable ? 0.28f : 0f;

            for (int i = 0; i < _materials.Length; i++)
            {
                Material material = _materials[i];
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else
                    material.color = color;

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * emission);
                }
            }
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
