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
        [Tooltip("Prefab must have EnemyBattleCharacter on its root.")]
        [SerializeField] EnemyBattleCharacter _enemyBattleCharacterPrefab;
        [Tooltip("Optional transform for spawned enemies; defaults under this manager.")]
        [SerializeField] Transform _enemySpawnParent;
        [SerializeField] List<EnemyDefinition> _enemyDefinitions = new List<EnemyDefinition>();

        [Header("Skills")]
        [SerializeField] SkillDefinition[] _skillDefinitions;

        Match3Board _activeBoard;
        EnemyDefinition _activeEnemyDefinition;
        EnemyBattleCharacter _activeEnemy;
        bool _isPlayerTurn = true;

        readonly Dictionary<int, SkillDefinition> _definitionsBySkillId = new Dictionary<int, SkillDefinition>();

        /// <summary>Board from the current battle, or null if no battle is running.</summary>
        public Match3Board ActiveBoard => _activeBoard;

        /// <summary>True when the human player may swap tiles on the board.</summary>
        public bool IsPlayerTurn => _isPlayerTurn;

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

        /// <summary>Spawns the match-3 board prefab (ends any existing battle first).</summary>
        public void StartBattle()
        {
            if (_match3BoardPrefab == null)
            {
                Debug.LogError($"{nameof(BattleManager)}: assign {nameof(_match3BoardPrefab)}.", this);
                return;
            }

            EndBattle();

            PickRandomEnemyDefinition();
            SpawnEnemyBattleCharacter();

            Transform parent = _boardParent != null ? _boardParent : transform;
            GameObject instance = Instantiate(_match3BoardPrefab, parent);
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

            if (_enemyBattleCharacterPrefab == null)
            {
                Debug.LogError($"{nameof(BattleManager)}: assign {nameof(_enemyBattleCharacterPrefab)} to spawn enemies from definitions.", this);
                _activeEnemyDefinition = null;
                return;
            }

            Transform parent = _enemySpawnParent != null ? _enemySpawnParent : transform;
            EnemyBattleCharacter spawned = Instantiate(_enemyBattleCharacterPrefab, parent);
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
            BeginPlayerTurn();
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
                    Debug.LogWarning($"{nameof(BattleManager)}: add {nameof(EnemyDefinition)} assets to {nameof(_enemyDefinitions)} and assign {nameof(_enemyBattleCharacterPrefab)}.", this);
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
            {
                Debug.LogWarning($"{nameof(BattleManager)}: enemy did not start a swap; returning turn to player.", this);
                BeginPlayerTurn();
            }
        }

        /// <summary>Destroys the board created by <see cref="StartBattle"/>.</summary>
        public void EndBattle()
        {
            StopAllCoroutines();

            if (_activeBoard != null)
            {
                _activeBoard.MoveCycleCompleted -= HandleBoardMoveCycleCompleted;
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
