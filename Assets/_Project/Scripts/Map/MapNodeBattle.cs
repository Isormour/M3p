using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Battle map marker: spawns the encounter enemy's visual model when the node is still uncleared.
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

        public override void SetState(bool isCurrent, bool reachable, bool cleared, bool highlighted)
        {
            base.SetState(isCurrent, reachable, cleared, highlighted);
            if (cleared)
                ClearSpawnedCharacter();
            else if (_spawnedCharacter == null)
                SpawnCharacterFromEncounter();
        }

        void SpawnCharacterFromEncounter()
        {
            ClearSpawnedCharacter();

            if (IsEncounterCleared())
                return;

            EnemyDefinition enemy = MapRunState.Active != null
                ? MapRunState.Active.PickEncounterEnemy(Encounter, NodeId)
                : Encounter != null ? Encounter.PickEnemy(0, NodeId) : null;
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
            if (!parent.gameObject.activeInHierarchy)
                parent = transform;

            _spawnedCharacter = Instantiate(prefab, parent);
            _spawnedCharacter.name = prefab.name;
            _spawnedCharacter.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _spawnedCharacter.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            _spawnedCharacter.transform.rotation = Quaternion.Euler(0, 180, 0);
            ApplyFlyingMapHover(_spawnedCharacter);
            // Prefab colliders are battle-sized and would cover neighbouring nodes.
            Collider[] colliders = _spawnedCharacter.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);

            AddClickVolume(_spawnedCharacter);
        }

        /// <summary>
        /// The marker collider sits on the pedestal. A volume on the mesh lets a click on the enemy
        /// select this node. The volume stays a child, so the raycast still resolves to <see cref="MapNode"/>.
        /// </summary>
        void AddClickVolume(GameObject root)
        {
            bool hasBounds = false;
            Bounds world = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;

                if (!hasBounds)
                {
                    world = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    world.Encapsulate(renderer.bounds);
                }
            }

            GameObject volumeObject = new GameObject("ClickVolume");
            volumeObject.layer = gameObject.layer;
            Transform volume = volumeObject.transform;
            volume.SetParent(root.transform, false);
            volume.rotation = Quaternion.identity;

            BoxCollider box = volumeObject.AddComponent<BoxCollider>();
            if (!hasBounds || world.size.sqrMagnitude < 0.0001f)
            {
                box.center = new Vector3(0f, 1f, 0f);
                box.size = new Vector3(0.8f, 2f, 0.8f);
                return;
            }

            box.center = volume.InverseTransformPoint(world.center);
            Vector3 size = volume.InverseTransformVector(world.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        static void ApplyFlyingMapHover(GameObject root)
        {
            WorldCharacter character = root.GetComponent<WorldCharacter>();
            if (character == null)
                character = root.GetComponentInChildren<WorldCharacter>();
            if (character == null || character.CharacterType != ECharacterType.Flying)
                return;

            FlyingCharacterAnimation flying = character.GetComponent<FlyingCharacterAnimation>();
            if (flying == null)
                flying = character.GetComponentInChildren<FlyingCharacterAnimation>(true);
            if (flying != null)
                flying.HipHoverHeight = 0.4f;
        }

        void ClearSpawnedCharacter()
        {
            if (_spawnedCharacter == null)
                return;

            Destroy(_spawnedCharacter);
            _spawnedCharacter = null;
        }

        static MapRunState ResolveRun()
        {
            if (MapRunState.Active != null)
                return MapRunState.Active;
            return GameManager.Instance != null ? GameManager.Instance.MapRun : null;
        }

        bool IsEncounterCleared()
        {
            MapRunState run = ResolveRun();
            return run != null && run.IsCleared(NodeId);
        }
    }
}
