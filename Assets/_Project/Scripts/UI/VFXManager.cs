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

        [Header("Shards")]
        [Tooltip("Played where a match long enough to drop shards was cleared.")]
        [SerializeField] ShardVFX _shardPrefab;
        [Tooltip("Where shards fly to, typically the shard counter. Defaults to the tile particle target.")]
        [SerializeField] Transform _shardTarget;
        [SerializeField] float _shardVfxLifetime = 2f;

        Match3Board _board;

        void Awake()
        {
            if (_battleManager == null)
                _battleManager = BattleManager.Instance;
        }

        void OnEnable()
        {
            if (_battleManager != null)
            {
                _battleManager.OnBattleStarted += HandleBattleStarted;
                _battleManager.ShardsEarned += HandleShardsEarned;
            }

            if (_battleManager?.ActiveBoard != null)
                BindBoard(_battleManager.ActiveBoard);
        }

        void OnDisable()
        {
            if (_battleManager != null)
            {
                _battleManager.OnBattleStarted -= HandleBattleStarted;
                _battleManager.ShardsEarned -= HandleShardsEarned;
            }

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
            BindAttractor(instance, _particleTarget);

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

        void HandleShardsEarned(ShardDrop drop)
        {
            if (_shardPrefab == null)
                return;

            ShardVFX instance = Instantiate(_shardPrefab, drop.WorldPosition, Quaternion.identity);

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            instance.Setup(config != null ? config.GetTileType(drop.TileTypeId) : null, drop.Amount);

            BindAttractor(instance.gameObject, _shardTarget != null ? _shardTarget : _particleTarget);

            Destroy(instance.gameObject, _shardVfxLifetime);
        }

        static void BindAttractor(GameObject instance, Transform target)
        {
            if (target == null)
                return;

            ParticleAttractor attractor = instance.GetComponent<ParticleAttractor>();
            if (attractor == null)
                attractor = instance.GetComponentInChildren<ParticleAttractor>();

            if (attractor != null)
                attractor.SetTarget(target);
        }
    }
}
