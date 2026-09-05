using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Spawns a ring of decorative props around this transform. Spacing is even,
    /// with small jitter so the circle reads as scattered loot rather than a formation.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MapMarkerProps : MonoBehaviour
    {
        const string SpawnPrefix = "Prop_";

        [SerializeField] GameObject[] propPrefabs;

        [Tooltip("Circle radius in local XZ. Keep it on the platform rim so props do not hover off the edge.")]
        [SerializeField] float _radius = 0.86f;

        [Tooltip("Inclusive random count. Clamped to the assigned prefab pool.")]
        [SerializeField] Vector2Int _countRange = new Vector2Int(4, 8);

        [SerializeField] float _yOffset;

        [Tooltip("Sink props into the plane after grounding, so blades and bones read as planted.")]
        [SerializeField] float _embed = 0.035f;

        [Tooltip("0 derives a stable seed from this instance so each marker looks different.")]
        [SerializeField] int _seed;

        [SerializeField, Range(0f, 0.45f)] float _radiusJitter = 0.06f;
        [SerializeField, Range(0f, 50f)] float _yawJitter = 18f;
        [SerializeField, Range(0f, 30f)] float _tiltMax = 12f;
        [SerializeField, Range(0f, 1f)] float _layChance = 0.35f;
        [SerializeField] Vector2 _scaleRange = new Vector2(0.9f, 1.2f);
        [SerializeField] bool _faceOutward = true;
        [SerializeField] bool _sitOnPlane = true;

        readonly List<GameObject> _spawned = new List<GameObject>();
        bool _rebuildQueued;

        void OnEnable()
        {
            Rebuild();
        }

        void OnDisable()
        {
            if (!Application.isPlaying)
                ClearSpawned();
        }

        void OnValidate()
        {
            _radius = Mathf.Max(0.05f, _radius);
            int minCount = Mathf.Max(0, _countRange.x);
            int maxCount = Mathf.Max(minCount, _countRange.y);
            _countRange = new Vector2Int(minCount, maxCount);
            _embed = Mathf.Max(0f, _embed);
            _layChance = Mathf.Clamp01(_layChance);
            if (_scaleRange.x > _scaleRange.y)
                _scaleRange = new Vector2(_scaleRange.y, _scaleRange.x);
            _scaleRange.x = Mathf.Max(0.05f, _scaleRange.x);
            _scaleRange.y = Mathf.Max(_scaleRange.x, _scaleRange.y);

            if (isActiveAndEnabled)
                QueueRebuild();
        }

        [ContextMenu("Rebuild Props")]
        public void Rebuild()
        {
            _rebuildQueued = false;
            if (!CanSpawn())
                return;

            ClearSpawned();

            GameObject[] pool = CollectPool();
            if (pool.Length == 0)
                return;

            var rng = new System.Random(ResolveSeed());
            int count = ResolveCount(pool.Length, rng);
            if (count <= 0)
                return;

            int[] picks = PickIndices(pool.Length, count, rng);
            float startAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float step = (Mathf.PI * 2f) / count;

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = pool[picks[i]];
                if (prefab == null)
                    continue;

                float angle = startAngle + step * i;
                float radialJitter = 1f + NextRange(rng, -_radiusJitter, _radiusJitter);
                float radius = _radius * Mathf.Max(0.15f, radialJitter);
                Vector3 localPos = new Vector3(Mathf.Cos(angle) * radius, _yOffset, Mathf.Sin(angle) * radius);
                Vector3 radial = new Vector3(localPos.x, 0f, localPos.z);
                if (radial.sqrMagnitude < 0.0001f)
                    radial = Vector3.forward;
                radial.Normalize();

                Vector3 facing = _faceOutward ? radial : Vector3.Cross(Vector3.up, radial);
                Quaternion look = Quaternion.LookRotation(facing, Vector3.up);
                bool layDown = ShouldLayDown(prefab, rng);
                Quaternion pose = layDown
                    ? Quaternion.Euler(88f + NextRange(rng, -8f, 8f), NextRange(rng, -_yawJitter, _yawJitter), NextRange(rng, -12f, 12f))
                    : Quaternion.Euler(
                        NextRange(rng, -_tiltMax * 0.35f, _tiltMax),
                        NextRange(rng, -_yawJitter, _yawJitter),
                        NextRange(rng, -_tiltMax * 0.4f, _tiltMax * 0.4f));

                float scale = NextRange(rng, _scaleRange.x, _scaleRange.y);
                GameObject instance = Instantiate(prefab, transform);
                instance.name = SpawnPrefix + prefab.name;
                instance.transform.SetLocalPositionAndRotation(localPos, look * pose);
                instance.transform.localScale = prefab.transform.localScale * scale;
                instance.layer = gameObject.layer;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    instance.hideFlags = HideFlags.DontSave;
#endif
                StripColliders(instance);
                if (_sitOnPlane)
                    SitOnLocalPlane(instance, _yOffset, _embed);

                _spawned.Add(instance);
            }
        }

        bool CanSpawn()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(this))
                return false;
