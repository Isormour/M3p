using M3P;
using System;
using UnityEngine;

namespace Match3
{
    public class Match3Tile : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private SpriteRenderer _tileRenderer;
        [SerializeField] private TileSlot _slotUp;
        [SerializeField] private TileSlot _slotDown;
        [SerializeField] private TileSlot _slotLeft;
        [SerializeField] private TileSlot _slotRight;
        [SerializeField] private TileArrow _arrowUp;
        [SerializeField] private TileArrow _arrowDown;
        [SerializeField] private TileArrow _arrowLeft;
        [SerializeField] private TileArrow _arrowRight;

        const int UpgradeSlotCount = 4;

        /// <summary>Colour multiplied into the sprite while the tile is marked for removal.</summary>
        static readonly Color CrackedTint = new Color(0.45f, 0.45f, 0.5f, 0.85f);

        public int X { get; private set; }
        public int Y { get; private set; }
        public int TypeId { get; private set; }

        /// <summary>
        /// Identity that survives moves, so the predicted board can follow this exact tile rather than
        /// whatever currently sits in a cell.
        /// </summary>
        public int TileId { get; private set; }

        /// <summary>
        /// Marked by a Destroy card. The mark travels with the tile through later cards in the sequence;
        /// the tile itself only leaves the board once every card has run.
        /// </summary>
        public bool IsCracked { get; private set; }

        /// <summary>Crafted upgrades copied from the owned tile that spawned this piece.</summary>
        public int[] UpgradeIds { get; private set; } = Array.Empty<int>();

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
        TileUpgradeConfig _upgrades;

        void Awake()
        {
            ApplyUpgradeSlots();
        }

        public void Initialize(Match3Board board, int x, int y, int typeId, int[] upgradeIds = null)
        {
            X = x;
            Y = y;
            TypeId = typeId;
            UpgradeIds = CloneUpgradeIds(upgradeIds);
            _baseScale = transform.localScale;
            ApplyFlags(false, false, false, false, true, true);
            SetCracked(false);
            ApplyUpgradeSlots();
        }

        public void ApplyGraphics(Match3TileTypeDefinition definition, TileUpgradeConfig upgrades = null)
        {
            if (upgrades != null)
                _upgrades = upgrades;

            if (definition == null)
            {
                Debug.LogWarning($"{nameof(Match3Tile)}: Cannot apply graphics, definition is null.", this);
                ApplyUpgradeSlots();
                return;
            }

            ApplyGraphicsToRenderer(_tileRenderer, definition.TileGraphics);
            ApplyUpgradeSlots();
        }

        void ApplyUpgradeSlots()
        {
            bool showBar = HasAnyUpgrade();
            for (int i = 0; i < UpgradeSlotCount; i++)
                ApplyUpgradeSlot(i, showBar);
        }

        TileSlot SlotAt(int index)
        {
            switch (index)
            {
                case 0: return _slotUp;
                case 1: return _slotDown;
                case 2: return _slotLeft;
                case 3: return _slotRight;
                default: return null;
            }
        }

        TileArrow ArrowAt(int index)
        {
            switch (index)
            {
                case 0: return _arrowUp;
                case 1: return _arrowDown;
                case 2: return _arrowLeft;
                case 3: return _arrowRight;
                default: return null;
            }
        }

        void ApplyUpgradeSlot(int index, bool showBar)
        {
            TileSlot slot = SlotAt(index);
            TileUpgradeDefinition upgrade = showBar ? UpgradeAt(index) : null;

            if (slot != null)
            {
                if (upgrade == null && !showBar)
                    slot.Hide();
                else
                    slot.Show(upgrade != null ? upgrade.Icon : null);
            }

            TileArrow arrow = ArrowAt(index);
            if (arrow != null)
                arrow.SetEnabled(upgrade != null && upgrade.Logic != null && upgrade.Logic.AffectsNeighbor);
        }

        TileUpgradeDefinition UpgradeAt(int index)
        {
            int upgradeId = UpgradeIdAt(index);
            if (upgradeId == TileUpgradeConfig.InvalidUpgradeId || _upgrades == null)
                return null;

            return _upgrades.TryGetUpgrade(upgradeId, out TileUpgradeDefinition upgrade) ? upgrade : null;
        }

        bool HasAnyUpgrade()
        {
            if (UpgradeIds == null)
                return false;

            for (int i = 0; i < UpgradeIds.Length; i++)
            {
                if (UpgradeIds[i] != TileUpgradeConfig.InvalidUpgradeId)
                    return true;
            }

            return false;
        }

        int UpgradeIdAt(int index)
        {
            if (UpgradeIds == null || index < 0 || index >= UpgradeIds.Length)
                return TileUpgradeConfig.InvalidUpgradeId;

            return UpgradeIds[index];
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

        public void SetTileId(int tileId)
        {
            TileId = tileId;
        }

        public void SetCracked(bool cracked)
        {
            IsCracked = cracked;

            if (_tileRenderer != null)
                _tileRenderer.color = cracked ? CrackedTint : Color.white;
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

        static int[] CloneUpgradeIds(int[] upgradeIds)
        {
            if (upgradeIds == null || upgradeIds.Length == 0)
                return Array.Empty<int>();

            int[] copy = new int[upgradeIds.Length];
            Array.Copy(upgradeIds, copy, upgradeIds.Length);
            return copy;
        }
    }
}
