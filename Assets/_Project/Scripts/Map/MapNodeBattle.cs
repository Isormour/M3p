using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Battle map marker: spawns the encounter enemy's visual model when configured.
    /// </summary>
    public class MapNodeBattle : MapNode
    {
        [Tooltip("Parent for the spawned enemy model. Defaults to this transform.")]
        [SerializeField] Transform _characterParent;

        GameObject _spawnedCharacter;

        public override void Configure(string nodeId, EncounterConfig encounter, MapNodeType type, Color color)
        {
            base.Configure(nodeId, encounter, type, color);
            SpawnCharacterFromEncounter();
        }

        void SpawnCharacterFromEncounter()
        {
            ClearSpawnedCharacter();

            EnemyDefinition enemy = Encounter != null ? Encounter.Enemy : null;
            GameObject prefab = enemy != null ? enemy.EnemyModelPrefab : null;
            if (prefab == null)
            {
                if (enemy != null)
                {
                    Debug.LogWarning(
                        $"{nameof(MapNodeBattle)} on '{name}': enemy '{enemy.name}' has no {nameof(EnemyDefinition.EnemyModelPrefab)}.",
                        this);
                }

                return;
            }

            Transform parent = _characterParent != null ? _characterParent : transform;
            _spawnedCharacter = Instantiate(prefab, parent);
            _spawnedCharacter.name = prefab.name;
            _spawnedCharacter.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _spawnedCharacter.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            // Marker collider handles clicks; character mesh must not steal raycasts.
            Collider[] colliders = _spawnedCharacter.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);
        }

        void ClearSpawnedCharacter()
        {
            if (_spawnedCharacter == null)
                return;

            Destroy(_spawnedCharacter);
            _spawnedCharacter = null;
        }
    }
}
