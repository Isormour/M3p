using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One map-room definition: type, (for battles) a pool of enemies of which one is picked,
    /// and (for chests) the loot table. Map markers are chosen by <see cref="MapNodeType"/>
    /// in <see cref="MapManager"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EncounterConfig", menuName = "M3P/Encounter Config", order = 21)]
    public class EncounterConfig : ScriptableObject
    {
        [SerializeField] MapNodeType _type = MapNodeType.Battle;

        [Tooltip("Enemies that may appear when Type is Battle, Elite or Boss. One is picked per node. Ignored for other types.")]
        [SerializeField] EnemyDefinition[] _enemies = Array.Empty<EnemyDefinition>();

        [Tooltip("Loot granted when Type is Chest. Ignored for other types.")]
        [SerializeField] ChestConfig _chest;

        public MapNodeType Type => _type;

        public IReadOnlyList<EnemyDefinition> Enemies => _enemies ?? Array.Empty<EnemyDefinition>();

        public ChestConfig Chest => _chest;

        public bool IsBattle =>
            _type == MapNodeType.Battle || _type == MapNodeType.Elite || _type == MapNodeType.Boss;

        public bool IsChest => _type == MapNodeType.Chest;

        public bool HasEnemy
        {
            get
            {
                if (_enemies == null)
                    return false;

                for (int i = 0; i < _enemies.Length; i++)
                {
                    if (_enemies[i] != null)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Picks one authored enemy. The same seed and node always yield the same enemy.</summary>
        public EnemyDefinition PickEnemy(int seed, string nodeId)
        {
            int count = CountEnemies();
            if (count == 0)
                return null;

            return EnemyAt(StableIndex(seed, nodeId, count));
        }

        /// <summary>Picks one authored enemy using <paramref name="rng"/>, or the first when rng is null.</summary>
        public EnemyDefinition PickEnemy(System.Random rng)
        {
            int count = CountEnemies();
            if (count == 0)
                return null;

            int index = rng != null ? rng.Next(count) : 0;
            return EnemyAt(index);
        }

        int CountEnemies()
        {
            if (_enemies == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i] != null)
                    count++;
            }

            return count;
        }

        EnemyDefinition EnemyAt(int validIndex)
        {
            int seen = 0;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i] == null)
                    continue;

                if (seen == validIndex)
                    return _enemies[i];

                seen++;
            }

            return null;
        }

        static int StableIndex(int seed, string nodeId, int count)
        {
            unchecked
            {
                int hash = seed * 397;
                if (!string.IsNullOrEmpty(nodeId))
                {
                    for (int i = 0; i < nodeId.Length; i++)
                        hash = (hash * 31) + nodeId[i];
                }

                return (int)((uint)hash % (uint)count);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if ((_type == MapNodeType.Battle || _type == MapNodeType.Elite || _type == MapNodeType.Boss) &&
                !HasEnemy)
            {
                Debug.LogWarning(
                    $"{nameof(EncounterConfig)} '{name}': Battle encounters should assign at least one {nameof(EnemyDefinition)}.",
                    this);
            }

            if (_type == MapNodeType.Chest && _chest == null)
            {
                Debug.LogWarning(
                    $"{nameof(EncounterConfig)} '{name}': Chest encounters should assign a {nameof(ChestConfig)}.",
                    this);
            }
        }
#endif
    }
}
