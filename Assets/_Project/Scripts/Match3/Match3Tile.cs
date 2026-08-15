using UnityEngine;

namespace Match3
{
    public class Match3Tile : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private SpriteRenderer _tileRenderer;
        [SerializeField] private SpriteRenderer _runeRenderer;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int TypeId { get; private set; }

        /// <summary>Anchored tiles keep their cell during shuffle, gravity and cycles.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>Negative board objects that <c>Purge</c> can remove.</summary>
        public bool IsNegative { get; private set; }

        /// <summary>A blocker occupying this cell. Purge can strip it; colour-change cannot, unless allowed.</summary>
        public bool IsBlockade { get; private set; }

        /// <summary>An opponent-created piece. Purge can remove it; colour-change cannot, unless allowed.</summary>
        public bool IsEnemyElement { get; private set; }

        /// <summary>When false, Change Color and Wild Transmutation cannot target this tile.</summary>
        public bool AllowsColorChange { get; private set; } = true;

        /// <summary>When false, Destroy cards cannot target this tile.</summary>
        public bool CanDestroy { get; private set; } = true;

        public bool CanRecolor => AllowsColorChange;

        public bool IsPurgeable => IsNegative || IsBlockade || IsEnemyElement;

        public bool CanMove => !IsLocked && !IsBlockade;

        Vector3 _baseScale;

        public void Initialize(Match3Board board, int x, int y, int typeId)
        {
            X = x;
            Y = y;
            TypeId = typeId;
            _baseScale = transform.localScale;
            ApplyFlags(false, false, false, false, true, true);
        }

        public void ApplyGraphics(Match3TileTypeDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning($"{nameof(Match3Tile)}: Cannot apply graphics, definition is null.", this);
                return;
            }

            ApplyGraphicsToRenderer(_tileRenderer, definition.TileGraphics);
            ApplyGraphicsToRenderer(_runeRenderer, definition.RuneGraphics);
        }

        private void ApplyGraphicsToRenderer(SpriteRenderer renderer, TileTypeGraphics graphics)
        {
            if (renderer == null || graphics == null)
                return;

            if (graphics.MainSprite != null)
            {
                renderer.sprite = graphics.MainSprite;
            }

            if (graphics.SpriteMaterial != null)
            {
                renderer.material = graphics.SpriteMaterial;
            }

            if (graphics.NormalMap != null && renderer.material != null)
            {
                renderer.material.SetTexture("_BumpMap", graphics.NormalMap);
            }
        }

        public void SetCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetType(int typeId)
        {
            TypeId = typeId;
        }

        public void ApplyFlags(
            bool isLocked,
            bool isNegative,
            bool isBlockade,
            bool isEnemyElement,
            bool allowsColorChange,
            bool canDestroy)
        {
            IsLocked = isLocked;
            IsNegative = isNegative;
            IsBlockade = isBlockade;
            IsEnemyElement = isEnemyElement;
            AllowsColorChange = allowsColorChange;
            CanDestroy = canDestroy;
        }

        public void ClearNegativeOverlay()
        {
            IsNegative = false;
            IsBlockade = false;
            IsEnemyElement = false;
            AllowsColorChange = true;
            CanDestroy = true;
            IsLocked = false;
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? _baseScale * 1.12f : _baseScale;
        }
    }
}
