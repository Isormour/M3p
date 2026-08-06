using Match3;
using UnityEngine;

namespace M3P
{
    public sealed class VFXManager : MonoBehaviour
    {
        [SerializeField] GameObject _particlePrefab;
        [SerializeField] BattleManager _battleManager;
        [SerializeField] Transform _particleTarget;
        [SerializeField] float _vfxLifetime = 2f;

        Match3Board _board;

        void Awake()
        {
            if (_battleManager == null)
                _battleManager = BattleManager.Instance;
        }

        void OnEnable()
        {
            if (_battleManager != null)
                _battleManager.OnBattleStarted += HandleBattleStarted;

            if (_battleManager?.ActiveBoard != null)
                BindBoard(_battleManager.ActiveBoard);
        }

        void OnDisable()
        {
            if (_battleManager != null)
                _battleManager.OnBattleStarted -= HandleBattleStarted;

            UnbindBoard();
        }

        void HandleBattleStarted(Match3Board board)
        {
            BindBoard(board);
        }

        void BindBoard(Match3Board board)
        {
            UnbindBoard();

            if (board == null)
                return;

            _board = board;
            _board.TileDestroyed += HandleTileDestroyed;
        }

        void UnbindBoard()
        {
            if (_board != null)
                _board.TileDestroyed -= HandleTileDestroyed;

            _board = null;
        }

        void HandleTileDestroyed(Vector3 worldPosition, int typeId)
        {
            if (_particlePrefab == null)
                return;

            GameObject instance = Instantiate(_particlePrefab, worldPosition, Quaternion.identity);
            ParticleAttractor attractor = instance.GetComponent<ParticleAttractor>();
            if (attractor == null)
                attractor = instance.GetComponentInChildren<ParticleAttractor>();

            Transform particleTarget = _particleTarget;
            if (attractor != null && particleTarget != null)
                attractor.SetTarget(particleTarget);

            if (_board != null)
            {
                ParticleSystem particles = instance.GetComponent<ParticleSystem>();
                if (particles == null)
                    particles = instance.GetComponentInChildren<ParticleSystem>();

                if (particles != null)
                {
                    ParticleSystem.MainModule main = particles.main;
                    main.startColor = _board.GetTileTypeColor(typeId);
                }
            }

            Destroy(instance, _vfxLifetime);
        }
    }
}
