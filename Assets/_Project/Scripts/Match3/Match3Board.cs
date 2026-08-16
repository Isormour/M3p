using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using M3P;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Match3
{
    public enum BoardGravity
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }

    public class Match3Board : MonoBehaviour
    {
        /// <summary>Shortest run that counts as a match.</summary>
        public const int MinimumMatchSize = 3;

        [Header("Board")]
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float tileSpacing = 1.1f;

        [Header("Animation")]
        [SerializeField] private float swapDuration = 0.12f;
        [SerializeField] private float fallDuration = 0.15f;

        [Header("Events")]
        [SerializeField] UnityEvent<int> _onComboChanged;
        [SerializeField] UnityEvent<int> _onBestComboChanged;
        [SerializeField] UnityEvent<int> _onTilePointsChanged;
        [SerializeField] UnityEvent<int, int> _onDestroyedTypeTotalChanged;
        [SerializeField] UnityEvent<int, int> _onTilesDestroyedInWave;

        private GameConfig _config;
        private Match3Tile[,] _tiles;
        private readonly List<TileSpawnSpec> _spawnSpecs = new List<TileSpawnSpec>();
        private bool _isResolving;
        private int _currentCombo;
        private int _bestCombo;

        private readonly Dictionary<int, int> _destroyedTypeCountsThisResolve = new Dictionary<int, int>();
        /// <summary>Counts cleared in the current cascade wave only (merged into totals after each wave).</summary>
        private readonly Dictionary<int, int> _destroyedThisWave = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _totalDestroyedByTypeAllTime = new Dictionary<int, int>();
        private readonly HashSet<Match3Tile> _matchedTilesBuffer = new HashSet<Match3Tile>();
        private ReadOnlyDictionary<int, int> _lastDestroyedTypeCounts;
        private int _tilePoints;
        private BoardGravity? _pendingGravity;
        private BoardGravity _activeGravity = BoardGravity.Down;
        private BoardTileSnapshot[,] _rewindSnapshot;
        private BoardGravity? _rewindPendingGravity;
        private bool _hasRewindSnapshot;

        struct BoardTileSnapshot
        {
            public int TypeId;
            public int[] UpgradeIds;
            public bool IsLocked;
            public bool IsNegative;
            public bool IsBlockade;
            public bool IsEnemyElement;
            public bool AllowsColorChange;
            public bool CanDestroy;
        }

        /// <summary>
        /// Per-type counts from the most recent completed match resolution (after swap or full cascade). Empty if the last swap had no matches.
        /// </summary>
        public IReadOnlyDictionary<int, int> LastDestroyedTypeCounts => _lastDestroyedTypeCounts ?? EmptyDestroyedCounts;

        /// <summary>Running score: each clear wave contributes <c>-2 + (tiles cleared that wave)</c>.</summary>
        public int TilePoints => _tilePoints;

        /// <summary>Highest combo reached in the current board session.</summary>
        public int BestCombo => _bestCombo;

        /// <summary>Fired once a played card action and every cascade it triggered have finished resolving.</summary>
        public event Action BoardActionResolved;

        /// <summary>Fired when the player clicks a tile. Targeting is owned by whoever is playing cards.</summary>
        public event Action<Match3Tile> TileClicked;

        /// <summary>
        /// Fired once per cascade wave with the match groups cleared in that wave. Tiles are already
        /// destroyed when this runs, so only read <see cref="MatchGroup.TypeId"/> and <see cref="MatchGroup.Size"/>.
        /// </summary>
        public event Action<IReadOnlyList<MatchGroup>> MatchWaveCompleted;

        /// <summary>Fired for each tile right before it is destroyed.</summary>
        public event Action<Vector3, int> TileDestroyed;

        /// <summary>When false, mouse input does not affect the board.</summary>
        public bool AllowPlayerInput { get; set; } = true;

        /// <summary>True while tiles are swapping or cascades are resolving.</summary>
        public bool IsResolving => _isResolving;

        public int Width => width;

        public int Height => height;

        public int TileTypeCount => _config != null ? _config.TileTypeCount : 0;

        public bool IsInsideBoard(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public Match3Tile GetTile(int x, int y)
        {
            return _tiles != null && IsInsideBoard(x, y) ? _tiles[x, y] : null;
        }

        public static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        public static bool AreHorizontallyAdjacent(Vector2Int first, Vector2Int second)
        {
            return first.y == second.y && Mathf.Abs(first.x - second.x) == 1;
        }

        public static bool AreVerticallyAdjacent(Vector2Int first, Vector2Int second)
        {
            return first.x == second.x && Mathf.Abs(first.y - second.y) == 1;
        }

        public static bool AreDiagonallyAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) == 1 && Mathf.Abs(first.y - second.y) == 1;
        }

        public static bool AreDistantLineNeighbors(Vector2Int first, Vector2Int second)
        {
            bool sameRow = first.y == second.y && Mathf.Abs(first.x - second.x) == 2;
            bool sameColumn = first.x == second.x && Mathf.Abs(first.y - second.y) == 2;
            return sameRow || sameColumn;
        }

        public string GetTileTypeName(int typeId)
        {
            Match3TileTypeDefinition definition = _config != null ? _config.GetTileType(typeId) : null;
            return definition != null ? definition.name : string.Empty;
        }

        public bool CanRecolorTile(int x, int y)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && tile.CanRecolor;
        }

        public bool CanDestroyTile(int x, int y)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && tile.CanDestroy;
        }

        public bool CanPurgeTile(int x, int y)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && tile.IsPurgeable;
        }

        public bool CanMoveTile(int x, int y)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && tile.CanMove;
        }

        public UnityEvent<int, int> TilesDestroyedInWave => _onTilesDestroyedInWave;

        public Color GetTileTypeColor(int typeId)
        {
            return _config != null ? _config.GetTileTypeColor(typeId) : Color.white;
        }

        public Sprite GetTileTypeSprite(int typeId)
        {
            return _config != null ? _config.GetTileTypeSprite(typeId) : null;
        }

        public int GetTileTypeId(Match3TileTypeDefinition tileType)
        {
            return _config != null ? _config.GetTileTypeId(tileType) : -1;
        }

        public TileTypeGraphics GetTileTypeRuneGraphics(int typeId)
        {
            return _config != null ? _config.GetTileTypeRuneGraphics(typeId) : null;
        }

        public TileTypeGraphics GetTileTypeTileGraphics(int typeId)
        {
            return _config != null ? _config.GetTileTypeTileGraphics(typeId) : null;
        }

        private static readonly ReadOnlyDictionary<int, int> EmptyDestroyedCounts =
            new ReadOnlyDictionary<int, int>(new Dictionary<int, int>());

        private void Update()
        {
            if (_tiles == null || !AllowPlayerInput || _isResolving || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            TryHandleMouseSelection();
        }

        private void Start()
        {
            if (!TryResolveConfig(out string configError))
            {
                Debug.LogError(configError);
                enabled = false;
                return;
            }

            _tiles = new Match3Tile[width, height];
            BuildInitialBoard();
            RaiseComboChanged(0);
            RaiseTilePointsChanged();
        }

        public void OnTileClicked(Match3Tile tile)
        {
            if (!AllowPlayerInput || _isResolving || tile == null)
            {
                return;
            }

            TileClicked?.Invoke(tile);
        }

        private void BuildInitialBoard()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileSpawnSpec spec = GetRandomSpecAvoidingImmediateMatch(x, y);
                    SpawnTile(x, y, spec.TypeId, spec.UpgradeIds);
                }
            }
        }

        private TileSpawnSpec GetRandomSpecAvoidingImmediateMatch(int x, int y)
        {
            int attempts = 0;
            TileSpawnSpec spec;

            do
            {
                spec = PickSpawnSpec();
                attempts++;
            }
            while (CreatesInitialMatch(x, y, spec.TypeId) && attempts < 16);

            return spec;
        }

        private bool CreatesInitialMatch(int x, int y, int typeId)
        {
            if (x >= 2 &&
                _tiles[x - 1, y] != null &&
                _tiles[x - 2, y] != null &&
                _tiles[x - 1, y].TypeId == typeId &&
                _tiles[x - 2, y].TypeId == typeId)
            {
                return true;
            }

            if (y >= 2 &&
                _tiles[x, y - 1] != null &&
                _tiles[x, y - 2] != null &&
                _tiles[x, y - 1].TypeId == typeId &&
                _tiles[x, y - 2].TypeId == typeId)
            {
                return true;
            }

            return false;
        }

        private void SpawnTile(int x, int y, int typeId, int[] upgradeIds = null)
        {
            Match3TileTypeDefinition definition = _config.GetTileType(typeId);
            GameObject prefab = definition.Prefab;
            Vector3 worldPos = GridToWorld(x, y);
            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            Match3Tile tile = instance.GetComponent<Match3Tile>();

            if (tile == null)
            {
                tile = instance.AddComponent<Match3Tile>();
            }

            if (instance.GetComponent<Collider2D>() == null)
            {
                instance.AddComponent<BoxCollider2D>();
            }

            tile.Initialize(this, x, y, typeId, upgradeIds);
            tile.ApplyGraphics(definition, _config != null ? _config.TileUpgrades : null);
            _tiles[x, y] = tile;
        }

        /// <summary>
        /// Runs one card action and everything it sets off, then reports completion through
        /// <see cref="BoardActionResolved"/>. Card actions are never rolled back: a swap that forms no
        /// match still stands.
        /// </summary>
        public IEnumerator ExecuteActionRoutine(
            BoardActionLogic logic,
            IReadOnlyList<Vector2Int> targets,
            int extraChoice = 0)
        {
            if (logic == null || _tiles == null || _isResolving)
            {
                yield break;
            }

            _isResolving = true;
            _currentCombo = 0;
            _destroyedTypeCountsThisResolve.Clear();
            RaiseComboChanged(0);

            bool isRewind = logic is RewindBoardLogic;
            if (!isRewind)
            {
                CaptureRewindSnapshot();
            }

            yield return StartCoroutine(logic.ExecuteRoutine(this, targets, extraChoice));

            if (logic.ResolvesMatchesAfterExecute)
            {
                yield return StartCoroutine(ResolveCascadesRoutine());
                if (_destroyedTypeCountsThisResolve.Count > 0)
                {
                    _pendingGravity = null;
                }
            }

            _activeGravity = BoardGravity.Down;
            _lastDestroyedTypeCounts = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>(_destroyedTypeCountsThisResolve));
            _isResolving = false;
            BoardActionResolved?.Invoke();
        }

        /// <summary>
        /// Settles the board after an action: drops tiles into gaps, refills, then clears every match
        /// until none remain. Each wave reports its groups so damage can be scored per match.
        /// </summary>
        private IEnumerator ResolveCascadesRoutine()
        {
            _activeGravity = _pendingGravity ?? BoardGravity.Down;
            yield return StartCoroutine(CollapseAlongGravity());
            yield return StartCoroutine(RefillBoard());

            List<MatchGroup> groups = FindAllMatchGroups();
            while (groups.Count > 0)
            {
                _currentCombo++;
                if (_currentCombo > _bestCombo)
                {
                    _bestCombo = _currentCombo;
                    _onBestComboChanged?.Invoke(_bestCombo);
                }

                RaiseComboChanged(_currentCombo);
                CollectUniqueTiles(groups, _matchedTilesBuffer);
                ClearTiles(_matchedTilesBuffer);
                MatchWaveCompleted?.Invoke(groups);
                AccumulateTilePointsForWave(_destroyedThisWave);
                RaiseTilePointsChanged();
                yield return StartCoroutine(CollapseAlongGravity());
                yield return StartCoroutine(RefillBoard());
                groups = FindAllMatchGroups();
            }
        }

        /// <summary>Swaps two tiles unconditionally. Adjacency is the caller's rule, not the board's.</summary>
        public IEnumerator SwapRoutine(Vector2Int first, Vector2Int second)
        {
            Match3Tile firstTile = GetTile(first.x, first.y);
            Match3Tile secondTile = GetTile(second.x, second.y);

            if (firstTile == null || secondTile == null || firstTile == secondTile)
            {
                yield break;
            }

            _tiles[first.x, first.y] = secondTile;
            _tiles[second.x, second.y] = firstTile;

            firstTile.SetCoordinates(second.x, second.y);
            secondTile.SetCoordinates(first.x, first.y);

            Coroutine firstMove = StartCoroutine(AnimateMove(firstTile.transform, GridToWorld(second.x, second.y), swapDuration));
            Coroutine secondMove = StartCoroutine(AnimateMove(secondTile.transform, GridToWorld(first.x, first.y), swapDuration));
            yield return firstMove;
            yield return secondMove;
        }

        /// <summary>Slides a whole row sideways, wrapping the tile pushed off the edge around to the other side.</summary>
        public IEnumerator ShiftRowRoutine(int y, int direction)
        {
            if (_tiles == null || y < 0 || y >= height || direction == 0)
            {
                yield break;
            }

            int step = direction > 0 ? 1 : -1;
            Match3Tile[] before = new Match3Tile[width];
            for (int x = 0; x < width; x++)
            {
                before[x] = _tiles[x, y];
            }

            List<Coroutine> moves = new List<Coroutine>();
            for (int x = 0; x < width; x++)
            {
                int sourceX = ((x - step) % width + width) % width;
                Match3Tile tile = before[sourceX];
                _tiles[x, y] = tile;

                if (tile == null)
                {
                    continue;
                }

                tile.SetCoordinates(x, y);
                Vector3 target = GridToWorld(x, y);

                if (Mathf.Abs(x - sourceX) > 1)
                {
                    tile.transform.position = target;
                    continue;
                }

                moves.Add(StartCoroutine(AnimateMove(tile.transform, target, swapDuration)));
            }

            for (int i = 0; i < moves.Count; i++)
            {
                yield return moves[i];
            }
        }

        /// <summary>
        /// Destroys tiles outright. They still pay mana, but because no <see cref="MatchGroup"/> is formed
        /// they trigger no basic attack — destruction cards feed skills rather than dealing damage.
        /// </summary>
        public void DestroyTiles(IReadOnlyList<Vector2Int> cells, bool grantEnergy = true)
        {
            if (_tiles == null || cells == null)
            {
                return;
            }

            _matchedTilesBuffer.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Match3Tile tile = GetTile(cells[i].x, cells[i].y);
                if (tile != null)
                {
                    _matchedTilesBuffer.Add(tile);
                }
            }

            ClearTiles(_matchedTilesBuffer, grantEnergy);
        }

        /// <summary>
        /// Removes a purgeable object. Overlay blockades leave the tile underneath; a whole negative
        /// tile is deleted without energy or shards.
        /// </summary>
        public void PurgeTile(Vector2Int cell)
        {
            Match3Tile tile = GetTile(cell.x, cell.y);
            if (tile == null || !tile.IsPurgeable)
            {
                return;
            }

            if (tile.IsBlockade && !tile.IsNegative && !tile.IsEnemyElement)
            {
                tile.ClearNegativeOverlay();
                return;
            }

            DestroyTiles(new[] { cell }, grantEnergy: false);
        }

        public void SetPendingGravity(BoardGravity gravity)
        {
            _pendingGravity = gravity;
        }

        /// <summary>Cycles tiles along <paramref name="cells"/>: the tile at index i moves to index i + 1.</summary>
        public IEnumerator CycleCellsRoutine(IReadOnlyList<Vector2Int> cells)
        {
            if (_tiles == null || cells == null || cells.Count < 2)
            {
                yield break;
            }

            int count = cells.Count;
            Match3Tile[] moving = new Match3Tile[count];
            for (int i = 0; i < count; i++)
            {
                moving[i] = GetTile(cells[i].x, cells[i].y);
                if (moving[i] == null || !moving[i].CanMove)
                {
                    yield break;
                }
            }

            for (int i = 0; i < count; i++)
            {
                Vector2Int destination = cells[(i + 1) % count];
                _tiles[destination.x, destination.y] = moving[i];
                moving[i].SetCoordinates(destination.x, destination.y);
            }

            List<Coroutine> moves = new List<Coroutine>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2Int destination = cells[(i + 1) % count];
                moves.Add(StartCoroutine(AnimateMove(moving[i].transform, GridToWorld(destination.x, destination.y), swapDuration)));
            }

            for (int i = 0; i < moves.Count; i++)
            {
                yield return moves[i];
            }
        }

        public IEnumerator ShuffleMovableTilesRoutine()
        {
            if (_tiles == null)
            {
                yield break;
            }

            List<Match3Tile> movable = new List<Match3Tile>();
            List<Vector2Int> cells = new List<Vector2Int>();
            CollectMovableTiles(movable, cells);
            if (movable.Count < 2)
            {
                yield break;
            }

            Dictionary<Match3Tile, Vector3> startPositions = new Dictionary<Match3Tile, Vector3>(movable.Count);
            for (int i = 0; i < movable.Count; i++)
            {
                startPositions[movable[i]] = movable[i].transform.position;
            }

            const int maxAttempts = 32;
            bool foundLayout = false;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                ShuffleList(movable);
                ApplyTilesToCells(movable, cells);
                if (!HasAnyMatch() && HasLegalSwapMove())
                {
                    foundLayout = true;
                    break;
                }
            }

            if (!foundLayout)
            {
                ApplyTilesToCells(movable, cells);
            }

            List<Coroutine> moves = new List<Coroutine>(movable.Count);
            for (int i = 0; i < movable.Count; i++)
            {
                Match3Tile tile = movable[i];
                tile.transform.position = startPositions[tile];
                moves.Add(StartCoroutine(AnimateMove(tile.transform, GridToWorld(tile.X, tile.Y), swapDuration)));
            }

            for (int i = 0; i < moves.Count; i++)
            {
                yield return moves[i];
            }
        }

        public bool RestoreRewindSnapshot()
        {
            if (!_hasRewindSnapshot || _rewindSnapshot == null || _tiles == null)
            {
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Match3Tile tile = _tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    _tiles[x, y] = null;
                    Destroy(tile.gameObject);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    BoardTileSnapshot snapshot = _rewindSnapshot[x, y];
                    if (snapshot.TypeId < 0)
                    {
                        continue;
                    }

                    SpawnTile(x, y, snapshot.TypeId, snapshot.UpgradeIds);
                    Match3Tile spawned = _tiles[x, y];
                    spawned?.ApplyFlags(
                        snapshot.IsLocked,
                        snapshot.IsNegative,
                        snapshot.IsBlockade,
                        snapshot.IsEnemyElement,
                        snapshot.AllowsColorChange,
                        snapshot.CanDestroy);
                }
            }

            _pendingGravity = _rewindPendingGravity;
            _hasRewindSnapshot = false;
            _rewindSnapshot = null;
            return true;
        }

        /// <summary>Replaces the tile at a cell with a fresh one of another type, keeping the correct prefab.</summary>
        public bool SetTileType(int x, int y, int typeId)
        {
            Match3Tile tile = GetTile(x, y);
            if (tile == null || _config == null || _config.GetTileType(typeId) == null)
            {
                return false;
            }

            int[] upgradeIds = tile.UpgradeIds;
            _tiles[x, y] = null;
            Destroy(tile.gameObject);
            SpawnTile(x, y, typeId, upgradeIds);
            return true;
        }

        /// <summary>
        /// Every maximal run of <see cref="MinimumMatchSize"/> or more same-type tiles, one entry per line.
        /// A tile sitting on an L or T intersection belongs to two groups.
        /// </summary>
        private List<MatchGroup> FindAllMatchGroups()
        {
            List<MatchGroup> groups = new List<MatchGroup>();

            for (int y = 0; y < height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= width; x++)
                {
                    if (x < width && ContinuesRun(_tiles[x, y], _tiles[x - 1, y]))
                    {
                        continue;
                    }

                    int runLength = x - runStart;
                    if (runLength >= MinimumMatchSize)
                    {
                        List<Match3Tile> tiles = new List<Match3Tile>(runLength);
                        for (int i = runStart; i < x; i++)
                        {
                            tiles.Add(_tiles[i, y]);
                        }

                        groups.Add(new MatchGroup(tiles[0].TypeId, MatchOrientation.Horizontal, tiles));
                    }

                    runStart = x;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= height; y++)
                {
                    if (y < height && ContinuesRun(_tiles[x, y], _tiles[x, y - 1]))
                    {
                        continue;
                    }

                    int runLength = y - runStart;
                    if (runLength >= MinimumMatchSize)
                    {
                        List<Match3Tile> tiles = new List<Match3Tile>(runLength);
                        for (int i = runStart; i < y; i++)
                        {
                            tiles.Add(_tiles[x, i]);
                        }

                        groups.Add(new MatchGroup(tiles[0].TypeId, MatchOrientation.Vertical, tiles));
                    }

                    runStart = y;
                }
            }

            return groups;
        }

        private static bool ContinuesRun(Match3Tile current, Match3Tile previous)
        {
            return current != null && previous != null && current.TypeId == previous.TypeId;
        }

        private static void CollectUniqueTiles(List<MatchGroup> groups, HashSet<Match3Tile> destination)
        {
            destination.Clear();

            for (int i = 0; i < groups.Count; i++)
            {
                IReadOnlyList<Match3Tile> tiles = groups[i].Tiles;
                for (int t = 0; t < tiles.Count; t++)
                {
                    destination.Add(tiles[t]);
                }
            }
        }

        /// <summary>
        /// Removes tiles and pays out their mana. Shared by cascade waves and by cards that destroy
        /// tiles directly, so both routes grant mana identically.
        /// </summary>
        private void ClearTiles(HashSet<Match3Tile> tiles, bool grantEnergy = true)
        {
            if (grantEnergy)
                _config?.TileUpgrades?.ExpandClears(this, tiles);

            _destroyedThisWave.Clear();

            BattleCharacter player = BattleManager.Instance != null ? BattleManager.Instance.Player : null;
            BattleCharacter opponent = BattleManager.Instance != null ? BattleManager.Instance.ActiveEnemy : null;

            foreach (Match3Tile tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                int typeId = tile.TypeId;
                if (grantEnergy)
                {
                    AddDestroyedCount(typeId, _destroyedThisWave);
                    AddDestroyedCount(typeId, _destroyedTypeCountsThisResolve);
                    _config?.TileUpgrades?.ApplyCleared(
                        tile.UpgradeIds,
                        new TileUpgradeContext(player, opponent, typeId, tile.transform.position));
                }

                TileDestroyed?.Invoke(tile.transform.position, typeId);

                _tiles[tile.X, tile.Y] = null;
                Destroy(tile.gameObject);
            }

            if (!grantEnergy || _destroyedThisWave.Count == 0)
            {
                return;
            }

            AccumulateDestroyedTypesIntoTotal(_destroyedThisWave);
            RaiseDestroyedTypeTotalsChanged(_destroyedThisWave);
            RaiseTilesDestroyedInWave(_destroyedThisWave);
        }

        private static void AddDestroyedCount(int typeId, Dictionary<int, int> target)
        {
            if (target.TryGetValue(typeId, out int count))
            {
                target[typeId] = count + 1;
            }
            else
            {
                target[typeId] = 1;
            }
        }

        private void TryHandleMouseSelection()
        {
            Camera cameraRef = Camera.main;
            if (cameraRef == null)
            {
                return;
            }

            Ray ray = cameraRef.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hitInfo = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            Collider2D hit = hitInfo.collider;

            if (hit == null)
            {
                return;
            }

            Match3Tile tile = hit.GetComponent<Match3Tile>();
            if (tile != null)
            {
                OnTileClicked(tile);
            }
        }

        private IEnumerator CollapseAlongGravity()
        {
            List<Coroutine> moveCoroutines = new List<Coroutine>();

            if (_activeGravity == BoardGravity.Left || _activeGravity == BoardGravity.Right)
            {
                for (int y = 0; y < height; y++)
                {
                    PackLine(false, y, _activeGravity == BoardGravity.Left, moveCoroutines);
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    PackLine(true, x, _activeGravity == BoardGravity.Down, moveCoroutines);
                }
            }

            for (int i = 0; i < moveCoroutines.Count; i++)
            {
                yield return moveCoroutines[i];
            }
        }

        private IEnumerator RefillBoard()
        {
            List<Coroutine> moveCoroutines = new List<Coroutine>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_tiles[x, y] != null)
                    {
                        continue;
                    }

                    TileSpawnSpec spec = PickSpawnSpec();
                    SpawnTile(x, y, spec.TypeId, spec.UpgradeIds);
                    Match3Tile tile = _tiles[x, y];
                    tile.transform.position = GetRefillSpawnPosition(x, y);
                    Vector3 targetPos = GridToWorld(x, y);
                    moveCoroutines.Add(StartCoroutine(AnimateMove(tile.transform, targetPos, fallDuration)));
                }
            }

            for (int i = 0; i < moveCoroutines.Count; i++)
            {
                yield return moveCoroutines[i];
            }
        }

        private IEnumerator AnimateMove(Transform tileTransform, Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = tileTransform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                tileTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            tileTransform.position = targetPosition;
        }

        private Vector3 GridToWorld(int x, int y)
        {
            return transform.position + new Vector3(x * tileSpacing, y * tileSpacing, 0f);
        }

        private void RaiseComboChanged(int comboValue)
        {
            _onComboChanged?.Invoke(comboValue);
        }

        private void RaiseTilePointsChanged()
        {
            _onTilePointsChanged?.Invoke(_tilePoints);
        }

        private void RaiseDestroyedTypeTotalsChanged(Dictionary<int, int> deltaByType)
        {
            foreach (KeyValuePair<int, int> kv in deltaByType)
            {
                if (_totalDestroyedByTypeAllTime.TryGetValue(kv.Key, out int total))
                    _onDestroyedTypeTotalChanged?.Invoke(kv.Key, total);
            }
        }

        private void RaiseTilesDestroyedInWave(Dictionary<int, int> destroyedThisWave)
        {
            foreach (KeyValuePair<int, int> kv in destroyedThisWave)
                _onTilesDestroyedInWave?.Invoke(kv.Key, kv.Value);
        }

        private void AccumulateDestroyedTypesIntoTotal(Dictionary<int, int> deltaByType)
        {
            foreach (KeyValuePair<int, int> kv in deltaByType)
            {
                if (_totalDestroyedByTypeAllTime.TryGetValue(kv.Key, out int total))
                {
                    _totalDestroyedByTypeAllTime[kv.Key] = total + kv.Value;
                }
                else
                {
                    _totalDestroyedByTypeAllTime[kv.Key] = kv.Value;
                }
            }
        }

        private void AccumulateTilePointsForWave(Dictionary<int, int> destroyedByTypeThisWave)
        {
            int destroyedAmount = SumValueCounts(destroyedByTypeThisWave);
            _tilePoints += -2 + destroyedAmount;
        }

        private static int SumValueCounts(Dictionary<int, int> destroyedByType)
        {
            int sum = 0;
            foreach (KeyValuePair<int, int> kv in destroyedByType)
            {
                sum += kv.Value;
            }

            return sum;
        }

        Vector3 GetRefillSpawnPosition(int x, int y)
        {
            switch (_activeGravity)
            {
                case BoardGravity.Up:
                    return GridToWorld(x, -1);
                case BoardGravity.Left:
                    return GridToWorld(width + 1, y);
                case BoardGravity.Right:
                    return GridToWorld(-1, y);
                default:
                    return GridToWorld(x, height + 1);
            }
        }

        void PackLine(bool vertical, int line, bool towardMin, List<Coroutine> moveCoroutines)
        {
            int length = vertical ? height : width;
            int cursor = 0;
            while (cursor < length)
            {
                int x = vertical ? line : cursor;
                int y = vertical ? cursor : line;
                if (IsLockedCell(x, y))
                {
                    cursor++;
                    continue;
                }

                int segmentStart = cursor;
                while (cursor < length)
                {
                    int cx = vertical ? line : cursor;
                    int cy = vertical ? cursor : line;
                    if (IsLockedCell(cx, cy))
                    {
                        break;
                    }

                    cursor++;
                }

                PackSegment(vertical, line, segmentStart, cursor, towardMin, moveCoroutines);
            }
        }

        void PackSegment(
            bool vertical,
            int line,
            int start,
            int end,
            bool towardMin,
            List<Coroutine> moveCoroutines)
        {
            List<Match3Tile> packed = new List<Match3Tile>(end - start);
            if (towardMin)
            {
                for (int i = start; i < end; i++)
                {
                    Match3Tile tile = GetLineTile(vertical, line, i);
                    if (tile != null)
                    {
                        packed.Add(tile);
                    }
                }
            }
            else
            {
                for (int i = end - 1; i >= start; i--)
                {
                    Match3Tile tile = GetLineTile(vertical, line, i);
                    if (tile != null)
                    {
                        packed.Add(tile);
                    }
                }
            }

            for (int i = start; i < end; i++)
            {
                SetLineTile(vertical, line, i, null);
            }

            for (int i = 0; i < packed.Count; i++)
            {
                int dest = towardMin ? start + i : end - 1 - i;
                Match3Tile tile = packed[i];
                int destX = vertical ? line : dest;
                int destY = vertical ? dest : line;
                _tiles[destX, destY] = tile;
                if (tile.X == destX && tile.Y == destY)
                {
                    continue;
                }

                tile.SetCoordinates(destX, destY);
                moveCoroutines.Add(StartCoroutine(AnimateMove(tile.transform, GridToWorld(destX, destY), fallDuration)));
            }
        }

        Match3Tile GetLineTile(bool vertical, int line, int index)
        {
            return vertical ? _tiles[line, index] : _tiles[index, line];
        }

        void SetLineTile(bool vertical, int line, int index, Match3Tile tile)
        {
            if (vertical)
            {
                _tiles[line, index] = tile;
            }
            else
            {
                _tiles[index, line] = tile;
            }
        }

        bool IsLockedCell(int x, int y)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && !tile.CanMove;
        }

        void CaptureRewindSnapshot()
        {
            _rewindSnapshot = new BoardTileSnapshot[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Match3Tile tile = _tiles[x, y];
                    if (tile == null)
                    {
                        _rewindSnapshot[x, y] = new BoardTileSnapshot { TypeId = -1 };
                        continue;
                    }

                    _rewindSnapshot[x, y] = new BoardTileSnapshot
                    {
                        TypeId = tile.TypeId,
                        UpgradeIds = tile.UpgradeIds,
                        IsLocked = tile.IsLocked,
                        IsNegative = tile.IsNegative,
                        IsBlockade = tile.IsBlockade,
                        IsEnemyElement = tile.IsEnemyElement,
                        AllowsColorChange = tile.AllowsColorChange,
                        CanDestroy = tile.CanDestroy
                    };
                }
            }

            _rewindPendingGravity = _pendingGravity;
            _hasRewindSnapshot = true;
        }

        void CollectMovableTiles(List<Match3Tile> movable, List<Vector2Int> cells)
        {
            movable.Clear();
            cells.Clear();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Match3Tile tile = _tiles[x, y];
                    if (tile == null || !tile.CanMove)
                    {
                        continue;
                    }

                    movable.Add(tile);
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        void ApplyTilesToCells(List<Match3Tile> tiles, List<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                Match3Tile tile = tiles[i];
                _tiles[cell.x, cell.y] = tile;
                tile.SetCoordinates(cell.x, cell.y);
            }
        }

        static void ShuffleList<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        bool HasAnyMatch()
        {
            return FindAllMatchGroups().Count > 0;
        }

        bool HasLegalSwapMove()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!CanMoveTile(x, y))
                    {
                        continue;
                    }

                    if (x + 1 < width && CanMoveTile(x + 1, y) && WouldSwapCreateMatch(cell, new Vector2Int(x + 1, y)))
                    {
                        return true;
                    }

                    if (y + 1 < height && CanMoveTile(x, y + 1) && WouldSwapCreateMatch(cell, new Vector2Int(x, y + 1)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool WouldSwapCreateMatch(Vector2Int first, Vector2Int second)
        {
            Match3Tile firstTile = GetTile(first.x, first.y);
            Match3Tile secondTile = GetTile(second.x, second.y);
            if (firstTile == null || secondTile == null || firstTile.TypeId == secondTile.TypeId)
            {
                return false;
            }

            _tiles[first.x, first.y] = secondTile;
            _tiles[second.x, second.y] = firstTile;
            bool createsMatch = FormsMatchAt(first.x, first.y, secondTile.TypeId) ||
                                FormsMatchAt(second.x, second.y, firstTile.TypeId);
            _tiles[first.x, first.y] = firstTile;
            _tiles[second.x, second.y] = secondTile;
            return createsMatch;
        }

        bool FormsMatchAt(int x, int y, int typeId)
        {
            int horizontal = 1;
            for (int i = x - 1; i >= 0 && TileHasType(i, y, typeId); i--)
            {
                horizontal++;
            }

            for (int i = x + 1; i < width && TileHasType(i, y, typeId); i++)
            {
                horizontal++;
            }

            int vertical = 1;
            for (int i = y - 1; i >= 0 && TileHasType(x, i, typeId); i--)
            {
                vertical++;
            }

            for (int i = y + 1; i < height && TileHasType(x, i, typeId); i++)
            {
                vertical++;
            }

            return horizontal >= MinimumMatchSize || vertical >= MinimumMatchSize;
        }

        bool TileHasType(int x, int y, int typeId)
        {
            Match3Tile tile = GetTile(x, y);
            return tile != null && tile.TypeId == typeId;
        }

        private bool TryResolveConfig(out string message)
        {
            if (GameManager.Instance == null)
            {
                message = "GameManager instance is missing.";
                return false;
            }

            _config = GameManager.Instance.Config;
            if (_config == null)
            {
                message = "GameManager has no GameConfig assigned.";
                return false;
            }

            if (_config.TileTypeCount < 3)
            {
                message = "GameConfig must define at least 3 tile types.";
                return false;
            }

            BuildSpawnPool();
            if (_spawnSpecs.Count == 0)
            {
                message = "No tile types are available to spawn. Assign a tile deck on the profile, or tile types on GameConfig.";
                return false;
            }

            Match3TileTypeDefinition[] tileTypes = _config.TileTypes;
            for (int i = 0; i < tileTypes.Length; i++)
            {
                Match3TileTypeDefinition def = tileTypes[i];
                if (def == null)
                {
                    message = "Tile type definition at index " + i + " is null.";
                    return false;
                }

                if (def.Prefab == null)
                {
                    message = "Tile type definition '" + def.name + "' has no prefab.";
                    return false;
                }
            }

            message = null;
            return true;
        }

        void BuildSpawnPool()
        {
            _spawnSpecs.Clear();

            PlayerProfile profile = GameManager.Instance != null
                ? GameManager.Instance.ProfileManager?.CurrentProfile
                : null;
            _config.ResolveTileDeckSpawns(profile, _spawnSpecs);

            if (_spawnSpecs.Count > 0)
                return;

            for (int i = 0; i < _config.TileTypeCount; i++)
            {
                if (_config.GetTileType(i) != null)
                    _spawnSpecs.Add(new TileSpawnSpec(i, Array.Empty<int>()));
            }
        }

        TileSpawnSpec PickSpawnSpec()
        {
            if (_spawnSpecs.Count == 0)
                return new TileSpawnSpec(Random.Range(0, Mathf.Max(1, _config.TileTypeCount)), Array.Empty<int>());

            return _spawnSpecs[Random.Range(0, _spawnSpecs.Count)];
        }
    }
}
