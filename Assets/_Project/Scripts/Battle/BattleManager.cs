using System.Collections;
using System.Collections.Generic;
using Match3;
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

        [Header("Skills")]
        [SerializeField] SkillDefinition[] _skillDefinitions;

        [Header("Flow")]
        [Tooltip("When enabled, begins a battle as soon as this component starts (scene load).")]
        [SerializeField] bool _startBattleImmediately;

        Match3Board _activeBoard;
        EnemyDefinition _activeEnemyDefinition;
        EnemyBattleCharacter _activeEnemy;
        bool _isPlayerTurn = true;

        readonly Dictionary<int, SkillDefinition> _definitionsBySkillId = new Dictionary<int, SkillDefinition>();

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

        /// <summary>Skill definitions keyed by <see cref="SkillDefinition.SkillId"/>.</summary>
        public IReadOnlyDictionary<int, SkillDefinition> DefinitionsBySkillId => _definitionsBySkillId;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildSkillLookup();
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

        void RebuildSkillLookup()
        {
            _definitionsBySkillId.Clear();

            if (_skillDefinitions == null)
                return;

            foreach (var def in _skillDefinitions)
            {
                if (def == null)
                    continue;

                int id = def.SkillId;
                if (_definitionsBySkillId.ContainsKey(id))
                    Debug.LogWarning($"{nameof(BattleManager)}: duplicate {nameof(SkillDefinition)} for SkillId {id}. Using last assignment.", this);

                _definitionsBySkillId[id] = def;
            }
        }

        public bool TryGetSkillDefinition(int skillId, out SkillDefinition definition)
        {
            return _definitionsBySkillId.TryGetValue(skillId, out definition);
        }

        public void ExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            if (skill == null || target == null)
                return;

            skill.UseSkill(caster, target);
        }

        public bool TryExecuteSkill(SkillDefinition skill, BattleCharacter caster, BattleCharacter target)
        {
            if (skill == null || caster == null || target == null || !target.IsAlive)
                return false;

            SoftStats softStats = caster.Stats?.Soft;
            if (softStats == null || !skill.HasEnoughMana(softStats))
                return false;

            if (!skill.TrySpendMana(softStats))
                return false;

            skill.UseSkill(caster, target);
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

            _player?.PrepareForBattle();

            PickRandomEnemyDefinition();
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

            StartCoroutine(SetupTurnFlowRoutine());
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
                if (Random.Range(0, seen) == 0)
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
        }

        IEnumerator SetupTurnFlowRoutine()
        {
            yield return null;

            if (_activeBoard == null)
                yield break;

            _activeBoard.MoveCycleCompleted += HandleBoardMoveCycleCompleted;
            _activeBoard.MatchWaveCompleted += HandleMatchWaveCompleted;
            BeginPlayerTurn();
        }

        void HandleMatchWaveCompleted(int tilesDestroyed)
        {
            if (!_isPlayerTurn || _activeEnemy == null || tilesDestroyed <= 0)
                return;

            _activeEnemy.Stats?.Soft?.TakeDamage(tilesDestroyed);
        }

        void HandleBoardMoveCycleCompleted()
        {
            if (_activeBoard == null)
                return;

            if (_isPlayerTurn)
            {
                _isPlayerTurn = false;
                _activeBoard.AllowPlayerInput = false;

                if (_activeEnemy == null)
                {
                    Debug.LogWarning(
                        $"{nameof(BattleManager)}: ensure {nameof(_enemyDefinitions)} are set and each has {nameof(EnemyDefinition.EnemyCharacterPrefab)}.",
                        this);
                    BeginPlayerTurn();
                    return;
                }

                StartCoroutine(RunEnemyTurnRoutine());
                return;
            }

            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            _isPlayerTurn = true;

            if (_activeBoard != null)
                _activeBoard.AllowPlayerInput = true;

            _player?.OnTurnStarted();
        }

        IEnumerator RunEnemyTurnRoutine()
        {
            yield return _activeEnemy.PlayTurn(_activeBoard);

            if (_activeBoard == null || _isPlayerTurn)
                yield break;

            if (!_activeBoard.IsResolving)
                BeginPlayerTurn();
        }

        /// <summary>Destroys the board created by <see cref="StartBattle"/>.</summary>
        public void EndBattle()
        {
            StopAllCoroutines();

            if (_activeBoard != null)
            {
                _activeBoard.MoveCycleCompleted -= HandleBoardMoveCycleCompleted;
                _activeBoard.MatchWaveCompleted -= HandleMatchWaveCompleted;
                Destroy(_activeBoard.gameObject);
                _activeBoard = null;
            }

            _isPlayerTurn = true;

            ClearSpawnedEnemy();
        }

        void ClearSpawnedEnemy()
        {
            if (_activeEnemy != null)
            {
                Destroy(_activeEnemy.gameObject);
                _activeEnemy = null;
            }

            _activeEnemyDefinition = null;
        }
    }
}
