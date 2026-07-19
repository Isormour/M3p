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
    public class Match3Board : MonoBehaviour
    {
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
        private Match3Tile _selectedTile;
        private bool _isResolving;
        private int _currentCombo;
        private int _bestCombo;

        private readonly Dictionary<int, int> _destroyedTypeCountsThisResolve = new Dictionary<int, int>();
        /// <summary>Counts cleared in the current cascade wave only (merged into totals after each wave).</summary>
        private readonly Dictionary<int, int> _destroyedThisWave = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _totalDestroyedByTypeAllTime = new Dictionary<int, int>();
        private ReadOnlyDictionary<int, int> _lastDestroyedTypeCounts;
        private int _tilePoints;

        /// <summary>
        /// Per-type counts from the most recent completed match resolution (after swap or full cascade). Empty if the last swap had no matches.
        /// </summary>
        public IReadOnlyDictionary<int, int> LastDestroyedTypeCounts => _lastDestroyedTypeCounts ?? EmptyDestroyedCounts;

        /// <summary>Running score: each clear wave contributes <c>-2 + (tiles cleared that wave)</c>.</summary>
        public int TilePoints => _tilePoints;

        /// <summary>Highest combo reached in the current board session.</summary>
        public int BestCombo => _bestCombo;

        /// <summary>Fired after a swap attempt finishes (matched cascades or swap cancelled).</summary>
        public event Action MoveCycleCompleted;

        /// <summary>Fired once per cascade wave with the total number of tiles cleared in that wave.</summary>
        public event Action<int> MatchWaveCompleted;

        /// <summary>When false, mouse input does not affect the board.</summary>
        public bool AllowPlayerInput { get; set; } = true;

        /// <summary>True while tiles are swapping or cascades are resolving.</summary>
        public bool IsResolving => _isResolving;

        public int TileTypeCount => _config != null ? _config.TileTypeCount : 0;

        public UnityEvent<int, int> TilesDestroyedInWave => _onTilesDestroyedInWave;

        public Color GetTileTypeColor(int typeId)
        {
            return _config != null ? _config.GetTileTypeColor(typeId) : Color.white;
        }

        public Sprite GetTileTypeSprite(int typeId)
        {
            return _config != null ? _config.GetTileTypeSprite(typeId) : null;
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

            if (_selectedTile == null)
            {
                _selectedTile = tile;
                _selectedTile.SetSelected(true);
                return;
            }

            if (_selectedTile == tile)
            {
                _selectedTile.SetSelected(false);
                _selectedTile = null;
                return;
            }

            if (!AreAdjacent(_selectedTile, tile))
            {
                _selectedTile.SetSelected(false);
                _selectedTile = tile;
                _selectedTile.SetSelected(true);
                return;
            }

            StartCoroutine(TrySwapAndResolve(_selectedTile, tile));
            _selectedTile.SetSelected(false);
            _selectedTile = null;
        }

        private void BuildInitialBoard()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int typeId = GetRandomTypeAvoidingImmediateMatch(x, y);
                    SpawnTile(x, y, typeId);
                }
            }
        }

        private int GetRandomTypeAvoidingImmediateMatch(int x, int y)
        {
            int attempts = 0;
            int typeId;

            do
            {
                typeId = Random.Range(0, _config.TileTypeCount);
                attempts++;
            }
            while (CreatesInitialMatch(x, y, typeId) && attempts < 16);

            return typeId;
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

        private void SpawnTile(int x, int y, int typeId)
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

            tile.Initialize(this, x, y, typeId);
            tile.ApplyMeshColor(definition.Color);
            _tiles[x, y] = tile;
        }

        private IEnumerator TrySwapAndResolve(Match3Tile first, Match3Tile second)
        {
            _isResolving = true;
            _currentCombo = 0;
            _destroyedTypeCountsThisResolve.Clear();
            RaiseComboChanged(0);
            yield return StartCoroutine(SwapTiles(first, second, swapDuration));

            HashSet<Match3Tile> matches = FindAllMatches();
            if (matches.Count == 0)
            {
                yield return StartCoroutine(SwapTiles(first, second, swapDuration));
                _lastDestroyedTypeCounts = EmptyDestroyedCounts;
                _isResolving = false;
                MoveCycleCompleted?.Invoke();
                yield break;
            }

            while (matches.Count > 0)
            {
                _currentCombo++;
                if (_currentCombo > _bestCombo)
                {
                    _bestCombo = _currentCombo;
                    _onBestComboChanged?.Invoke(_bestCombo);
                }

                RaiseComboChanged(_currentCombo);
                ClearMatches(matches);
                AccumulateDestroyedTypesIntoTotal(_destroyedThisWave);
                RaiseDestroyedTypeTotalsChanged(_destroyedThisWave);
                RaiseTilesDestroyedInWave(_destroyedThisWave);
                MatchWaveCompleted?.Invoke(SumValueCounts(_destroyedThisWave));
                AccumulateTilePointsForWave(_destroyedThisWave);
                RaiseTilePointsChanged();
                yield return StartCoroutine(CollapseColumns());
                yield return StartCoroutine(RefillBoard());
                matches = FindAllMatches();
            }

            _lastDestroyedTypeCounts = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>(_destroyedTypeCountsThisResolve));
            _isResolving = false;
            MoveCycleCompleted?.Invoke();
        }

        /// <summary>
        /// Picks a random adjacent pair and starts swap resolution (for AI).
        /// </summary>
        /// <returns>True if a swap coroutine was started.</returns>
        public bool TryRandomLegalSwap()
        {
            if (_isResolving || _tiles == null || width < 1 || height < 1)
            {
                return false;
            }

            for (int attempt = 0; attempt < 128; attempt++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                Match3Tile a = _tiles[x, y];
                if (a == null)
                {
                    continue;
                }

                int dir = Random.Range(0, 4);
                int nx = x;
                int ny = y;
                if (dir == 0)
                {
                    nx++;
                }
                else if (dir == 1)
                {
                    nx--;
                }
                else if (dir == 2)
                {
                    ny++;
                }
                else
                {
                    ny--;
                }

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                {
                    continue;
                }

                Match3Tile b = _tiles[nx, ny];
                if (b == null)
                {
                    continue;
                }

                StartCoroutine(TrySwapAndResolve(a, b));
                return true;
            }

            return false;
        }

        private IEnumerator SwapTiles(Match3Tile first, Match3Tile second, float duration)
        {
            int firstX = first.X;
            int firstY = first.Y;
            int secondX = second.X;
            int secondY = second.Y;

            _tiles[firstX, firstY] = second;
            _tiles[secondX, secondY] = first;

            first.SetCoordinates(secondX, secondY);
            second.SetCoordinates(firstX, firstY);

            Vector3 firstTarget = GridToWorld(first.X, first.Y);
            Vector3 secondTarget = GridToWorld(second.X, second.Y);

            Coroutine firstMove = StartCoroutine(AnimateMove(first.transform, firstTarget, duration));
            Coroutine secondMove = StartCoroutine(AnimateMove(second.transform, secondTarget, duration));
            yield return firstMove;
            yield return secondMove;
        }

        private HashSet<Match3Tile> FindAllMatches()
        {
            HashSet<Match3Tile> matches = new HashSet<Match3Tile>();

            for (int y = 0; y < height; y++)
            {
                int runLength = 1;
                for (int x = 1; x < width; x++)
                {
                    Match3Tile current = _tiles[x, y];
                    Match3Tile previous = _tiles[x - 1, y];

                    if (current != null && previous != null && current.TypeId == previous.TypeId)
                    {
                        runLength++;
                    }
                    else
                    {
                        if (runLength >= 3)
                        {
                            for (int i = 0; i < runLength; i++)
                            {
                                matches.Add(_tiles[x - 1 - i, y]);
                            }
                        }
                        runLength = 1;
                    }
                }

                if (runLength >= 3)
                {
                    for (int i = 0; i < runLength; i++)
                    {
                        matches.Add(_tiles[width - 1 - i, y]);
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                int runLength = 1;
                for (int y = 1; y < height; y++)
                {
                    Match3Tile current = _tiles[x, y];
                    Match3Tile previous = _tiles[x, y - 1];

                    if (current != null && previous != null && current.TypeId == previous.TypeId)
                    {
                        runLength++;
                    }
                    else
                    {
                        if (runLength >= 3)
                        {
                            for (int i = 0; i < runLength; i++)
                            {
                                matches.Add(_tiles[x, y - 1 - i]);
                            }
                        }
                        runLength = 1;
                    }
                }

                if (runLength >= 3)
                {
                    for (int i = 0; i < runLength; i++)
                    {
                        matches.Add(_tiles[x, height - 1 - i]);
                    }
                }
            }

            return matches;
        }

        private void ClearMatches(HashSet<Match3Tile> matches)
        {
            _destroyedThisWave.Clear();
            foreach (Match3Tile tile in matches)
            {
                if (tile == null)
                {
                    continue;
                }

                if (tile == _selectedTile)
                {
                    _selectedTile = null;
                }

                int typeId = tile.TypeId;
                AddDestroyedCount(typeId, _destroyedThisWave);
                AddDestroyedCount(typeId, _destroyedTypeCountsThisResolve);

                _tiles[tile.X, tile.Y] = null;
                Destroy(tile.gameObject);
            }
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

        private IEnumerator CollapseColumns()
        {
            List<Coroutine> moveCoroutines = new List<Coroutine>();

            for (int x = 0; x < width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < height; y++)
                {
                    Match3Tile tile = _tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    if (writeY != y)
                    {
                        _tiles[x, writeY] = tile;
                        _tiles[x, y] = null;
                        tile.SetCoordinates(x, writeY);
                        Vector3 targetPos = GridToWorld(x, writeY);
                        moveCoroutines.Add(StartCoroutine(AnimateMove(tile.transform, targetPos, fallDuration)));
                    }

                    writeY++;
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

                    int typeId = Random.Range(0, _config.TileTypeCount);
                    SpawnTile(x, y, typeId);
                    Match3Tile tile = _tiles[x, y];
                    Vector3 spawnPos = GridToWorld(x, height + 1);
                    tile.transform.position = spawnPos;
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

        private bool AreAdjacent(Match3Tile first, Match3Tile second)
        {
            int dx = Mathf.Abs(first.X - second.X);
            int dy = Mathf.Abs(first.Y - second.Y);
            return dx + dy == 1;
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
    }
}
