using System;
using UnityEngine;
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

        Transform _target;
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
        }

        public void Launch(Transform target, Color color, Action onArrived = null)
        {
            _target = target;
            _onArrived = onArrived;
            _start = transform.position;
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
            if (_trail != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                    new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
                _trail.colorGradient = gradient;
            }

            if (_particleSystems == null || _particleSystems.Length == 0)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particles = _particleSystems[i];
                if (particles == null)
                    continue;

                ParticleSystem.MainModule main = particles.main;
                main.startColor = color;
                particles.Clear(true);
                particles.Play(true);
            }
        }
    }
}
