using UnityEngine;

namespace M3P
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PillarHighlight : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColId = Shader.PropertyToID("_Col");

        [SerializeField] Color _onColor = new Color(1f, 0.63f, 0f, 1f);
        [SerializeField] Color _offColor = new Color(0.18f, 0.2f, 0.24f, 0.35f);

        LineRenderer _line;
        MaterialPropertyBlock _block;
        Gradient _gradient;
        GradientColorKey[] _colorKeys;
        GradientAlphaKey[] _alphaKeys;
        bool _highlighted;

        void Awake()
        {
            EnsureLine();
            ApplyColor(_highlighted);
        }

        void OnEnable()
        {
            MapNode node = GetComponentInParent<MapNode>();
            if (node != null)
                SetHighlighted(node.ShouldHighlight);
            else
                ApplyColor(_highlighted);
        }

        void OnValidate()
        {
            ApplyColor(_highlighted);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            ApplyColor(highlighted);
        }

        void EnsureLine()
        {
            if (_line == null)
                _line = GetComponent<LineRenderer>();
        }

        void ApplyColor(bool highlighted)
        {
            EnsureLine();
            if (_line == null)
                return;

            Color color = highlighted ? _onColor : _offColor;
            _line.startColor = color;
            _line.endColor = color;
            _line.colorGradient = BuildGradient(color);

            if (_block == null)
                _block = new MaterialPropertyBlock();

            _line.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColId, color);
            _line.SetPropertyBlock(_block);
        }

        Gradient BuildGradient(Color color)
        {
            if (_gradient == null)
                _gradient = new Gradient();
            if (_colorKeys == null || _colorKeys.Length != 2)
                _colorKeys = new GradientColorKey[2];
            if (_alphaKeys == null || _alphaKeys.Length != 2)
                _alphaKeys = new GradientAlphaKey[2];

            _colorKeys[0] = new GradientColorKey(color, 0f);
            _colorKeys[1] = new GradientColorKey(color, 1f);
            _alphaKeys[0] = new GradientAlphaKey(color.a, 0f);
            _alphaKeys[1] = new GradientAlphaKey(color.a, 1f);
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            return _gradient;
        }
    }
}
