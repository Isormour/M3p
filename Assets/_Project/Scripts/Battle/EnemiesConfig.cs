using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The registry of every enemy archetype in the game. Ids are handed out once and never reused,
    /// so a reference by id survives the list being reordered or trimmed.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemiesConfig", menuName = "M3P/Enemies Config", order = 3)]
    public class EnemiesConfig : ScriptableObject
    {
        /// <summary>Id of an enemy that this config does not know about.</summary>
        public const int InvalidEnemyId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans every encounter that stored this id.")]
            public int Id;
            public EnemyDefinition Enemy;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Only ever counts up, so removing an enemy does not free its id for the next one.</summary>
        [SerializeField, HideInInspector] int _nextEnemyId = InvalidEnemyId + 1;

        Dictionary<EnemyDefinition, int> _idsByEnemy;
        Dictionary<int, EnemyDefinition> _enemiesById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Id of a registered enemy, or <see cref="InvalidEnemyId"/> when it is not in this config.</summary>
        public int GetEnemyId(EnemyDefinition enemy)
        {
            if (enemy == null)
                return InvalidEnemyId;

            EnsureLookups();
            return _idsByEnemy.TryGetValue(enemy, out int id) ? id : InvalidEnemyId;
        }

        public bool TryGetEnemy(int enemyId, out EnemyDefinition enemy)
        {
            EnsureLookups();
            return _enemiesById.TryGetValue(enemyId, out enemy);
        }

        public EnemyDefinition GetEnemy(int enemyId)
        {
            return TryGetEnemy(enemyId, out EnemyDefinition enemy) ? enemy : null;
        }

        /// <summary>A random registered enemy, or null when the registry is empty.</summary>
        public EnemyDefinition PickRandom()
        {
            Entry[] entries = Entries;
            int seen = 0;
            EnemyDefinition pick = null;
            for (int i = 0; i < entries.Length; i++)
            {
                EnemyDefinition candidate = entries[i].Enemy;
                if (candidate == null)
                    continue;

                seen++;
                if (UnityEngine.Random.Range(0, seen) == 0)
                    pick = candidate;
            }

            return pick;
        }

        void EnsureLookups()
        {
            if (_idsByEnemy != null)
                return;

            Entry[] entries = Entries;
            _idsByEnemy = new Dictionary<EnemyDefinition, int>(entries.Length);
            _enemiesById = new Dictionary<int, EnemyDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Enemy == null || entry.Id == InvalidEnemyId)
                    continue;

                if (_enemiesById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(EnemiesConfig)} '{name}': enemy id {entry.Id} is used twice. Give '{entry.Enemy.name}' a unique id.",
                        this);
                    continue;
                }

                _idsByEnemy[entry.Enemy] = entry.Id;
                _enemiesById[entry.Id] = entry.Enemy;
            }
        }

        /// <summary>
        /// Hands the next free id to every newly added enemy. Existing ids are left alone so nothing
        /// already stored against them changes meaning.
        /// </summary>
        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Length; i++)
                _nextEnemyId = Mathf.Max(_nextEnemyId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Enemy == null || _entries[i].Id > InvalidEnemyId)
                    continue;

                _entries[i].Id = _nextEnemyId++;
            }
        }

        void OnEnable()
        {
            _idsByEnemy = null;
            _enemiesById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsByEnemy = null;
            _enemiesById = null;
        }
    }
}
