using System.Collections;
using System.Collections.Generic;
using M3P;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Draws the predicted board on top of the real one while the player is planning: a translucent ghost
    /// wherever a queued command will change a cell, and a darker mark on tiles a Destroy has cracked.
    /// Refills and cascades are deliberately absent, because the preview must only promise what the
    /// sequence itself determines.
    /// </summary>
    [RequireComponent(typeof(Match3Board))]
    public sealed class BoardPreviewOverlay : MonoBehaviour
    {
        const int GhostSortingOrder = 50;

        static readonly Color GhostTint = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color CrackedTint = new Color(1f, 0.35f, 0.3f, 0.65f);

        readonly List<SpriteRenderer> _ghosts = new List<SpriteRenderer>();

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
            bool planning = predicted != null && _board != null && !_board.IsResolving && _cardPlay.HasQueuedCards;

            if (planning)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    for (int y = 0; y < _board.Height; y++)
                        DrawCellIfChanged(predicted, x, y);
                }
            }

            for (int i = _usedGhosts; i < _ghosts.Count; i++)
            {
                if (_ghosts[i] != null)
                    _ghosts[i].gameObject.SetActive(false);
            }
        }

        void DrawCellIfChanged(SimBoard predicted, int x, int y)
        {
            SimTile expected = predicted.GetTile(x, y);
            Match3Tile actual = _board.GetTile(x, y);

            if (expected == null)
            {
                // A purge is the only command that empties a cell before the board settles.
                if (actual != null)
                    ShowGhost(x, y, null, CrackedTint);

                return;
            }

            bool typeChanged = actual == null || actual.TypeId != expected.TypeId;
            bool movedHere = actual != null && actual.TileId != expected.Id;

            if (expected.IsCracked)
            {
                ShowGhost(x, y, _board.GetTileTypeSprite(expected.TypeId), CrackedTint);
                return;
            }

            if (typeChanged || movedHere)
                ShowGhost(x, y, _board.GetTileTypeSprite(expected.TypeId), GhostTint);
        }

        void ShowGhost(int x, int y, Sprite sprite, Color tint)
        {
            SpriteRenderer ghost = RentGhost();
            ghost.transform.position = _board.GridToWorld(x, y);
            ghost.transform.localScale = Vector3.one * GhostScaleFor(sprite);
            ghost.sprite = sprite;
            ghost.color = tint;
            ghost.enabled = sprite != null;
            ghost.gameObject.SetActive(true);
        }

        /// <summary>Fits the sprite to a cell, since tile prefabs carry their own scale.</summary>
        float GhostScaleFor(Sprite sprite)
        {
            if (sprite == null)
                return 1f;

            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (spriteSize <= 0.0001f)
                return 1f;

            return _board.TileSpacing * 0.9f / spriteSize;
        }

        SpriteRenderer RentGhost()
        {
            if (_usedGhosts < _ghosts.Count)
            {
                SpriteRenderer existing = _ghosts[_usedGhosts++];
                if (existing != null)
                    return existing;

                _ghosts[_usedGhosts - 1] = CreateGhost();
                return _ghosts[_usedGhosts - 1];
            }

            SpriteRenderer created = CreateGhost();
            _ghosts.Add(created);
            _usedGhosts++;
            return created;
        }

        SpriteRenderer CreateGhost()
        {
            GameObject ghost = new GameObject($"Ghost_{_ghosts.Count}");
            ghost.transform.SetParent(_ghostRoot, false);

            SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = GhostSortingOrder;
            return renderer;
        }

        void HideAll()
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (_ghosts[i] != null)
                    _ghosts[i].gameObject.SetActive(false);
            }

            _usedGhosts = 0;
        }
    }
}