#endif
            return isActiveAndEnabled;
        }

        void QueueRebuild()
        {
            if (_rebuildQueued)
                return;

            _rebuildQueued = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += FlushRebuild;
#else
            Rebuild();
#endif
        }

#if UNITY_EDITOR
        void FlushRebuild()
        {
            UnityEditor.EditorApplication.delayCall -= FlushRebuild;
            if (this == null || !isActiveAndEnabled)
            {
                _rebuildQueued = false;
                return;
            }

            Rebuild();
        }
#endif

        GameObject[] CollectPool()
        {
            if (propPrefabs == null || propPrefabs.Length == 0)
                return System.Array.Empty<GameObject>();

            int valid = 0;
            for (int i = 0; i < propPrefabs.Length; i++)
            {
                if (propPrefabs[i] != null)
                    valid++;
            }

            if (valid == 0)
                return System.Array.Empty<GameObject>();

            var pool = new GameObject[valid];
            int w = 0;
            for (int i = 0; i < propPrefabs.Length; i++)
            {
                if (propPrefabs[i] != null)
                    pool[w++] = propPrefabs[i];
            }

            return pool;
        }

        static int[] PickIndices(int poolLength, int count, System.Random rng)
        {
            var picks = new int[count];
            var bag = new int[poolLength];
            for (int i = 0; i < poolLength; i++)
                bag[i] = i;

            int remaining = poolLength;
            for (int i = 0; i < count; i++)
            {
                if (remaining == 0)
                {
                    remaining = poolLength;
                    for (int b = 0; b < poolLength; b++)
                        bag[b] = b;
                }

                int choice = rng.Next(remaining);
                picks[i] = bag[choice];
                bag[choice] = bag[remaining - 1];
                remaining--;
            }

            return picks;
        }

        int ResolveCount(int poolLength, System.Random rng)
        {
            int min = Mathf.Clamp(_countRange.x, 0, poolLength);
            int max = Mathf.Clamp(_countRange.y, min, poolLength);
            if (max <= 0)
                return 0;

            return rng.Next(min, max + 1);
        }

        int ResolveSeed()
        {
            if (_seed != 0)
                return _seed;

            return unchecked(GetInstanceID() * 397);
        }

        static float NextRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        bool ShouldLayDown(GameObject prefab, System.Random rng)
        {
            Vector3 size = MeasurePrefabSize(prefab);
            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            float shortest = Mathf.Max(0.001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)));
            bool compact = longest / shortest < 2.1f || size.y < 0.22f;
            return compact || rng.NextDouble() < _layChance;
        }

        static Vector3 MeasurePrefabSize(GameObject prefab)
        {
            MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh.bounds.size;
            return Vector3.one * 0.3f;
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
                DestroySpawned(_spawned[i]);
            _spawned.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith(SpawnPrefix))
                    DestroySpawned(child.gameObject);
            }
        }

        static void DestroySpawned(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        static void StripColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(colliders[i]);
                else
                    DestroyImmediate(colliders[i]);
            }
        }

        static void SitOnLocalPlane(GameObject instance, float localY, float embed)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            Transform parent = instance.transform.parent;
            float planeY = parent != null
                ? parent.TransformPoint(new Vector3(0f, localY, 0f)).y
                : localY;
            instance.transform.position += Vector3.up * (planeY - bounds.min.y - embed);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.85f);
            Gizmos.matrix = transform.localToWorldMatrix;

            const int segments = 48;
            Vector3 prev = new Vector3(_radius, _yOffset, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f) * i / segments;
                Vector3 next = new Vector3(Mathf.Cos(angle) * _radius, _yOffset, Mathf.Sin(angle) * _radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
