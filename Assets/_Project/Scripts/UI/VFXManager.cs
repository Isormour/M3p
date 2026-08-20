using Match3;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    public sealed class VFXManager : MonoBehaviour
    {
        [FormerlySerializedAs("_particlePrefab")]
        [SerializeField] GameObject _projectilePrefab;
        [SerializeField] BattleManager _battleManager;
        [Tooltip("Where player-origin attack projectiles spawn, and where enemy attacks fly to.")]
        [SerializeField] Transform _playerVfxPoint;
        [Tooltip("Where enemy-origin attack projectiles spawn, and where player attacks fly to.")]
        [SerializeField] Transform _particleTarget;
        [SerializeField] float _vfxLifetime = 2f;

        [Header("Tile Destroy")]
        [SerializeField] GameObject _destroyParticlePrefab;
        [SerializeField] float _destroyVfxLifetime = 2f;

        [Header("Shards")]
        [Tooltip("Played where a match long enough to drop shards was cleared.")]
        [SerializeField] ShardVFX _shardPrefab;
        [Tooltip("Where shards fly to, typically the shard counter. Defaults to the tile particle target.")]
        [SerializeField] Transform _shardTarget;
        [SerializeField] float _shardVfxLifetime = 2f;

        [Header("Battle Indicators")]
        [SerializeField] CascadeIndicator _cascadeIndicator;
        [SerializeField] SuperMatchIndicator _superMatchIndicator;
        [SerializeField] int _superMatchMinSize = 4;

        [Header("Tile Move")]
        [Tooltip("Arc drawn from a tile's current cell to the cell a queued card will move it to.")]
        [SerializeField] TileMoveIndicator _tileMoveIndicatorPrefab;

        Match3Board _board;
        CardPlayController _cardPlay;
        readonly List<TileMoveIndicator> _moveIndicators = new List<TileMoveIndicator>();
        readonly Dictionary<int, Vector2Int> _predictedCellsByTileId = new Dictionary<int, Vector2Int>();
        int _matchWaveIndex;

        void Awake()
        {
            if (_battleManager == null)
                _battleManager = BattleManager.Instance;

            if (_cascadeIndicator == null)
                _cascadeIndicator = FindAnyObjectByType<CascadeIndicator>(FindObjectsInactive.Include);

            if (_superMatchIndicator == null)
                _superMatchIndicator = FindAnyObjectByType<SuperMatchIndicator>(FindObjectsInactive.Include);
        }

        void Start()
        {
            _cascadeIndicator?.Hide();
            _superMatchIndicator?.Hide();
        }

        void OnEnable()
        {
            if (_battleManager != null)
            {
                _battleManager.OnBattleStarted += HandleBattleStarted;
                _battleManager.ShardsEarned += HandleShardsEarned;
                _battleManager.SkillExecuted += HandleSkillExecuted;
            }

            if (_battleManager?.ActiveBoard != null)
                BindBoard(_battleManager.ActiveBoard);

            BindCardPlay();
            RefreshMoveIndicators();
        }

        void OnDisable()
        {
            if (_battleManager != null)
            {
                _battleManager.OnBattleStarted -= HandleBattleStarted;
                _battleManager.ShardsEarned -= HandleShardsEarned;
                _battleManager.SkillExecuted -= HandleSkillExecuted;
            }

            UnbindCardPlay();
            UnbindBoard();
        }

        void HandleBattleStarted(Match3Board board)
        {
            BindBoard(board);
            BindCardPlay();
            RefreshMoveIndicators();
        }

        void BindBoard(Match3Board board)
        {
            UnbindBoard();

            if (board == null)
                return;

            _board = board;
            _matchWaveIndex = 0;
            _board.TileDestroyed += HandleTileDestroyed;
            _board.MatchWaveCompleted += HandleMatchWaveCompleted;
            _board.SequenceResolved += HandleSequenceResolved;
        }

        void UnbindBoard()
        {
            if (_board != null)
            {
                _board.TileDestroyed -= HandleTileDestroyed;
                _board.MatchWaveCompleted -= HandleMatchWaveCompleted;
                _board.SequenceResolved -= HandleSequenceResolved;
            }

            _board = null;
            _matchWaveIndex = 0;
            _cascadeIndicator?.Hide();
            _superMatchIndicator?.Hide();
            HideMoveIndicators();
        }

        void BindCardPlay()
        {
            CardPlayController cardPlay = _battleManager != null ? _battleManager.CardPlay : null;
            if (cardPlay == _cardPlay)
                return;

            UnbindCardPlay();
            _cardPlay = cardPlay;
            if (_cardPlay != null)
                _cardPlay.Changed += HandleCardPlayChanged;
        }

        void UnbindCardPlay()
        {
            if (_cardPlay != null)
                _cardPlay.Changed -= HandleCardPlayChanged;

            _cardPlay = null;
            HideMoveIndicators();
        }

        void HandleCardPlayChanged()
        {
            RefreshMoveIndicators();
        }

        void RefreshMoveIndicators()
        {
            int used = 0;
            SimBoard predicted = _cardPlay != null ? _cardPlay.PredictedBoard : null;
            bool planning = _board != null
                && predicted != null
                && !_board.IsResolving
                && _cardPlay.HasQueuedCards
                && _tileMoveIndicatorPrefab != null;

            if (planning)
            {
                _predictedCellsByTileId.Clear();
                for (int x = 0; x < predicted.Width; x++)
                {
                    for (int y = 0; y < predicted.Height; y++)
                    {
                        SimTile tile = predicted.GetTile(x, y);
                        if (tile != null)
                            _predictedCellsByTileId[tile.Id] = new Vector2Int(x, y);
                    }
                }

                for (int x = 0; x < _board.Width; x++)
                {
                    for (int y = 0; y < _board.Height; y++)
                    {
                        Match3Tile actual = _board.GetTile(x, y);
                        if (actual == null)
                            continue;

                        if (!_predictedCellsByTileId.TryGetValue(actual.TileId, out Vector2Int destination))
                            continue;

                        if (destination.x == x && destination.y == y)
                            continue;

                        TileMoveIndicator indicator = RentMoveIndicator(used++);
                        indicator.Present(
                            _board.GridToWorld(x, y),
                            _board.GridToWorld(destination.x, destination.y),
                            _board.GetTileTypeColor(actual.TypeId));
                    }
                }
            }

            HideUnusedMoveIndicators(used);
        }

        TileMoveIndicator RentMoveIndicator(int index)
        {
            while (_moveIndicators.Count <= index)
                _moveIndicators.Add(null);

            TileMoveIndicator existing = _moveIndicators[index];
            if (existing != null)
                return existing;

            TileMoveIndicator created = Instantiate(_tileMoveIndicatorPrefab, transform);
            _moveIndicators[index] = created;
            return created;
        }

        void HideUnusedMoveIndicators(int used)
        {
            for (int i = used; i < _moveIndicators.Count; i++)
            {
                if (_moveIndicators[i] != null)
                    _moveIndicators[i].Hide();
            }
        }

        void HideMoveIndicators()
        {
            HideUnusedMoveIndicators(0);
        }

        void HandleTileDestroyed(Vector3 worldPosition, int typeId)
        {
            SpawnDestroyParticle(worldPosition, typeId);
        }

        void HandleMatchWaveCompleted(IReadOnlyList<MatchGroup> groups)
        {
            if (groups == null || groups.Count == 0)
                return;

            _matchWaveIndex++;
            UpdateBattleIndicators(groups);

            Transform origin = GetPlayerVfxPoint();
            Transform destination = GetEnemyVfxPoint();
            BattleWorld world = _battleManager != null ? _battleManager.BattleWorld : null;

            for (int i = 0; i < groups.Count; i++)
            {
                bool isLast = i == groups.Count - 1;
                SpawnAttackProjectile(origin, destination, groups[i].TypeId, () =>
                {
                    bool died = isLast
                        && _battleManager?.ActiveEnemy != null
                        && !_battleManager.ActiveEnemy.IsAlive;
                    world?.NotifyEnemyHit(died);
                    PulseBattleIndicators();
                });
            }
        }

        void HandleSequenceResolved(ResolveReport report)
        {
            _matchWaveIndex = 0;
            HideMoveIndicators();
        }

        void UpdateBattleIndicators(IReadOnlyList<MatchGroup> groups)
        {
            if (_matchWaveIndex >= 2)
                _cascadeIndicator?.Present(_matchWaveIndex - 1);

            int largestSize = GetLargestMatchSize(groups);
            if (largestSize < _superMatchMinSize)
                return;

            if (_superMatchIndicator != null
                && _superMatchIndicator.IsShowing
                && largestSize <= _superMatchIndicator.CurrentAmount)
                return;

            _superMatchIndicator?.Present(largestSize);
        }

        void PulseBattleIndicators()
        {
            if (_cascadeIndicator != null && _cascadeIndicator.IsShowing)
                _cascadeIndicator.Pulse();

            if (_superMatchIndicator != null && _superMatchIndicator.IsShowing)
                _superMatchIndicator.Pulse();
        }

        static int GetLargestMatchSize(IReadOnlyList<MatchGroup> groups)
        {
            int size = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Size > size)
                    size = groups[i].Size;
            }

            return size;
        }

        void HandleSkillExecuted(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            if (skill == null || caster == null || !skill.AffectsOpponent())
                return;

            Transform origin;
            Transform destination;
            if (caster.IsPlayerControlled)
            {
                origin = GetPlayerVfxPoint();
                destination = GetEnemyVfxPoint();
            }
            else
            {
                origin = GetEnemyVfxPoint();
                destination = GetPlayerVfxPoint();
            }

            BattleWorld world = _battleManager != null ? _battleManager.BattleWorld : null;
            bool hitPlayer = target != null && target.IsPlayerControlled;
            SpawnAttackProjectile(origin, destination, onArrived: () =>
            {
                bool died = target != null && !target.IsAlive;
                if (hitPlayer)
                    world?.NotifyPlayerHit(died);
                else
                    world?.NotifyEnemyHit(died);
            });
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

        void SpawnDestroyParticle(Vector3 worldPosition, int typeId)
        {
            if (_destroyParticlePrefab == null)
                return;

            GameObject instance = Instantiate(_destroyParticlePrefab, worldPosition, Quaternion.identity);
            ApplyTileColor(instance, typeId);
            Destroy(instance, _destroyVfxLifetime);
        }

        void SpawnAttackProjectile(Transform origin, Transform destination, int typeId = -1, Action onArrived = null)
        {
            if (_projectilePrefab == null || origin == null || destination == null)
            {
                onArrived?.Invoke();
                return;
            }

            GameObject instance = Instantiate(_projectilePrefab, origin.position, Quaternion.identity);
            ProjectileVFX projectile = instance.GetComponent<ProjectileVFX>();
            Color color = typeId >= 0 && _board != null
                ? _board.GetTileTypeColor(typeId)
                : Color.white;

            if (projectile != null)
            {
                projectile.Launch(destination, color, onArrived);
                return;
            }

            BindAttractor(instance, destination);
            ApplyTileColor(instance, typeId);
            Destroy(instance, _vfxLifetime);
            onArrived?.Invoke();
        }

        void ApplyTileColor(GameObject instance, int typeId)
        {
            if (typeId < 0 || _board == null)
                return;

            ParticleSystem particles = instance.GetComponent<ParticleSystem>();
            if (particles == null)
                particles = instance.GetComponentInChildren<ParticleSystem>();

            if (particles == null)
                return;

            ParticleSystem.MainModule main = particles.main;
            main.startColor = _board.GetTileTypeColor(typeId);
        }

        Transform GetPlayerVfxPoint()
        {
            if (_playerVfxPoint != null)
                return _playerVfxPoint;

            return _battleManager != null ? _battleManager.BattleWorld?.PlayerVfxPoint : null;
        }

        Transform GetEnemyVfxPoint()
        {
            if (_particleTarget != null)
                return _particleTarget;

            return _battleManager != null ? _battleManager.BattleWorld?.EnemyVfxPoint : null;
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
