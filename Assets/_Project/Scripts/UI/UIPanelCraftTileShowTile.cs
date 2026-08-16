using Match3;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Center preview on the tile-crafting panel: the chosen tile type plus four upgrade slots.
    /// Slot clicks bubble to the crafting panel so it can assign or clear upgrades.
    /// </summary>
    public sealed class UIPanelCraftTileShowTile : MonoBehaviour
    {
        public const int SlotCount = OwnedTile.MaxUpgradeCount;

        static readonly string[] SlotObjectNames =
        {
            "SlotUp",
            "SlotDown",
            "SlotLeft",
            "SlotRight"
        };

        Image _baseImage;
        readonly Image[] _slotFrames = new Image[SlotCount];
        readonly Image[] _slotIcons = new Image[SlotCount];
        readonly Color[] _slotFrameColors = new Color[SlotCount];
        bool _resolved;
        bool _buttonsBound;

        public event Action<int> SlotClicked;

        void Awake()
        {
            EnsureResolved();
            BindSlotButtons();
        }

        void OnDestroy()
        {
            SlotClicked = null;
        }

        /// <summary>
        /// Paints the tile graphics on Base with <see cref="Match3TileTypeDefinition.UIMaterial"/>
        /// and each slot's BonusImg. Empty slots hide the icon.
        /// <paramref name="highlightedSlot"/> tints that slot frame so the player can see the target.
        /// </summary>
        public void Show(Match3TileTypeDefinition tile, IReadOnlyList<TileUpgradeDefinition> upgrades, int highlightedSlot = -1)
        {
            EnsureResolved();
            ApplyTile(tile);
            ApplyUpgrades(upgrades, highlightedSlot);
        }

        public void Clear()
        {
            Show(null, null, -1);
        }

        void ApplyTile(Match3TileTypeDefinition tile)
        {
            if (_baseImage == null)
                return;

            TileTypeGraphics graphics = tile != null ? tile.TileGraphics : null;
            Sprite sprite = graphics != null ? graphics.MainSprite : null;

            _baseImage.sprite = sprite;
            _baseImage.enabled = sprite != null;
            _baseImage.color = Color.white;
            _baseImage.material = tile != null ? tile.UIMaterial : null;
        }

        void ApplyUpgrades(IReadOnlyList<TileUpgradeDefinition> upgrades, int highlightedSlot)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                TileUpgradeDefinition upgrade = upgrades != null && i < upgrades.Count ? upgrades[i] : null;
                ApplySlotIcon(i, upgrade);
                ApplySlotHighlight(i, i == highlightedSlot);
            }
        }

        void ApplySlotIcon(int slot, TileUpgradeDefinition upgrade)
        {
            Image icon = _slotIcons[slot];
            if (icon == null)
                return;

            Sprite sprite = upgrade != null ? upgrade.Icon : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = Color.white;
            icon.raycastTarget = false;
        }

        void ApplySlotHighlight(int slot, bool highlighted)
        {
            Image frame = _slotFrames[slot];
            if (frame == null)
                return;

            frame.color = highlighted ? Color.white : _slotFrameColors[slot];
        }

        void EnsureResolved()
        {
            if (_resolved)
                return;

            _resolved = true;

            Transform baseTransform = FindNamed(transform, "Base");
            if (baseTransform != null)
                _baseImage = baseTransform.GetComponent<Image>();

            for (int i = 0; i < SlotCount; i++)
            {
                Transform slot = FindNamed(transform, SlotObjectNames[i]);
                if (slot == null)
                    continue;

                _slotFrames[i] = slot.GetComponent<Image>();
                _slotFrameColors[i] = _slotFrames[i] != null ? _slotFrames[i].color : Color.white;

                Transform bonus = FindNamed(slot, "BonusImg");
                if (bonus != null)
                    _slotIcons[i] = bonus.GetComponent<Image>();

                if (_slotIcons[i] != null)
                    _slotIcons[i].raycastTarget = false;
            }
        }

        void BindSlotButtons()
        {
            if (_buttonsBound)
                return;

            _buttonsBound = true;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotFrames[i] == null)
                    continue;

                Button button = _slotFrames[i].GetComponent<Button>();
                if (button == null)
                    button = _slotFrames[i].gameObject.AddComponent<Button>();

                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };

                int slot = i;
                button.onClick.AddListener(() => SlotClicked?.Invoke(slot));
            }
        }

        static Transform FindNamed(Transform root, string childName)
        {
            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
