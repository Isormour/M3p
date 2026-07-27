using UnityEngine;

namespace M3P
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleAttractor : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Vector3 _targetPosition;
        [SerializeField] bool _useTransformTarget = true;
        [SerializeField] float _attractStrength = 8f;
        [SerializeField] float _maxSpeed = 12f;
        [SerializeField] float _stopDistance = 0.05f;
        [SerializeField] float _delayBeforeAttract = 0f;

        ParticleSystem _particleSystem;
        ParticleSystem.Particle[] _particles;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        void LateUpdate()
        {
            if (_particleSystem == null)
                return;

            int particleCount = _particleSystem.particleCount;
            if (particleCount <= 0)
                return;

            if (_particles == null || _particles.Length < _particleSystem.main.maxParticles)
                _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];

            particleCount = _particleSystem.GetParticles(_particles);
            if (particleCount <= 0)
                return;

            ParticleSystem.MainModule main = _particleSystem.main;
            Vector3 targetPosition = GetTargetWorldPosition();
            if (main.simulationSpace == ParticleSystemSimulationSpace.Local)
                targetPosition = _particleSystem.transform.InverseTransformPoint(targetPosition);

            float deltaTime = Time.deltaTime;

            for (int i = 0; i < particleCount; i++)
            {
                ParticleSystem.Particle particle = _particles[i];

                if (_delayBeforeAttract > 0f)
                {
                    float age = particle.startLifetime - particle.remainingLifetime;
                    if (age < _delayBeforeAttract)
                        continue;
                }

                Vector3 toTarget = targetPosition - particle.position;
                float distance = toTarget.magnitude;
                if (distance <= _stopDistance)
                    continue;

                Vector3 direction = toTarget / distance;
                Vector3 velocity = particle.velocity + direction * (_attractStrength * deltaTime);

                float speed = velocity.magnitude;
                if (speed > _maxSpeed)
                    velocity = velocity / speed * _maxSpeed;

                particle.velocity = velocity;
                _particles[i] = particle;
            }

            _particleSystem.SetParticles(_particles, particleCount);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _useTransformTarget = target != null;
        }

        public void SetTargetPosition(Vector3 worldPosition)
        {
            _targetPosition = worldPosition;
            _useTransformTarget = false;
        }

        Vector3 GetTargetWorldPosition()
        {
            if (_useTransformTarget && _target != null)
                return _target.position;

            return _targetPosition;
        }
    }
}
