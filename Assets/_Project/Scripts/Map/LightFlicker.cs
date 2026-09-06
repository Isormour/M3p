using UnityEngine;

namespace M3P
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField] Light _light;
        [SerializeField] Color _colorA = new Color(1f, 0.55f, 0.2f, 1f);
        [SerializeField] Color _colorB = new Color(1f, 0.25f, 0.05f, 1f);
        [SerializeField, Min(0f)] float _intensityA = 1f;
        [SerializeField, Min(0f)] float _intensityB = 0.4f;
        [SerializeField, Min(0f)] float _speed = 1f;
        [SerializeField] bool _randomizeOffset = true;

        float _offset;

        void Awake()
        {
            EnsureLight();
            if (_randomizeOffset)
                _offset = Random.value * 1000f;
        }

        void Update()
        {
            EnsureLight();
            if (_light == null)
                return;

            float t = Mathf.PerlinNoise(Time.time * _speed + _offset, _offset);
            _light.color = Color.Lerp(_colorA, _colorB, t);
            _light.intensity = Mathf.Lerp(_intensityA, _intensityB, t);
        }

        void Reset()
        {
            _light = GetComponent<Light>();
        }

        void EnsureLight()
        {
            if (_light == null)
                _light = GetComponent<Light>();
        }
    }
}
