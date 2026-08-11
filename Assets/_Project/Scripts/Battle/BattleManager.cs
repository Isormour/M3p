using Match3;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Match3")]
        [Tooltip("Prefab root (or child) must have a Match3Board.")]
        [SerializeField] GameObject _match3BoardPrefab;
        [Tooltip("Optional parent for spawned boards; defaults to this transform.")]
        [SerializeField] Transform _boardParent;

        [Header("Turns")]
        [SerializeField] PlayerBattleCharacter _player;
        [Tooltip("Optional transform for spawned enemies; defaults under this manager.")]
        [SerializeField] Transform _enemySpawnParent;
        [SerializeField] List<EnemyDefinition> _enemyDefinitions = new List<EnemyDefinition>();

        [Header("Cards")]
        [SerializeField] CardPlayController _cardPlay;

        [Header("World")]
        [SerializeField] BattleWorld _battleWorld;

        [Header("Flow")]
        [Tooltip("When enabled, begins a battle as soon as this component starts (scene load).")]
        [SerializeField] bool _startBattleImmediately;

        [Header("UI")]
        [SerializeField] UIEndBattlePanel _endBattlePanel;

        readonly BattleSessionRewards _sessionRewards = new BattleSessionRewards();

        Match3Board _activeBoard;
        EnemyDefinition _activeEnemyDefinition;
        EnemyBattleCharacter _activeEnemy;
        bool _isPlayerTurn = true;
        bool _battleResolved;
        BattleOutcome _lastOutcome;
        MatchRewardRules _fallbackMatchRewards;

        /// <summary>Board from the current battle, or null if no battle is running.</summary>
        public Match3Board ActiveBoard => _activeBoard;

        /// <summary>True when the human player may swap tiles on the board.</summary>
        public bool IsPlayerTurn => _isPlayerTurn;

        /// <summary>Human player for the current battle.</summary>
        public PlayerBattleCharacter Player => _player;

        /// <summary>The enemy opponent for the current battle, chosen when the battle starts.</summary>
        public EnemyBattleCharacter ActiveEnemy => _activeEnemy;

        /// <summary>Enemy data asset used for the current battle.</summary>
        public EnemyDefinition ActiveEnemyDefinition => _activeEnemyDefinition;

        /// <summary>Deck and hand for the current battle.</summary>
        public CardPlayController CardPlay => _cardPlay;
        public Action<Match3Board> OnBattleStarted;

        /// <summary>Raised for every match long enough to drop shards, at the spot it was cleared.</summary>
        public event Action<ShardDrop> ShardsEarned;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Scene-local: Map <-> Battle reloads need fresh serialized references each visit.

            if (_endBattlePanel == null)
                _endBattlePanel = FindAnyObjectByType<UIEndBattlePanel>(FindObjectsInactive.Include);
        }

        void Start()
        {
            if (Instance != this)
                return;

            if (_startBattleImmediately)
                StartBattle();
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            EndBattle();
        }

        public void ExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            if (_battleResolved || skill == null || target == null)
                return;

            skill.UseSkill(caster, target);
            NotifySkillAnimation(skill, caster);
            TryResolveBattleOutcome();
        }

        void NotifySkillAnimation(SkillDefinition skill, BattleCharacter caster)
        {
            if (_battleWorld == null || skill == null)
                return;

            if (caster == _player)
                _battleWorld.NotifyPlayerSkillUsed(skill);
            else if (caster == _activeEnemy)
                _battleWorld.NotifyEnemySkillUsed(skill);
        }

        public bool TryExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            if (skill == null || caster == null || target == null || !target.IsAlive)
                return false;

            SoftStats softStats = caster.Stats?.Soft;
            if (softStats == null || !skill.HasEnoughActionPoints(softStats) || !skill.HasEnoughMana(softStats))
                return false;

            if (!skill.TrySpendActionPoints(softStats) || !skill.TrySpendMana(softStats))
                return false;

            ExecuteSkill(skill, caster, target);

            if (caster == _player)
                EndPlayerTurnIfExhausted();

            return true;
        }

        /// <summary>Spawns the match-3 board prefab (ends any existing battle first).</summary>
        public void StartBattle()
        {
            if (_match3BoardPrefab == null)
            {
                Debug.LogError($"{nameof(BattleManager)}: assign {nameof(_match3BoardPrefab)}.", this);
                return;
            }

            EndBattle();

            _battleResolved = false;
            _endBattlePanel?.Hide();

            _player?.PrepareForBattle();

            ResolveEnemyDefinition();
            _sessionRewards.Begin(_activeEnemyDefinition);
            SpawnEnemyBattleCharacter();

            Transform parent = _boardParent != null ? _boardParent : transform;
            GameObject instance = Instantiate(_match3BoardPrefab, parent);
            // Keep board at parent origin. Parent should sit where the camera can see (near world origin is typical).
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _activeBoard = instance.GetComponent<Match3Board>();
            if (_activeBoard == null)
                _activeBoard = instance.GetComponentInChildren<Match3Board>(true);

            if (_activeBoard == null)
            {
                Debug.LogError($"{nameof(BattleManager)}: prefab must include {nameof(Match3Board)}.", this);
                Destroy(instance);
                ClearSpawnedEnemy();
                return;
            }
            _cardPlay?.BeginBattle(_activeBoard, _player);
            OnBattleStarted?.Invoke(_activeBoard);
            StartCoroutine(SetupTurnFlowRoutine());
        }

        void ResolveEnemyDefinition()
        {
            // Map encounters win: the enemy on EncounterConfig is authoritative for that fight.
            EnemyDefinition fromMap = MapRunState.Active != null ? MapRunState.Active.PendingEnemy : null;
            if (fromMap != null)
            {
                _activeEnemyDefinition = fromMap;
                return;
            }

            PickRandomEnemyDefinition();
        }

        void PickRandomEnemyDefinition()
        {
            _activeEnemyDefinition = null;

            if (_enemyDefinitions == null || _enemyDefinitions.Count == 0)
                return;

            int seen = 0;
            EnemyDefinition pick = null;
            for (int i = 0; i < _enemyDefinitions.Count; i++)
            {
                EnemyDefinition candidate = _enemyDefinitions[i];
                if (candidate == null)
                    continue;

                seen++;
                if (UnityEngine.Random.Range(0, seen) == 0)
                    pick = candidate;
            }

            _activeEnemyDefinition = pick;
        }

        void SpawnEnemyBattleCharacter()
        {
            _activeEnemy = null;

            if (_activeEnemyDefinition == null)
                return;

            EnemyBattleCharacter prefab = _activeEnemyDefinition.EnemyCharacterPrefab;

            if (prefab == null)
            {
                Debug.LogError(
                    $"{nameof(BattleManager)}: set {nameof(EnemyDefinition.EnemyCharacterPrefab)} on {_activeEnemyDefinition.name}.",
                    this);
                _activeEnemyDefinition = null;
                return;
            }

            Transform parent = _enemySpawnParent != null ? _enemySpawnParent : transform;
            EnemyBattleCharacter spawned = Instantiate(prefab, parent);
            spawned.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            spawned.Configure(_activeEnemyDefinition);
            _activeEnemy = spawned;

            _battleWorld?.SpawnEnemyModel(_activeEnemyDefinition.EnemyModelPrefab);
        }

        IEnumerator SetupTurnFlowRoutine()
        {
            yield return null;

            if (_activeBoard == null)
                yield break;

            _activeBoard.BoardActionResolved += HandleBoardActionResolved;
            _activeBoard.MatchWaveCompleted += HandleMatchWaveCompleted;
            BeginPlayerTurn();
        }

        /// <summary>
        /// Resolves one basic attack per match group, so a cascade lands several separate hits while a
        /// single long line lands one bigger hit. Shards from the same wave are set aside for the win.
        /// </summary>
        void HandleMatchWaveCompleted(IReadOnlyList<MatchGroup> groups)
        {
            if (_battleResolved || !_isPlayerTurn || _activeEnemy == null || groups == null || groups.Count == 0)
                return;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return;

            MatchRewardRules matchRewards = ResolveMatchRewards(config);
            HardStats attacker = _player?.Stats != null ? _player.Stats.Hard : default;
            TalentBonuses talents = _player?.Stats?.TalentBonuses ?? TalentBonuses.None;
            SoftStats targetStats = _activeEnemy.Stats?.Soft;
            int tilesDestroyed = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                MatchGroup group = groups[i];
                tilesDestroyed += group.Size;
                targetStats?.TakeDamage(config.CalculateBasicAttackDamage(attacker, group.Size, talents));

                int shards = matchRewards.GetShardsForMatch(group.Size);
                if (shards <= 0)
                    continue;

                _sessionRewards.AddShards(group.TypeId, shards);
                ShardsEarned?.Invoke(new ShardDrop(group.Center, group.TypeId, shards));
            }

            _battleWorld?.NotifyMatchWave(tilesDestroyed);
            TryResolveBattleOutcome();
        }

        MatchRewardRules ResolveMatchRewards(GameConfig config)
        {
            if (config != null && config.MatchRewards != null)
                return config.MatchRewards;

            return _fallbackMatchRewards ??= MatchRewardRules.CreateDefault();
        }

        /// <summary>
        /// A card no longer ends the turn on its own. The turn runs until the player is out of action
        /// points, out of affordable cards and skills, or chooses to stop.
        /// </summary>
        void HandleBoardActionResolved()
        {
            if (_battleResolved)
                return;

            EndPlayerTurnIfExhausted();
        }

        void EndPlayerTurnIfExhausted()
        {
            if (_battleResolved || !_isPlayerTurn || _activeBoard == null || PlayerHasLegalAction())
                return;

            EndPlayerTurn();
        }

        /// <summary>Ends the turn on the player's request, banking nothing for unspent action points.</summary>
        public void RequestEndTurn()
        {
            if (_battleResolved || !_isPlayerTurn || _activeBoard == null || _activeBoard.IsResolving)
                return;

            EndPlayerTurn();
        }

        bool PlayerHasLegalAction()
        {
            if (_cardPlay != null && _cardPlay.HasPlayableCard())
                return true;

            return HasCastableSkill();
        }

        bool HasCastableSkill()
        {
            SoftStats softStats = _player?.Stats?.Soft;
            IReadOnlyList<SkillDefinition> skills = _player?.Skills;

            if (softStats == null || skills == null || _activeEnemy == null || !_activeEnemy.IsAlive)
                return false;

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill != null && skill.HasEnoughActionPoints(softStats) && skill.HasEnoughMana(softStats))
                    return true;
            }

            return false;
        }

        void BeginPlayerTurn()
        {
            if (_battleResolved)
                return;

            _isPlayerTurn = true;

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = true;

            _player?.OnTurnStarted();
            _cardPlay?.BeginTurn();
        }

        void EndPlayerTurn()
        {
            if (_battleResolved)
                return;

            _isPlayerTurn = false;

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = false;

            _cardPlay?.EndTurn();

            if (_activeEnemy == null)
            {
                Debug.LogWarning(
                    $"{nameof(BattleManager)}: ensure {nameof(_enemyDefinitions)} are set and each has {nameof(EnemyDefinition.EnemyCharacterPrefab)}.",
                    this);
                BeginPlayerTurn();
                return;
            }

            StartCoroutine(RunEnemyTurnRoutine());
        }

        IEnumerator RunEnemyTurnRoutine()
        {
            yield return _activeEnemy.PlayTurn(_activeBoard);
            TryResolveBattleOutcome();

            if (_battleResolved || _activeBoard == null || _isPlayerTurn)
                yield break;

            while (_activeBoard.IsResolving)
                yield return null;

            BeginPlayerTurn();
        }

        void TryResolveBattleOutcome()
        {
            if (_battleResolved || _activeBoard == null)
                return;

            if (_player != null && !_player.IsAlive)
            {
                ResolveBattle(BattleOutcome.Lose);
                return;
            }

            if (_activeEnemy != null && !_activeEnemy.IsAlive)
                ResolveBattle(BattleOutcome.Win);
        }

        void ResolveBattle(BattleOutcome outcome)
        {
            if (_battleResolved)
                return;

            _battleResolved = true;
            _lastOutcome = outcome;
            StopAllCoroutines();

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = false;

            _endBattlePanel?.Show(outcome, GrantBattleRewards(outcome));
        }

        /// <summary>
        /// Banks the payout for a finished battle. Only a win rewards anything, so nothing is gained by
        /// stretching out a fight that is already lost.
        /// </summary>
        BattleRewardResult GrantBattleRewards(BattleOutcome outcome)
        {
            ProgressionService progression = GameManager.Instance != null ? GameManager.Instance.Progression : null;
            if (progression == null || outcome != BattleOutcome.Win)
                return BattleRewardResult.None;

            return progression.ApplyBattleRewards(_sessionRewards.Experience, _sessionRewards.ShardsByTileType);
        }

        /// <summary>Called when the player closes the end-of-battle panel.</summary>
        public void DismissEndBattlePanel()
        {
            if (!_battleResolved)
            {
                _endBattlePanel?.Hide();
                return;
            }

            MapRunState mapRun = MapRunState.Active;
            bool returnToMap = mapRun != null && mapRun.HasPendingBattle;
            if (returnToMap)
                mapRun.ResolveBattle(_lastOutcome == BattleOutcome.Win);

            EndBattle();

            if (returnToMap)
                SceneFlow.LoadMap();
        }

        /// <summary>Destroys the board created by <see cref="StartBattle"/>.</summary>
        public void EndBattle()
        {
            StopAllCoroutines();
            _battleResolved = false;
            _endBattlePanel?.Hide();

            _cardPlay?.EndBattle();

            if (_activeBoard != null)
            {
                _activeBoard.BoardActionResolved -= HandleBoardActionResolved;
                _activeBoard.MatchWaveCompleted -= HandleMatchWaveCompleted;
                Destroy(_activeBoard.gameObject);
                _activeBoard = null;
            }

            _isPlayerTurn = true;

            ClearSpawnedEnemy();
        }

        void ClearSpawnedEnemy()
        {
            _battleWorld?.ClearEnemyModel();

            if (_activeEnemy != null)
            {
                Destroy(_activeEnemy.gameObject);
                _activeEnemy = null;
            }

            _activeEnemyDefinition = null;
        }
    }
}
