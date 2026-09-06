using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace M3P
{
    [DisallowMultipleComponent]
    public sealed class ProjectileVFX : MonoBehaviour
    {
        [SerializeField] TrailRenderer _trail;
        [SerializeField] float _speed = 32f;
        [SerializeField] float _arriveDistance = 0.15f;
        [SerializeField] float _arcHeight = 2.5f;
        [SerializeField] float _arcHeightJitter = 1.2f;
        [SerializeField] float _sideJitter = 1.8f;
        [FormerlySerializedAs("_minScale")]
        [SerializeField] float _minWidth = 1.15f;
        [FormerlySerializedAs("_maxScale")]
        [SerializeField] float _maxWidth = 2.6f;
        [SerializeField] float _referenceDamage = 3f;
        [SerializeField] float _heavyDamage = 18f;

        Transform _target;
        float _baseTrailWidth = 0.35f;
        Gradient _baseTrailGradient;
        ParticleSystem.MinMaxGradient[] _baseStartColors;
        Vector3 _start;
        float _arcHeightOffset;
        float _sideOffset;
        float _duration;
        float _elapsed;
        bool _arrived;
        Action _onArrived;
        ParticleSystem[] _particleSystems;

        void Awake()
        {
            if (_trail == null)
                _trail = GetComponent<TrailRenderer>();

            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (_trail != null)
            {
                _baseTrailWidth = _trail.widthMultiplier;
                _baseTrailGradient = CloneGradient(_trail.colorGradient);
            }

            if (_particleSystems == null)
                return;

            _baseStartColors = new ParticleSystem.MinMaxGradient[_particleSystems.Length];
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                    _baseStartColors[i] = _particleSystems[i].main.startColor;
            }
        }

        public void Launch(Transform target, Color color, int damage, Action onArrived = null)
        {
            _target = target;
            _onArrived = onArrived;
            _start = transform.position;
            ApplyWidth(damage);
            _arcHeightOffset = Mathf.Max(0.35f, _arcHeight + Random.Range(-_arcHeightJitter, _arcHeightJitter));
            _sideOffset = Random.Range(-_sideJitter, _sideJitter);
            _elapsed = 0f;
            _arrived = false;

            float distance = target != null
                ? Vector3.Distance(_start, target.position)
                : _speed;
            _duration = Mathf.Max(0.12f, distance / Mathf.Max(0.01f, _speed));

            ApplyColor(color);
        }

        void ApplyWidth(int damage)
        {
            float t = Mathf.InverseLerp(_referenceDamage, _heavyDamage, Mathf.Max(0, damage));
            float width = Mathf.Lerp(_minWidth, _maxWidth, t);

            if (_trail != null)
                _trail.widthMultiplier = _baseTrailWidth * width;

            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particles = _particleSystems[i];
                if (particles == null)
                    continue;

                ParticleSystem.MainModule main = particles.main;
                main.startSizeMultiplier = width;
            }
        }

        void Update()
        {
            if (_arrived)
                return;

            if (_target == null)
            {
                Arrive(transform.position);
                return;
            }

            Vector3 destination = _target.position;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            transform.position = EvaluateArc(_start, destination, t);

            if (t < 1f && (transform.position - destination).sqrMagnitude > _arriveDistance * _arriveDistance)
                return;

            Arrive(destination);
        }

        void Arrive(Vector3 destination)
        {
            _arrived = true;
            transform.position = destination;
            if (_trail != null)
                _trail.emitting = false;

            Destroy(gameObject, _trail != null ? _trail.time : 0.1f);
            Action arrived = _onArrived;
            _onArrived = null;
            arrived?.Invoke();
        }

        Vector3 EvaluateArc(Vector3 start, Vector3 end, float t)
        {
            Vector3 mid = (start + end) * 0.5f;
            Vector3 along = end - start;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;
            else
                side.Normalize();

            Vector3 control = mid + Vector3.up * _arcHeightOffset + side * _sideOffset;
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * start
                + 2f * oneMinusT * t * control
                + t * t * end;
        }

        void ApplyColor(Color color)
        {
            if (_trail != null && _baseTrailGradient != null)
                _trail.colorGradient = MultiplyGradient(_baseTrailGradient, color);

            if (_particleSystems == null || _particleSystems.Length == 0)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particles = _particleSystems[i];
                if (particles == null)
                    continue;

                ParticleSystem.MainModule main = particles.main;
                main.startColor = MultiplyMinMaxGradient(_baseStartColors[i], color);
                particles.Clear(true);
                particles.Play(true);
            }
        }

        static Gradient MultiplyGradient(Gradient source, Color color)
        {
            GradientColorKey[] colorKeys = source.colorKeys;
            GradientAlphaKey[] alphaKeys = source.alphaKeys;

            for (int i = 0; i < colorKeys.Length; i++)
                colorKeys[i].color *= color;

            for (int i = 0; i < alphaKeys.Length; i++)
                alphaKeys[i].alpha *= color.a;

            Gradient result = new Gradient { mode = source.mode };
            result.SetKeys(colorKeys, alphaKeys);
            return result;
        }

        static Gradient CloneGradient(Gradient source)
        {
            Gradient clone = new Gradient { mode = source.mode };
            clone.SetKeys(source.colorKeys, source.alphaKeys);
            return clone;
        }

        static ParticleSystem.MinMaxGradient MultiplyMinMaxGradient(
            ParticleSystem.MinMaxGradient source,
            Color color)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(source.color * color);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(source.colorMin * color, source.colorMax * color);
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(MultiplyGradient(source.gradient, color));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        MultiplyGradient(source.gradientMin, color),
                        MultiplyGradient(source.gradientMax, color));
                default:
                    return source;
            }
        }
    }
}
