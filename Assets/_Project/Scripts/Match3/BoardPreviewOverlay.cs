using System.Collections;
using System.Collections.Generic;
using M3P;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// While planning, draws a ghost of the new colour on any tile a Recolor will change.
    /// Movement is left to the arc indicators; only a type change on the same tile identity is shown.
    /// </summary>
    [RequireComponent(typeof(Match3Board))]
    public sealed class BoardPreviewOverlay : MonoBehaviour
    {
        [SerializeField] TileGhost _ghostPrefab;

        readonly List<TileGhost> _ghosts = new List<TileGhost>();
        readonly Dictionary<int, Vector2Int> _predictedCellsByTileId = new Dictionary<int, Vector2Int>();

        Match3Board _board;
        CardPlayController _cardPlay;
        Transform _ghostRoot;
        int _usedGhosts;

        void Awake()
        {
            _board = GetComponent<Match3Board>();

            GameObject root = new GameObject("PreviewGhosts");
            root.transform.SetParent(transform, false);
            _ghostRoot = root.transform;
        }

        void OnEnable()
        {
            StartCoroutine(WatchControllerRoutine());
        }

        void OnDisable()
        {
            Unbind();
            HideAll();
        }

        IEnumerator WatchControllerRoutine()
        {
            while (enabled)
            {
                CardPlayController active = BattleManager.Instance != null ? BattleManager.Instance.CardPlay : null;

                if (active != _cardPlay)
                {
                    Unbind();
                    _cardPlay = active;

                    if (_cardPlay != null)
                        _cardPlay.Changed += Rebuild;

                    Rebuild();
                }

                yield return null;
            }
        }

        void Unbind()
        {
            if (_cardPlay != null)
                _cardPlay.Changed -= Rebuild;

            _cardPlay = null;
        }

        void Rebuild()
        {
            _usedGhosts = 0;

            SimBoard predicted = _cardPlay != null ? _cardPlay.PredictedBoard : null;
            bool planning = predicted != null
                && _board != null
                && !_board.IsResolving
                && _cardPlay.HasQueuedCards
                && _ghostPrefab != null;

            if (planning)
                DrawRecolorGhosts(predicted);

            HideUnused();
        }

        void DrawRecolorGhosts(SimBoard predicted)
        {
            _predictedCellsByTileId.Clear();
            for (int x = 0; x < predicted.Width; x++)
            {
                for (int y = 0; y < predicted.Height; y++)
                {
                    SimTile tile = predicted.GetTile(x, y);
                    if (tile != null)
                        _predictedCellsByTileId[tile.Id] = new Vector2Int(x, y);
                }
            }

            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    Match3Tile actual = _board.GetTile(x, y);
                    if (actual == null)
                        continue;

                    if (!_predictedCellsByTileId.TryGetValue(actual.TileId, out Vector2Int destination))
                        continue;

                    SimTile expected = predicted.GetTile(destination.x, destination.y);
                    if (expected == null || expected.TypeId == actual.TypeId)
                        continue;

                    ShowGhost(destination.x, destination.y, expected.TypeId);
                }
            }
        }

        void ShowGhost(int x, int y, int typeId)
        {
            TileTypeGraphics graphics = _board.GetTileTypeTileGraphics(typeId);
            Sprite sprite = graphics != null && graphics.MainSprite != null
                ? graphics.MainSprite
                : _board.GetTileTypeSprite(typeId);

            RentGhost().Present(
                _board.GridToWorld(x, y),
                sprite,
                _board.GetTileTypeColor(typeId));
        }

        TileGhost RentGhost()
        {
            if (_usedGhosts < _ghosts.Count)
            {
                TileGhost existing = _ghosts[_usedGhosts++];
                if (existing != null)
                    return existing;

                _ghosts[_usedGhosts - 1] = CreateGhost();
                return _ghosts[_usedGhosts - 1];
            }

            TileGhost created = CreateGhost();
            _ghosts.Add(created);
            _usedGhosts++;
            return created;
        }

        TileGhost CreateGhost()
        {
            TileGhost ghost = Instantiate(_ghostPrefab, _ghostRoot);
            ghost.transform.localRotation = Quaternion.identity;
            return ghost;
        }

        void HideUnused()
        {
            for (int i = _usedGhosts; i < _ghosts.Count; i++)
            {
                if (_ghosts[i] != null)
                    _ghosts[i].Hide();
            }
        }

        void HideAll()
        {
            _usedGhosts = 0;
            HideUnused();
        }
    }
}
