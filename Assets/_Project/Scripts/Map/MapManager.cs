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
        [SerializeField] GameObject _forgeMarkerPrefab;

        [SerializeField] float _nodeWorldY = 0f;
        [SerializeField] float _nodeRadius = 0.55f;
        [SerializeField] VFXMapNodeLine _edgePrefab;
        [SerializeField] Color _startColor = new Color(0.75f, 0.75f, 0.8f);
        [SerializeField] Color _battleColor = new Color(0.85f, 0.25f, 0.22f);
        [SerializeField] Color _shopColor = new Color(0.25f, 0.45f, 0.9f);
        [SerializeField] Color _chestColor = new Color(0.95f, 0.75f, 0.2f);
        [SerializeField] Color _eliteColor = new Color(0.55f, 0.18f, 0.7f);
        [SerializeField] Color _forgeColor = new Color(0.95f, 0.5f, 0.18f);
        [SerializeField] Color _bossColor = new Color(0.55f, 0.08f, 0.1f);

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
        [SerializeField] int _generatedFloorIndex = 1;
        [SerializeField] float _generatedLayerSpacing = 3.5f;
        [SerializeField] float _generatedNodeSpacing = 3.5f;

        readonly Dictionary<string, MapNode> _nodeViews = new Dictionary<string, MapNode>();
        readonly Dictionary<string, Vector3> _nodePositions = new Dictionary<string, Vector3>();
        readonly List<VFXMapNodeLine> _edgeViews = new List<VFXMapNodeLine>();

        MapGraphDefinition _activeGraph;
        MapPlayerToken _token;
        MapEventPresenter _events;
        UIPanelCardCrafting _cardShopPanel;
        UIPanelTileCrafting _forgePanel;
        UIPanelGainSkill _gainSkillPanel;
        Transform _visualRoot;
        MapNode _previewNode;
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConfigureMapGeneration(
                    _graph,
                    _generatedLayerSpacing,
                    _generatedNodeSpacing);
            }

            EnsureGraph();
            EnsureRunState();
            BuildVisuals();
            EnsureWalkConfirmPanel();
            EnsureServicePanels();
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
                _walkNodeConfirmPanel != null && _walkNodeConfirmPanel.IsOpen ||
                _cardShopPanel != null && _cardShopPanel.IsOpen ||
                _forgePanel != null && _forgePanel.IsOpen ||
                _gainSkillPanel != null && _gainSkillPanel.IsOpen)
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
            run.BeginRun(_activeGraph.name, _activeGraph.StartNodeId, snapshot, run.FloorIndex);
            PersistRun();
        }

        void SanitizeRunAgainstGraph(MapRunState run)
        {
            if (run == null || _activeGraph == null)
                return;

            if (!_activeGraph.TryGetNode(run.CurrentNodeId, out _))
                run.MoveTo(_activeGraph.StartNodeId);

            if (string.IsNullOrEmpty(run.CurrentNodeId) || !_activeGraph.TryGetNode(run.CurrentNodeId, out _))
                run.BeginRun(_activeGraph.name, _activeGraph.StartNodeId, run.GraphSnapshot, run.FloorIndex);

            run.EnsurePath(_activeGraph);
        }

        void BuildVisuals()
        {
            if (_activeGraph == null)
                return;

            if (_visualRoot != null)
                Destroy(_visualRoot.gameObject);

            _nodeViews.Clear();
            _nodePositions.Clear();
            _edgeViews.Clear();
            ClearPreviewNode(refresh: false);

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

                CreateEdge(edgesRoot, edge.FromId, edge.ToId, from, to, i);
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
                case MapNodeType.Elite: return _battleMarkerPrefab;
                case MapNodeType.Boss: return _battleMarkerPrefab;
                case MapNodeType.Shop: return _shopMarkerPrefab;
                case MapNodeType.Forge: return _forgeMarkerPrefab != null ? _forgeMarkerPrefab : _shopMarkerPrefab;
                case MapNodeType.Chest: return _chestMarkerPrefab;
                default: return null;
            }
        }

        void CreateEdge(Transform parent, string fromId, string toId, Vector3 from, Vector3 to, int index)
        {
            if (_edgePrefab == null)
            {
                Debug.LogError($"{nameof(MapManager)}: assign {nameof(_edgePrefab)}.", this);
                return;
            }

            VFXMapNodeLine edge = Instantiate(_edgePrefab, parent);
            edge.name = $"Edge_{index}";
            edge.transform.localPosition = Vector3.zero;
            edge.Configure(fromId, toId, from, to);
            _edgeViews.Add(edge);
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
            string previewId = _previewNode != null ? _previewNode.NodeId : null;

            foreach (KeyValuePair<string, MapNode> pair in _nodeViews)
            {
                bool isCurrent = pair.Key == run.CurrentNodeId;
                bool reachable = neighbors.Contains(pair.Key);
                bool cleared = run.IsCleared(pair.Key);
                bool highlighted = run.IsOnPath(pair.Key) || pair.Key == previewId;
                pair.Value.SetState(isCurrent, reachable, cleared, highlighted);
            }

            bool undirected = _activeGraph != null && !_activeGraph.Directed;
            for (int i = 0; i < _edgeViews.Count; i++)
            {
                VFXMapNodeLine edge = _edgeViews[i];
                if (edge == null)
                    continue;

                bool walked = run.HasWalkedEdge(edge.FromId, edge.ToId) ||
                              undirected && run.HasWalkedEdge(edge.ToId, edge.FromId);
                bool toPreview = !string.IsNullOrEmpty(previewId) &&
                                 edge.Connects(run.CurrentNodeId, previewId, undirected);
                edge.SetHighlighted(walked || toPreview);
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

            PromptWalkTo(nodeId, target, node);
        }

        void PromptWalkTo(string nodeId, Vector3 target, MapNode node)
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

            SetPreviewNode(node);
            _walkNodeConfirmPanel.Show(
                () => BeginWalkTo(nodeId, target),
                () =>
                {
                    ClearPreviewNode();
                    if (_mapCam != null)
                        _mapCam.RestorePrevious();
                });
        }

        void BeginWalkTo(string nodeId, Vector3 target)
        {
            if (_token == null)
                return;

            _inputLocked = true;
            _previewNode = null;
            Run.MoveTo(nodeId);
            RefreshNodeStates();
            _token.MoveTo(target, OnArrivedAtNode);
        }

        void SetPreviewNode(MapNode node)
        {
            if (_previewNode == node)
                return;

            _previewNode = node;
            RefreshNodeStates();
        }

        void ClearPreviewNode(bool refresh = true)
        {
            if (_previewNode == null)
                return;

            _previewNode = null;
            if (refresh)
                RefreshNodeStates();
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
            if (Run.IsCleared(nodeId) && !node.ResolvedType.IsRevisitable())
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
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    if (!Run.IsCleared(nodeId))
                        EnterBattle(nodeId, node.Encounter, node.ResolvedType);
                    break;

                case MapNodeType.Shop:
                    OpenCardShop(nodeId);
                    break;

                case MapNodeType.Forge:
                    OpenForge(nodeId);
                    break;

                case MapNodeType.Chest:
                    if (!Run.IsCleared(nodeId))
                        OpenChest(nodeId, node.Encounter);
                    else if (!arrivedFresh)
                        _events.Show("Chest", "This chest is empty.", "OK", null);
                    break;
            }
        }

        void EnterBattle(string nodeId, EncounterConfig encounter, MapNodeType encounterType)
        {
            if (encounter != null && encounter.IsBattle && !encounter.HasEnemy)
            {
                Debug.LogError(
                    $"{nameof(MapManager)}: encounter '{encounter.name}' on node '{nodeId}' has no enemy.",
                    encounter);
            }

            Run.BeginBattle(nodeId, encounter, encounterType);
            _inputLocked = true;
            SceneFlow.LoadBattle();
        }

        void OpenCardShop(string nodeId)
        {
            EnsureServicePanels();
            if (_cardShopPanel != null)
            {
                _cardShopPanel.Show();
                MarkServiceVisited(nodeId);
                return;
            }

            OpenServiceFallback(nodeId, "Card Shop", "A card merchant. Crafting panel is missing from the scene.");
        }

        void OpenForge(string nodeId)
        {
            EnsureServicePanels();
            if (_forgePanel != null)
            {
                _forgePanel.Show();
                MarkServiceVisited(nodeId);
                return;
            }

            OpenServiceFallback(nodeId, "Forge", "A tile forge. Crafting panel is missing from the scene.");
        }

        void OpenServiceFallback(string nodeId, string title, string body)
        {
            _events.Show(
                title,
                body,
                "Leave",
                () => MarkServiceVisited(nodeId));
        }

        void MarkServiceVisited(string nodeId)
        {
            Run.MarkCleared(nodeId);
            RefreshNodeStates();
            PersistRun();
        }

        void EnsureServicePanels()
        {
            if (_cardShopPanel == null)
                _cardShopPanel = FindAnyObjectByType<UIPanelCardCrafting>(FindObjectsInactive.Include);
            if (_forgePanel == null)
                _forgePanel = FindAnyObjectByType<UIPanelTileCrafting>(FindObjectsInactive.Include);
            if (_gainSkillPanel == null)
                _gainSkillPanel = FindAnyObjectByType<UIPanelGainSkill>(FindObjectsInactive.Include);
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
                case MapNodeType.Forge: return _forgeColor;
                case MapNodeType.Chest: return _chestColor;
                case MapNodeType.Elite: return _eliteColor;
                case MapNodeType.Boss: return _bossColor;
                default: return _battleColor;
            }
        }

        MapGraphDefinition GenerateGraph()
        {
            MapEncounterPools pools = CollectEncounterPools();
            if (pools.Battles.Count == 0)
            {
                Debug.LogError(
                    $"{nameof(MapManager)}: cannot generate a map, no battle encounters found on {nameof(_graph)}.",
                    this);
                return _graph != null ? _graph : MapGraphDefinition.CreateRuntimeDemo();
            }

            int floorIndex = Run.FloorIndex > 0 ? Run.FloorIndex : _generatedFloorIndex;
            MapGraphDefinition generated = MapGenerator.Generate(
                pools,
                unchecked(System.Environment.TickCount),
                _generatedLayerSpacing,
                _generatedNodeSpacing,
                floorIndex);

            if (generated != null)
                return generated;

            Debug.LogError($"{nameof(MapManager)}: map generation failed validation.", this);
            return _graph != null ? _graph : MapGraphDefinition.CreateRuntimeDemo();
        }

        MapEncounterPools CollectEncounterPools()
        {
            return MapEncounterPools.FromGraph(_graph);
        }

        EncounterConfig ResolveEncounterByName(string encounterName)
        {
            if (string.IsNullOrEmpty(encounterName))
                return null;

            EncounterConfig fromPools = CollectEncounterPools().FindByName(encounterName);
            if (fromPools != null)
                return fromPools;

            IReadOnlyList<MapGraphDefinition.Node> nodes = _graph != null ? _graph.Nodes : null;
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                EncounterConfig encounter = nodes[i] != null ? nodes[i].Encounter : null;
                if (encounter != null && encounter.name == encounterName)
                    return encounter;
            }

            return null;
        }

        void PersistRun()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PersistMapRun();
        }

        void FrameCamera()
        {
            if (_mapCam != null && _token != null && _token.camTarget != null)
            {
                _mapCam.SetTarget(_token.camTarget);
                return;
            }

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
            float spanZ = Mathf.Max(1f, max.z - min.z);
            float height = Mathf.Max(_cameraHeight, spanZ * 0.55f + 6f);
            float distance = Mathf.Max(_cameraDistance, spanZ * 0.45f + 6f);
            cameraRef.orthographic = false;
            cameraRef.transform.position = center + new Vector3(0f, height, -distance);
            cameraRef.transform.rotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            cameraRef.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            cameraRef.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
