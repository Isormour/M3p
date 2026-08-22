using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Builds a walkable node graph on the Map scene. Click a highlighted neighbour to move;
    /// battle / shop / chest resolve when the token arrives.
    /// </summary>
    public sealed class MapManager : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField] MapGraphDefinition _graph;
        [Tooltip("When no asset is assigned, spawn the built-in demo floor.")]
        [SerializeField] bool _useDemoGraphIfMissing = true;

        [Header("Presentation")]
        [Tooltip("Player pawn spawned on the map. Root should have MapPlayerToken (added at runtime if missing).")]
        [SerializeField] GameObject _playerTokenPrefab;

        [Header("Markers by node type")]
        [SerializeField] GameObject _startMarkerPrefab;
        [SerializeField] GameObject _battleMarkerPrefab;
        [SerializeField] GameObject _shopMarkerPrefab;
        [SerializeField] GameObject _chestMarkerPrefab;

        [SerializeField] float _nodeWorldY = 0f;
        [SerializeField] float _nodeRadius = 0.55f;
        [SerializeField] float _edgeWidth = 0.08f;
        [SerializeField] Color _edgeColor = new Color(0.55f, 0.58f, 0.65f, 0.9f);
        [SerializeField] Color _startColor = new Color(0.75f, 0.75f, 0.8f);
        [SerializeField] Color _battleColor = new Color(0.85f, 0.25f, 0.22f);
        [SerializeField] Color _shopColor = new Color(0.25f, 0.45f, 0.9f);
        [SerializeField] Color _chestColor = new Color(0.95f, 0.75f, 0.2f);

        [Header("Camera")]
        [SerializeField] bool _frameCameraOnStart = true;
        [SerializeField] float _cameraHeight = 14f;
        [SerializeField] float _cameraDistance = 10f;
        [SerializeField] float _cameraPitch = 55f;
        [SerializeField] MapCamera _mapCam;

        [Header("UI")]
        [Tooltip("Shown before the token walks to a neighbour. Confirm moves; Close cancels.")]
        [SerializeField] UIMapPanelWalkNodeConfirm _walkNodeConfirmPanel;

        [Header("Generated map")]
        [SerializeField] int _generatedLayerCount = 4;
        [SerializeField] int _generatedNodesPerLayerMin = 2;
        [SerializeField] int _generatedNodesPerLayerMax = 3;
        [SerializeField] float _generatedLayerSpacing = 3.5f;
        [SerializeField] float _generatedNodeSpacing = 3.5f;

        readonly Dictionary<string, MapNode> _nodeViews = new Dictionary<string, MapNode>();
        readonly Dictionary<string, Vector3> _nodePositions = new Dictionary<string, Vector3>();

        MapGraphDefinition _activeGraph;
        MapPlayerToken _token;
        MapEventPresenter _events;
        Transform _visualRoot;
        bool _ownsRuntimeGraph;
        bool _inputLocked;

        MapRunState _fallbackRun;

        MapRunState Run
        {
            get
            {
                if (GameManager.Instance != null)
                    return GameManager.Instance.MapRun;

                if (MapRunState.Active != null)
                    return MapRunState.Active;

                return _fallbackRun ??= new MapRunState();
            }
        }

        void Start()
        {
            transform.position = Vector3.zero;
            EnsureGraph();
            EnsureRunState();
            BuildVisuals();
            EnsureWalkConfirmPanel();
            PlaceTokenAtCurrentNode();
            RefreshNodeStates();
            if (_frameCameraOnStart)
                FrameCamera();

            // Returning from battle: run state already updated in BattleManager.
            TryPromptCurrentNodeIfNeeded();
        }

        void Update()
        {
            if (_inputLocked ||
                _token != null && _token.IsMoving ||
                _events != null && _events.IsOpen ||
                _walkNodeConfirmPanel != null && _walkNodeConfirmPanel.IsOpen)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            Camera cameraRef = Camera.main;
            if (cameraRef == null)
                return;

            Ray ray = cameraRef.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
                return;

            MapNode node = hit.collider.GetComponentInParent<MapNode>();
            if (node == null)
                return;

            HandleNodeClicked(node.NodeId, node);
        }

        void OnDestroy()
        {
            if (_ownsRuntimeGraph && _activeGraph != null)
                Destroy(_activeGraph);
        }

        void EnsureGraph()
        {
            MapRunState run = Run;
            GameManager.MapLaunchMode mode = GameManager.Instance != null
                ? GameManager.Instance.LaunchMode
                : GameManager.MapLaunchMode.None;

            if (run.IsActive && run.IsGenerated && run.GraphSnapshot != null)
            {
                _activeGraph = MapGraphDefinition.CreateFromSnapshot(run.GraphSnapshot, ResolveEncounterByName);
                _ownsRuntimeGraph = true;
                return;
            }

            if (mode == GameManager.MapLaunchMode.NewGenerated)
            {
                _activeGraph = GenerateGraph();
                _ownsRuntimeGraph = _activeGraph != _graph;
                return;
            }

            if (_graph != null)
            {
                _activeGraph = _graph;
                _ownsRuntimeGraph = false;
                return;
            }

            if (!_useDemoGraphIfMissing)
            {
                Debug.LogError($"{nameof(MapManager)}: assign {nameof(_graph)} or enable demo fallback.", this);
                return;
            }

            _activeGraph = MapGraphDefinition.CreateRuntimeDemo();
            _ownsRuntimeGraph = true;
        }

        void EnsureRunState()
        {
            if (_activeGraph == null)
                return;

            MapRunState run = Run;
            if (run.IsActive && run.GraphName == _activeGraph.name)
            {
                SanitizeRunAgainstGraph(run);
                MapRunState.Active = run;
                return;
            }

            MapGraphSnapshot snapshot = _activeGraph.name == MapGenerator.GeneratedGraphName
                ? _activeGraph.ToSnapshot()
                : null;
            run.BeginRun(_activeGraph.name, _activeGraph.StartNodeId, snapshot);
            PersistRun();
        }

        void SanitizeRunAgainstGraph(MapRunState run)
        {
            if (run == null || _activeGraph == null)
                return;

            if (!_activeGraph.TryGetNode(run.CurrentNodeId, out _))
                run.MoveTo(_activeGraph.StartNodeId);

            if (string.IsNullOrEmpty(run.CurrentNodeId) || !_activeGraph.TryGetNode(run.CurrentNodeId, out _))
                run.BeginRun(_activeGraph.name, _activeGraph.StartNodeId, run.GraphSnapshot);
        }

        void BuildVisuals()
        {
            if (_activeGraph == null)
                return;

            if (_visualRoot != null)
                Destroy(_visualRoot.gameObject);

            _nodeViews.Clear();
            _nodePositions.Clear();

            _visualRoot = new GameObject("MapVisuals").transform;
            _visualRoot.SetParent(transform, false);

            var nodesRoot = new GameObject("Nodes").transform;
            nodesRoot.SetParent(_visualRoot, false);

            var edgesRoot = new GameObject("Edges").transform;
            edgesRoot.SetParent(_visualRoot, false);

            IReadOnlyList<MapGraphDefinition.Node> nodes = _activeGraph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapGraphDefinition.Node node = nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                    continue;

                Vector3 worldPos = new Vector3(node.Position.x, _nodeWorldY, node.Position.y);
                _nodePositions[node.Id] = worldPos;

                MapNodeType type = node.ResolvedType;
                GameObject nodeObject = SpawnNodeMarker(node, nodesRoot, worldPos);
                var view = nodeObject.GetComponent<MapNode>();
                if (view == null)
                    view = nodeObject.AddComponent<MapNode>();

                view.Configure(node.Id, node.Encounter, type, ColorForType(type));
                _nodeViews[node.Id] = view;
            }

            IReadOnlyList<MapGraphDefinition.Edge> edges = _activeGraph.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                MapGraphDefinition.Edge edge = edges[i];
                if (edge == null)
                    continue;

                if (!_nodePositions.TryGetValue(edge.FromId, out Vector3 from) ||
                    !_nodePositions.TryGetValue(edge.ToId, out Vector3 to))
                    continue;

                CreateEdge(edgesRoot, from, to, i);
            }

            SpawnPlayerToken();
            _events = gameObject.GetComponent<MapEventPresenter>();
            if (_events == null)
                _events = gameObject.AddComponent<MapEventPresenter>();
        }

        void SpawnPlayerToken()
        {
            if (_playerTokenPrefab == null)
            {
                Debug.LogError($"{nameof(MapManager)}: assign {nameof(_playerTokenPrefab)}.", this);
                return;
            }

            GameObject tokenObject = Instantiate(_playerTokenPrefab, _visualRoot);
            tokenObject.name = "PlayerToken";

            // Markers are clickable; the pawn should not steal raycasts.
            Collider[] colliders = tokenObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);

            _token = tokenObject.GetComponent<MapPlayerToken>();
            if (_token == null)
                _token = tokenObject.GetComponentInChildren<MapPlayerToken>();
            if (_token == null)
                _token = tokenObject.AddComponent<MapPlayerToken>();
        }

        GameObject SpawnNodeMarker(MapGraphDefinition.Node node, Transform parent, Vector3 worldPos)
        {
            MapNodeType type = node.ResolvedType;
            GameObject markerPrefab = GetMarkerPrefab(type);
            GameObject nodeObject;

            if (markerPrefab != null)
            {
                nodeObject = Instantiate(markerPrefab, parent);
                nodeObject.name = $"Node_{node.Id}";
                nodeObject.transform.position = worldPos;
                return nodeObject;
            }

            Debug.LogWarning(
                $"{nameof(MapManager)}: no marker prefab for {type}. Assign one under Markers by node type.",
                this);

            nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObject.name = $"Node_{node.Id}";
            nodeObject.transform.SetParent(parent, false);
            nodeObject.transform.position = worldPos;
            nodeObject.transform.localScale = Vector3.one * (_nodeRadius * 2f);
            return nodeObject;
        }

        GameObject GetMarkerPrefab(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Start: return _startMarkerPrefab;
                case MapNodeType.Battle: return _battleMarkerPrefab;
                case MapNodeType.Shop: return _shopMarkerPrefab;
                case MapNodeType.Chest: return _chestMarkerPrefab;
                default: return null;
            }
        }

        void CreateEdge(Transform parent, Vector3 from, Vector3 to, int index)
        {
            var edgeObject = new GameObject($"Edge_{index}");
            edgeObject.transform.SetParent(parent, false);

            var line = edgeObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from + Vector3.up * 0.05f);
            line.SetPosition(1, to + Vector3.up * 0.05f);
            line.startWidth = _edgeWidth;
            line.endWidth = _edgeWidth;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", _edgeColor);
            else
                material.color = _edgeColor;

            line.sharedMaterial = material;
            line.startColor = _edgeColor;
            line.endColor = _edgeColor;
        }

        void PlaceTokenAtCurrentNode()
        {
            if (_token == null)
                return;

            string currentId = Run.CurrentNodeId;
            if (currentId == null || !_nodePositions.TryGetValue(currentId, out Vector3 pos))
            {
                currentId = _activeGraph != null ? _activeGraph.StartNodeId : null;
                if (currentId == null || !_nodePositions.TryGetValue(currentId, out pos))
                    return;

                Run.MoveTo(currentId);
            }

            _token.SnapTo(pos);
        }

        void RefreshNodeStates()
        {
            MapRunState run = Run;
            List<string> neighbors = _activeGraph != null
                ? _activeGraph.GetNeighborIds(run.CurrentNodeId)
                : new List<string>();

            foreach (KeyValuePair<string, MapNode> pair in _nodeViews)
            {
                bool isCurrent = pair.Key == run.CurrentNodeId;
                bool reachable = neighbors.Contains(pair.Key);
                bool cleared = run.IsCleared(pair.Key);
                pair.Value.SetState(isCurrent, reachable, cleared);
            }
        }

        void EnsureWalkConfirmPanel()
        {
            if (_walkNodeConfirmPanel != null)
                return;

            _walkNodeConfirmPanel = FindAnyObjectByType<UIMapPanelWalkNodeConfirm>(FindObjectsInactive.Include);
        }

        void HandleNodeClicked(string nodeId, MapNode node)
        {
            MapRunState run = Run;
            if (nodeId == run.CurrentNodeId)
            {
                TryActivateCurrentNode();
                return;
            }

            if (_activeGraph == null)
                return;

            List<string> neighbors = _activeGraph.GetNeighborIds(run.CurrentNodeId);
            if (!neighbors.Contains(nodeId))
                return;

            if (!_nodePositions.TryGetValue(nodeId, out Vector3 target))
                return;
            _mapCam.SetTarget(node);

            PromptWalkTo(nodeId, target);
        }

        void PromptWalkTo(string nodeId, Vector3 target)
        {
            EnsureWalkConfirmPanel();

            if (_walkNodeConfirmPanel == null)
            {
                Debug.LogWarning(
                    $"{nameof(MapManager)}: no {nameof(UIMapPanelWalkNodeConfirm)} assigned — walking without confirm.",
                    this);
                BeginWalkTo(nodeId, target);
                return;
            }

            _walkNodeConfirmPanel.Show(
                () => BeginWalkTo(nodeId, target),
                () =>
                {
                    if (_mapCam != null)
                        _mapCam.RestorePrevious();
                });
        }

        void BeginWalkTo(string nodeId, Vector3 target)
        {
            if (_token == null)
                return;

            _inputLocked = true;
            Run.MoveTo(nodeId);
            RefreshNodeStates();
            _token.MoveTo(target, OnArrivedAtNode);
        }

        void OnArrivedAtNode()
        {
            _inputLocked = false;
            RefreshNodeStates();
            PersistRun();
            ResolveNode(Run.CurrentNodeId, arrivedFresh: true);
        }

        void TryPromptCurrentNodeIfNeeded()
        {
            // After a lost battle the player is on the previous node (already cleared path).
            // After a win they sit on a cleared battle — no auto prompt.
            TryActivateCurrentNode(force: false);
        }

        void TryActivateCurrentNode(bool force = true)
        {
            string nodeId = Run.CurrentNodeId;
            if (string.IsNullOrEmpty(nodeId) || _activeGraph == null)
                return;

            if (!_activeGraph.TryGetNode(nodeId, out MapGraphDefinition.Node node))
                return;

            if (node.ResolvedType == MapNodeType.Start)
                return;

            if (!force && Run.IsCleared(nodeId))
                return;

            // Only re-trigger uncleared nodes when clicking the current node.
            if (Run.IsCleared(nodeId) && node.ResolvedType != MapNodeType.Shop)
                return;

            ResolveNode(nodeId, arrivedFresh: false);
        }

        void ResolveNode(string nodeId, bool arrivedFresh)
        {
            if (_activeGraph == null || !_activeGraph.TryGetNode(nodeId, out MapGraphDefinition.Node node))
                return;

            switch (node.ResolvedType)
            {
                case MapNodeType.Battle:
                    if (!Run.IsCleared(nodeId))
                        EnterBattle(nodeId, node.Encounter);
                    break;

                case MapNodeType.Shop:
                    OpenShop(nodeId);
                    break;

                case MapNodeType.Chest:
                    if (!Run.IsCleared(nodeId))
                        OpenChest(nodeId, node.Encounter);
                    else if (!arrivedFresh)
                        _events.Show("Chest", "This chest is empty.", "OK", null);
                    break;
            }
        }

        void EnterBattle(string nodeId, EncounterConfig encounter)
        {
            if (encounter != null && encounter.IsBattle && encounter.Enemy == null)
            {
                Debug.LogError(
                    $"{nameof(MapManager)}: encounter '{encounter.name}' on node '{nodeId}' has no enemy.",
                    encounter);
            }

            Run.BeginBattle(nodeId, encounter);
            _inputLocked = true;
            SceneFlow.LoadBattle();
        }

        void OpenShop(string nodeId)
        {
            _events.Show(
                "Shop",
                "A merchant nods. Full shop inventory comes later — for now you may pass through.",
                "Leave",
                () =>
                {
                    Run.MarkCleared(nodeId);
                    RefreshNodeStates();
                    PersistRun();
                });
        }

        void OpenChest(string nodeId, EncounterConfig encounter)
        {
            ProgressionService progression = GameManager.Instance != null ? GameManager.Instance.Progression : null;
            ChestConfig chest = encounter != null ? encounter.Chest : null;
            if (encounter != null && encounter.IsChest && chest == null)
            {
                Debug.LogWarning(
                    $"{nameof(MapManager)}: chest node '{nodeId}' has no {nameof(ChestConfig)}.",
                    encounter);
            }

            bool canGrant = progression != null && chest != null && chest.HasRewards;
            string rewardText = progression == null
                ? "You crack the chest open (no GameManager — reward skipped)."
                : chest != null
                    ? chest.DescribeRewards()
                    : "The chest is empty.";

            _events.Show(
                "Chest",
                rewardText,
                canGrant ? "Take" : "OK",
                () =>
                {
                    if (canGrant)
                        progression.ApplyRewards(chest.Experience, chest.Shards);

                    Run.MarkCleared(nodeId);
                    RefreshNodeStates();
                    PersistRun();
                });
        }

        Color ColorForType(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Start: return _startColor;
                case MapNodeType.Shop: return _shopColor;
                case MapNodeType.Chest: return _chestColor;
                default: return _battleColor;
            }
        }

        MapGraphDefinition GenerateGraph()
        {
            CollectEncounterPools(
                out EncounterConfig start,
                out List<EncounterConfig> battles,
                out EncounterConfig boss,
                out List<EncounterConfig> chests,
                out List<EncounterConfig> shops);

            if (battles.Count == 0)
            {
                Debug.LogError(
                    $"{nameof(MapManager)}: cannot generate a map, no battle encounters found on {nameof(_graph)}.",
                    this);
                return _graph != null ? _graph : MapGraphDefinition.CreateRuntimeDemo();
            }

            return MapGenerator.Generate(
                start,
                battles,
                boss != null ? boss : battles[battles.Count - 1],
                chests,
                shops,
                _generatedLayerCount,
                _generatedNodesPerLayerMin,
                _generatedNodesPerLayerMax,
                _generatedLayerSpacing,
                _generatedNodeSpacing,
                unchecked(System.Environment.TickCount));
        }

        void CollectEncounterPools(
            out EncounterConfig start,
            out List<EncounterConfig> battles,
            out EncounterConfig boss,
            out List<EncounterConfig> chests,
            out List<EncounterConfig> shops)
        {
            start = null;
            boss = null;
            battles = new List<EncounterConfig>();
            chests = new List<EncounterConfig>();
            shops = new List<EncounterConfig>();

            IReadOnlyList<MapGraphDefinition.Node> nodes = _graph != null ? _graph.Nodes : null;
            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapGraphDefinition.Node node = nodes[i];
                if (node == null || node.Encounter == null)
                    continue;

                switch (node.ResolvedType)
                {
                    case MapNodeType.Start:
                        if (start == null)
                            start = node.Encounter;
                        break;
                    case MapNodeType.Chest:
                        if (!chests.Contains(node.Encounter))
                            chests.Add(node.Encounter);
                        break;
                    case MapNodeType.Shop:
                        if (!shops.Contains(node.Encounter))
                            shops.Add(node.Encounter);
                        break;
                    default:
                        if (node.Id != null &&
                            node.Id.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            boss = node.Encounter;
                        else if (!battles.Contains(node.Encounter))
                            battles.Add(node.Encounter);
                        break;
                }
            }
        }

        EncounterConfig ResolveEncounterByName(string encounterName)
        {
            if (string.IsNullOrEmpty(encounterName))
                return null;

            CollectEncounterPools(
                out EncounterConfig start,
                out List<EncounterConfig> battles,
                out EncounterConfig boss,
                out List<EncounterConfig> chests,
                out List<EncounterConfig> shops);

            if (MatchesEncounter(start, encounterName))
                return start;
            if (MatchesEncounter(boss, encounterName))
                return boss;

            EncounterConfig match = FindEncounter(battles, encounterName)
                ?? FindEncounter(chests, encounterName)
                ?? FindEncounter(shops, encounterName);
            if (match != null)
                return match;

            IReadOnlyList<MapGraphDefinition.Node> nodes = _graph != null ? _graph.Nodes : null;
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                EncounterConfig encounter = nodes[i] != null ? nodes[i].Encounter : null;
                if (MatchesEncounter(encounter, encounterName))
                    return encounter;
            }

            return null;
        }

        static EncounterConfig FindEncounter(List<EncounterConfig> pool, string encounterName)
        {
            if (pool == null)
                return null;

            for (int i = 0; i < pool.Count; i++)
            {
                if (MatchesEncounter(pool[i], encounterName))
                    return pool[i];
            }

            return null;
        }

        static bool MatchesEncounter(EncounterConfig encounter, string encounterName)
        {
            return encounter != null && encounter.name == encounterName;
        }

        void PersistRun()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PersistMapRun();
        }

        void FrameCamera()
        {
            Camera cameraRef = Camera.main;
            if (cameraRef == null || _nodePositions.Count == 0)
                return;

            Vector3 min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, 0f, float.MinValue);
            foreach (Vector3 pos in _nodePositions.Values)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            Vector3 center = (min + max) * 0.5f;
            cameraRef.orthographic = false;
            cameraRef.transform.position = center + new Vector3(0f, _cameraHeight, -_cameraDistance);
            cameraRef.transform.rotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            cameraRef.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            cameraRef.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
