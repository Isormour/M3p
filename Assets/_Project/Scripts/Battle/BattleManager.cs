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
        [Tooltip("Shown after a boss win. Closing it generates the next floor and returns to Map.")]
        [SerializeField] UIPanelGainSkill _gainSkillPanel;

        readonly BattleSessionRewards _sessionRewards = new BattleSessionRewards();
        readonly ResolveLimits _resolveLimits = new ResolveLimits();
        readonly TurnReport _turnReport = new TurnReport();

        Match3Board _activeBoard;
        EnemyDefinition _activeEnemyDefinition;
        EnemyRuntimeSpec _activeEnemySpec;
        EnemyBattleCharacter _activeEnemy;
        bool _isPlayerTurn = true;
        bool _battleResolved;
        bool _awaitingSkillChoice;
        bool _awaitingBossSkillReward;
        BattleOutcome _lastOutcome;
        MatchRewardRules _fallbackMatchRewards;

        /// <summary>Board from the current battle, or null if no battle is running.</summary>
        public Match3Board ActiveBoard => _activeBoard;

        /// <summary>True when the human player may swap tiles on the board.</summary>
        public bool IsPlayerTurn => _isPlayerTurn;

        /// <summary>True while Recycle or Transmute is waiting on a UI choice.</summary>
        public bool IsAwaitingSkillChoice => _awaitingSkillChoice;

        /// <summary>Human player for the current battle.</summary>
        public PlayerBattleCharacter Player => _player;

        /// <summary>The enemy opponent for the current battle, chosen when the battle starts.</summary>
        public EnemyBattleCharacter ActiveEnemy => _activeEnemy;

        /// <summary>Enemy data asset used for the current battle.</summary>
        public EnemyDefinition ActiveEnemyDefinition => _activeEnemyDefinition;

        /// <summary>Floor- and encounter-scaled enemy used for the current battle.</summary>
        public EnemyRuntimeSpec ActiveEnemySpec => _activeEnemySpec;

        /// <summary>Deck and hand for the current battle.</summary>
        public CardPlayController CardPlay => _cardPlay;

        /// <summary>
        /// Per-Resolve budgets for card draws and burn stacks, plus a running stamina-refund total.
        /// Stamina from tile upgrades is not gated here.
        /// </summary>
        public ResolveLimits ResolveLimits => _resolveLimits;

        /// <summary>Running summary of the player's turn across every sequence it contains.</summary>
        public TurnReport TurnReport => _turnReport;

        public Action<Match3Board> OnBattleStarted;

        /// <summary>Raised after a skill is applied, including enemy turns.</summary>
        public event Action<SkillDefinition, BattleCharacter, BattleCharacter> SkillExecuted;

        /// <summary>Raised for every match long enough to drop shards, at the spot it was cleared.</summary>
        public event Action<ShardDrop> ShardsEarned;

        public BattleWorld BattleWorld => _battleWorld;

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

            if (_gainSkillPanel == null)
                _gainSkillPanel = FindAnyObjectByType<UIPanelGainSkill>(FindObjectsInactive.Include);
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

            UnsubscribeBossSkillPanel();
            Instance = null;
            EndBattle(hideEndPanel: false);
        }

        public void ExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            ExecuteSkill(skill, caster, target, default);
        }

        public void ExecuteSkill(
            SkillDefinition skill,
            BattleCharacter caster,
            BattleCharacter target,
            SkillCastChoice choice)
        {
            if (_battleResolved || skill == null || target == null)
                return;

            skill.UseSkill(caster, target, choice);
            NotifySkillAnimation(skill, caster);
            SkillExecuted?.Invoke(skill, caster, target);
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

        static void SnapshotVitals(BattleCharacter character, out int health, out int shield)
        {
            SoftStats soft = character?.Stats?.Soft;
            health = soft != null ? soft.CurrentHealth : 0;
            shield = soft != null ? soft.CurrentShield : 0;
        }

        void NotifyHitReaction(BattleCharacter character, int healthBefore, int shieldBefore)
        {
            if (_battleWorld == null || character == null)
                return;

            SoftStats soft = character.Stats?.Soft;
            if (soft == null)
                return;

            if (soft.CurrentHealth >= healthBefore && soft.CurrentShield >= shieldBefore)
                return;

            bool died = !character.IsAlive;
            if (character == _player)
                _battleWorld.NotifyPlayerHit(died);
            else if (character == _activeEnemy)
                _battleWorld.NotifyEnemyHit(died);
        }

        public bool TryExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            return TryExecuteSkill(skill, caster, target, default);
        }

        public bool TryExecuteSkill(
            SkillDefinition skill,
            BattleCharacter caster,
            BattleCharacter target,
            SkillCastChoice choice)
        {
            if (skill == null || caster == null || target == null || !target.IsAlive)
                return false;

            if (_awaitingSkillChoice)
                return false;

            if (!caster.IsSkillReady(skill))
                return false;

            if (!skill.MeetsCastRequirements(caster, target))
                return false;

            SoftStats softStats = caster.Stats?.Soft;
            if (softStats == null || !skill.HasEnoughActionPoints(softStats) || !skill.HasEnoughMana(softStats))
                return false;

            if (!skill.TrySpendActionPoints(softStats) || !skill.TrySpendMana(softStats))
                return false;

            ExecuteSkill(skill, caster, target, choice);
            caster.StartSkillCooldown(skill);

            if (caster == _player)
                EndPlayerTurnIfExhausted();

            return true;
        }

        public void BeginSkillChoice()
        {
            _awaitingSkillChoice = true;
        }

        public void CancelSkillChoice()
        {
            _awaitingSkillChoice = false;
        }

        /// <summary>Spends 1 AP to reduce the remaining cooldown of a player skill by 1.</summary>
        public bool TryReduceSkillCooldown(SkillDefinition skill, PlayerBattleCharacter player)
        {
            if (_battleResolved || !_isPlayerTurn || skill == null || player == null || player != _player)
                return false;

            if (!player.TryReduceSkillCooldownWithActionPoint(skill))
                return false;

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
            HideEndBattlePanel();

            _player?.PrepareForBattle();

            ResolveEnemyDefinition();
            _sessionRewards.Begin(_activeEnemySpec);
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
            _activeEnemySpec = null;

            // Map encounters win: the enemy on EncounterConfig is authoritative for that fight.
            MapRunState mapRun = MapRunState.Active;
            EnemyDefinition fromMap = mapRun != null ? mapRun.PendingEnemy : null;
            if (fromMap != null)
            {
                _activeEnemyDefinition = fromMap;
                _activeEnemySpec = EnemyProgressionResolver.Resolve(
                    fromMap,
                    mapRun.FloorIndex,
                    mapRun.PendingEncounterType);
                return;
            }

            PickRandomEnemyDefinition();
            if (_activeEnemyDefinition != null)
            {
                _activeEnemySpec = EnemyProgressionResolver.Resolve(
                    _activeEnemyDefinition,
                    1,
                    MapNodeType.Battle);
            }
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
                _activeEnemySpec = null;
                return;
            }

            Transform parent = _enemySpawnParent != null ? _enemySpawnParent : transform;
            EnemyBattleCharacter spawned = Instantiate(prefab, parent);
            spawned.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            spawned.Configure(_activeEnemySpec ?? EnemyProgressionResolver.Resolve(
                _activeEnemyDefinition,
                1,
                MapNodeType.Battle));
            _activeEnemy = spawned;

            _battleWorld?.SpawnEnemyModel(_activeEnemyDefinition.EnemyModelPrefab);
        }

        IEnumerator SetupTurnFlowRoutine()
        {
            yield return null;

            if (_activeBoard == null)
                yield break;

            _activeBoard.SequenceResolved += HandleSequenceResolved;
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
            HardStats attacker = _player != null ? _player.GetEffectiveHard() : default;
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
        /// Runs the queued sequence. Resolve ends the sequence, not the turn: the player gets control back
        /// afterwards and can spend stamina the payout handed them on another sequence or a skill.
        /// </summary>
        public void RequestResolve()
        {
            if (_battleResolved || !_isPlayerTurn || _activeBoard == null || _cardPlay == null)
                return;

            if (!_cardPlay.CanResolve())
                return;

            StartCoroutine(ResolveSequenceRoutine(endTurnAfterwards: false));
        }

        /// <summary>
        /// Ends the turn on the player's request. A queue that is still standing resolves first, so no
        /// stamina already committed to cards is silently thrown away — but stamina the payout returns is
        /// lost, which is the cost of ending on a full queue.
        /// </summary>
        public void RequestEndTurn()
        {
            if (_battleResolved || !_isPlayerTurn || _activeBoard == null || _activeBoard.IsResolving)
                return;

            if (_cardPlay != null && _cardPlay.CanResolve())
            {
                StartCoroutine(ResolveSequenceRoutine(endTurnAfterwards: true));
                return;
            }

            EndPlayerTurn();
        }

        IEnumerator ResolveSequenceRoutine(bool endTurnAfterwards)
        {
            _resolveLimits.BeginResolve();

            yield return _cardPlay.ResolveSequenceRoutine();

            if (_battleResolved)
                yield break;

            if (endTurnAfterwards)
            {
                EndPlayerTurn();
                yield break;
            }

            EndPlayerTurnIfExhausted();
        }

        /// <summary>
        /// Runs once a Resolve has fully settled, including cascades and every enchant. Only here can the
        /// game tell whether the turn can continue, because a tile may have handed back the stamina that
        /// pays for the next sequence.
        /// </summary>
        void HandleSequenceResolved(ResolveReport report)
        {
            if (_battleResolved)
                return;

            _turnReport.AddResolve(report, _resolveLimits.StaminaRefunded);
        }

        void EndPlayerTurnIfExhausted()
        {
            if (_battleResolved || !_isPlayerTurn || _activeBoard == null || PlayerHasLegalAction())
                return;

            EndPlayerTurn();
        }

        bool PlayerHasLegalAction()
        {
            if (_cardPlay != null && (_cardPlay.HasQueueableCard() || _cardPlay.CanResolve()))
                return true;

            if (HasCastableSkill())
                return true;

            return HasReducibleSkillCooldown();
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
                if (skill != null
                    && _player.IsSkillReady(skill)
                    && skill.HasEnoughActionPoints(softStats)
                    && skill.HasEnoughMana(softStats)
                    && skill.MeetsCastRequirements(_player, _activeEnemy))
                    return true;
            }

            return false;
        }

        bool HasReducibleSkillCooldown()
        {
            SoftStats softStats = _player?.Stats?.Soft;
            IReadOnlyList<SkillDefinition> skills = _player?.Skills;

            if (softStats == null || skills == null || !softStats.HasActionPoints(1))
                return false;

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill != null && _player.GetRemainingCooldown(skill) > 0)
                    return true;
            }

            return false;
        }

        void BeginPlayerTurn()
        {
            if (_battleResolved)
                return;

            _isPlayerTurn = true;
            _turnReport.BeginTurn();
            _resolveLimits.BeginResolve();

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = true;

            SnapshotVitals(_player, out int playerHealth, out int playerShield);
            _player?.OnTurnStarted();
            _activeEnemy?.RefreshTelegraph();
            NotifyHitReaction(_player, playerHealth, playerShield);
            TryResolveBattleOutcome();

            if (_battleResolved)
                return;

            _cardPlay?.BeginTurn();
        }

        void EndPlayerTurn()
        {
            if (_battleResolved)
                return;

            _isPlayerTurn = false;
            _awaitingSkillChoice = false;

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = false;

            _cardPlay?.EndTurn();
            _player?.TickSkillCooldowns();

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
            SnapshotVitals(_activeEnemy, out int enemyHealth, out int enemyShield);
            _activeEnemy?.OnTurnStarted();
            NotifyHitReaction(_activeEnemy, enemyHealth, enemyShield);
            TryResolveBattleOutcome();

            if (_battleResolved)
                yield break;

            yield return _activeEnemy.PlayTurn(_activeBoard);
            _activeEnemy?.TickSkillCooldowns();
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
                HideEndBattlePanel();
                return;
            }

            MapRunState mapRun = MapRunState.Active;
            bool returnToMap = mapRun != null && mapRun.HasPendingBattle;
            bool wonBoss = returnToMap &&
                           _lastOutcome == BattleOutcome.Win &&
                           mapRun.IsPendingBossBattle;

            if (returnToMap && !wonBoss)
                mapRun.ResolveBattle(_lastOutcome == BattleOutcome.Win);

            EndBattle();

            if (wonBoss)
            {
                if (TryShowBossSkillReward())
                    return;

                AdvanceAfterBossVictory();
                return;
            }

            if (returnToMap)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.PersistMapRun();
                SceneFlow.LoadMap();
            }
        }

        bool TryShowBossSkillReward()
        {
            if (_gainSkillPanel == null)
                _gainSkillPanel = FindAnyObjectByType<UIPanelGainSkill>(FindObjectsInactive.Include);

            if (_gainSkillPanel == null)
            {
                Debug.LogError($"{nameof(BattleManager)}: assign {nameof(_gainSkillPanel)} to offer a skill after a boss.", this);
                return false;
            }

            _awaitingBossSkillReward = true;
            _gainSkillPanel.Closed -= HandleBossSkillPanelClosed;
            _gainSkillPanel.Closed += HandleBossSkillPanelClosed;
            _gainSkillPanel.Show();
            return true;
        }

        void HandleBossSkillPanelClosed()
        {
            UnsubscribeBossSkillPanel();
            if (!_awaitingBossSkillReward)
                return;

            _awaitingBossSkillReward = false;
            AdvanceAfterBossVictory();
        }

        void UnsubscribeBossSkillPanel()
        {
            if (_gainSkillPanel != null)
                _gainSkillPanel.Closed -= HandleBossSkillPanelClosed;
        }

        void AdvanceAfterBossVictory()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNextGeneratedMap();
        }

        void HideEndBattlePanel()
        {
            if (_endBattlePanel != null)
                _endBattlePanel.Hide();
        }

        /// <summary>Destroys the board created by <see cref="StartBattle"/>.</summary>
        public void EndBattle()
        {
            EndBattle(hideEndPanel: true);
        }

        void EndBattle(bool hideEndPanel)
        {
            StopAllCoroutines();
            _battleResolved = false;
            if (hideEndPanel)
                HideEndBattlePanel();

            if (_cardPlay != null)
                _cardPlay.EndBattle();

            if (_activeBoard != null)
            {
                _activeBoard.SequenceResolved -= HandleSequenceResolved;
                _activeBoard.MatchWaveCompleted -= HandleMatchWaveCompleted;
                Destroy(_activeBoard.gameObject);
                _activeBoard = null;
            }

            _isPlayerTurn = true;

            ClearSpawnedEnemy();
        }

        void ClearSpawnedEnemy()
        {
            if (_battleWorld != null)
                _battleWorld.ClearEnemyModel();

            if (_activeEnemy != null)
            {
                Destroy(_activeEnemy.gameObject);
                _activeEnemy = null;
            }

            _activeEnemyDefinition = null;
            _activeEnemySpec = null;
        }
    }
}
