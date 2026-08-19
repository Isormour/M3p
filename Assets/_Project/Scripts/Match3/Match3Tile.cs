using System;
using M3P;
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

        const int UpgradeSlotCount = 4;
        const float UpgradeSlotY = -0.38f;
        const float UpgradeSlotSpacing = 0.22f;
        const float UpgradeSlotScale = 0.48f;
        const float UpgradeSlotZ = -0.022f;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int TypeId { get; private set; }

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
            LayoutUpgradeBar();
            ApplyUpgradeSlots();
        }

        void OnValidate()
        {
            LayoutUpgradeBar();
        }

        public void Initialize(Match3Board board, int x, int y, int typeId, int[] upgradeIds = null)
        {
            X = x;
            Y = y;
            TypeId = typeId;
            UpgradeIds = CloneUpgradeIds(upgradeIds);
            _baseScale = transform.localScale;
            ApplyFlags(false, false, false, false, true, true);
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
            LayoutUpgradeBar();
            bool showBar = HasAnyUpgrade();
            for (int i = 0; i < UpgradeSlotCount; i++)
                ApplyUpgradeSlot(SlotAt(i), i, showBar);
        }

        /// <summary>
        /// Four upgrade icons sit in a row along the bottom of the gem, matching the
        /// tile-upgrade concept art (not the old compass layout).
        /// </summary>
        void LayoutUpgradeBar()
        {
            float startX = -UpgradeSlotSpacing * (UpgradeSlotCount - 1) * 0.5f;
            for (int i = 0; i < UpgradeSlotCount; i++)
            {
                TileSlot slot = SlotAt(i);
                if (slot == null)
                    continue;

                Transform slotTransform = slot.transform;
                slotTransform.localPosition = new Vector3(
                    startX + i * UpgradeSlotSpacing,
                    UpgradeSlotY,
                    UpgradeSlotZ);
                slotTransform.localScale = Vector3.one * UpgradeSlotScale;
            }
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

        void ApplyUpgradeSlot(TileSlot slot, int index, bool showBar)
        {
            if (slot == null)
                return;

            if (!showBar)
            {
                slot.Hide();
                return;
            }

            int upgradeId = UpgradeIdAt(index);
            Sprite icon = null;
            if (upgradeId != TileUpgradeConfig.InvalidUpgradeId
                && _upgrades != null
                && _upgrades.TryGetUpgrade(upgradeId, out TileUpgradeDefinition upgrade))
            {
                icon = upgrade.Icon;
            }

            slot.Show(icon);
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
