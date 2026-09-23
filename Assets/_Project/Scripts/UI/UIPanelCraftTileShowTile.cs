using Match3;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Forge preview on the tile-crafting panel: workpiece on the anvil, four upgrade slots,
    /// and the enchantment before/after plus effect copy.
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

        /// <summary>
        /// Same corners as the board tile: Up is bottom-right, Down bottom-left, Left top-left, Right top-right.
        /// </summary>
        static readonly Vector2[] AfterCornerAnchors =
        {
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        const float AfterCornerSize = 36f;

        [SerializeField] Sprite[] WorkpieceSprites;
        [SerializeField] Sprite SlotEmptySprite;
        [SerializeField] Sprite SlotSelectedSprite;

        Image _baseImage;
        Image _beforeImage;
        Image _afterImage;
        Image _afterBonus;
        TextMeshProUGUI _effectName;
        TextMeshProUGUI _effectDescription;
        readonly Image[] _afterCorners = new Image[SlotCount];
        readonly Image[] _slotFrames = new Image[SlotCount];
        readonly Image[] _slotIcons = new Image[SlotCount];
        bool _resolved;
        bool _buttonsBound;

        public event Action<int> SlotClicked;

        void Awake()
        {
            EnsureResolved();
            BindSlotButtons();
        }

        void OnValidate()
        {
            _resolved = false;
            EnsureResolved();
        }

        void OnDestroy()
        {
            SlotClicked = null;
        }

        /// <summary>
        /// Paints the forge workpiece, enchantment preview, and each slot's BonusImg.
        /// Empty slots hide the icon. <paramref name="highlightedSlot"/> uses the selected ring.
        /// </summary>
        public void Show(Match3TileTypeDefinition tile, IReadOnlyList<TileUpgradeDefinition> upgrades, int highlightedSlot = -1)
        {
            EnsureResolved();
            BindSlotButtons();
            ApplyTile(tile);
            ApplyUpgrades(upgrades, highlightedSlot);
            ApplyEnchantmentPreview(tile, upgrades, highlightedSlot);
        }

        public void Clear()
        {
            Show(null, null, -1);
        }

        void ApplyTile(Match3TileTypeDefinition tile)
        {
            if (_baseImage == null)
                return;

            Sprite workpiece = ResolveWorkpiece(tile);
            TileTypeGraphics graphics = tile != null ? tile.TileGraphics : null;
            Sprite fallback = graphics != null ? graphics.MainSprite : null;
            Sprite sprite = workpiece != null ? workpiece : fallback;

            _baseImage.sprite = sprite;
            _baseImage.enabled = sprite != null;
            _baseImage.color = Color.white;
            _baseImage.preserveAspect = true;
            _baseImage.material = workpiece != null ? null : (tile != null ? tile.UIMaterial : null);
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

        void ApplyEnchantmentPreview(Match3TileTypeDefinition tile, IReadOnlyList<TileUpgradeDefinition> upgrades, int highlightedSlot)
        {
            TileTypeGraphics graphics = tile != null ? tile.TileGraphics : null;
            Sprite tileSprite = graphics != null ? graphics.MainSprite : null;
            Material tileMaterial = tile != null ? tile.UIMaterial : null;
            TileUpgradeDefinition preview = ResolvePreviewUpgrade(upgrades, highlightedSlot);

            ApplyPreviewImage(_beforeImage, tileSprite, tileMaterial);
            ApplyPreviewImage(_afterImage, tileSprite, tileMaterial);
            ApplyAfterCorners(upgrades);

            if (_effectName != null)
                _effectName.text = preview != null ? preview.DisplayName : string.Empty;

            if (_effectDescription != null)
            {
                FitDescriptionBox();
                _effectDescription.alignment = TextAlignmentOptions.Top;
                _effectDescription.textWrappingMode = TextWrappingModes.Normal;
                _effectDescription.overflowMode = TextOverflowModes.Ellipsis;
                _effectDescription.enableAutoSizing = true;
                _effectDescription.fontSizeMin = 13f;
                _effectDescription.fontSizeMax = 20f;
                _effectDescription.text = BuildCombinedDescription(upgrades);
            }
        }

        void ApplyAfterCorners(IReadOnlyList<TileUpgradeDefinition> upgrades)
        {
            EnsureAfterCorners();
            if (_afterBonus != null)
                _afterBonus.enabled = false;

            for (int i = 0; i < SlotCount; i++)
            {
                Image corner = _afterCorners[i];
                if (corner == null)
                    continue;

                TileUpgradeDefinition upgrade = upgrades != null && i < upgrades.Count ? upgrades[i] : null;
                Sprite icon = upgrade != null ? upgrade.Icon : null;
                corner.sprite = icon;
                corner.enabled = icon != null;
                corner.color = Color.white;
                corner.preserveAspect = true;
                corner.raycastTarget = false;
            }
        }

        void EnsureAfterCorners()
        {
            if (_afterImage == null)
                return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_afterCorners[i] != null)
                    continue;

                string cornerName = "AfterCorner" + i;
                Transform existing = _afterImage.transform.Find(cornerName);
                Image image;
                if (existing != null)
                {
                    image = existing.GetComponent<Image>();
                    if (image == null)
                        image = existing.gameObject.AddComponent<Image>();
                }
                else
                {
                    var cornerObject = new GameObject(cornerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    cornerObject.transform.SetParent(_afterImage.transform, false);
                    image = cornerObject.GetComponent<Image>();
                }

                RectTransform rect = image.rectTransform;
                Vector2 anchor = AfterCornerAnchors[i];
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.sizeDelta = new Vector2(AfterCornerSize, AfterCornerSize);
                rect.anchoredPosition = Vector2.zero;
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.enabled = false;
                _afterCorners[i] = image;
            }
        }

        void FitDescriptionBox()
        {
            if (_effectDescription == null)
                return;

            RectTransform desc = _effectDescription.rectTransform;
            RectTransform parent = desc.parent as RectTransform;
            if (parent == null || desc.anchorMin.y < 0.9f)
                return;

            float parentHeight = Mathf.Approximately(parent.anchorMin.y, parent.anchorMax.y)
                ? parent.sizeDelta.y
                : parent.rect.height;
            float limitFromTop = parentHeight - 80f;
            Transform cost = parent.Find("LabelCost");
            if (cost is RectTransform costRect && costRect.anchorMax.y < 0.5f)
            {
                float costTop = costRect.anchoredPosition.y + costRect.sizeDelta.y * (1f - costRect.pivot.y);
                limitFromTop = parentHeight - costTop - 8f;
            }

            float top = -desc.anchoredPosition.y;
            float height = Mathf.Max(desc.sizeDelta.y, limitFromTop - top);
            if (height > 64f)
                desc.sizeDelta = new Vector2(desc.sizeDelta.x, height);
        }

        static string BuildCombinedDescription(IReadOnlyList<TileUpgradeDefinition> upgrades)
        {
            if (upgrades == null || upgrades.Count == 0)
                return string.Empty;

            var text = new StringBuilder();
            for (int i = 0; i < upgrades.Count; i++)
            {
                TileUpgradeDefinition upgrade = upgrades[i];
                if (upgrade == null || string.IsNullOrEmpty(upgrade.Description))
                    continue;

                bool alreadyListed = false;
                for (int j = 0; j < i; j++)
                {
                    if (upgrades[j] == upgrade)
                    {
                        alreadyListed = true;
                        break;
                    }
                }

                if (alreadyListed)
                    continue;

                int copies = 1;
                for (int j = i + 1; j < upgrades.Count; j++)
                {
                    if (upgrades[j] == upgrade)
                        copies++;
                }

                if (text.Length > 0)
                    text.Append('\n');
                text.Append(FormatStackedDescription(upgrade.Description, copies));
            }

            return text.ToString();
        }

        /// <summary>
        /// Two copies of "restore 1 HP" read as "restore 2 HP". The same applies to AP and shield.
        /// </summary>
        static string FormatStackedDescription(string description, int copies)
        {
            if (copies <= 1 || string.IsNullOrEmpty(description))
                return description ?? string.Empty;

            if (!TryReadFirstInteger(description, out int start, out int length, out int value))
                return description;

            int total = value * copies;
            string number = total.ToString();
            string scaled = description.Remove(start, length).Insert(start, number);
            return PluralizeCountedNoun(scaled, start + number.Length, total);
        }

        static bool TryReadFirstInteger(string text, out int start, out int length, out int value)
        {
            start = 0;
            length = 0;
            value = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    continue;

                if (i > 0 && char.IsDigit(text[i - 1]))
                    continue;

                int end = i + 1;
                while (end < text.Length && char.IsDigit(text[end]))
                    end++;

                if (!int.TryParse(text.Substring(i, end - i), out value))
                    return false;

                start = i;
                length = end - i;
                return true;
            }

            return false;
        }

        static string PluralizeCountedNoun(string text, int index, int total)
        {
            if (total == 1)
                return text;

            int i = index;
            for (int word = 0; word < 3 && i < text.Length; word++)
            {
                while (i < text.Length && !char.IsLetter(text[i]))
                    i++;

                int wordStart = i;
                while (i < text.Length && char.IsLetter(text[i]))
                    i++;

                if (wordStart == i)
                    break;

                string plural = Plural(text.Substring(wordStart, i - wordStart));
                if (plural != null)
                    return text.Remove(wordStart, i - wordStart).Insert(wordStart, plural);
            }

            return text;
        }

        static string Plural(string word)
        {
            switch (word)
            {
                case "point": return "points";
                case "shield": return "shields";
                case "stack": return "stacks";
                default: return null;
            }
        }

        static TileUpgradeDefinition ResolvePreviewUpgrade(IReadOnlyList<TileUpgradeDefinition> upgrades, int highlightedSlot)
        {
            if (upgrades == null)
                return null;

            if (highlightedSlot >= 0 && highlightedSlot < upgrades.Count && upgrades[highlightedSlot] != null)
                return upgrades[highlightedSlot];

            for (int i = upgrades.Count - 1; i >= 0; i--)
            {
                if (upgrades[i] != null)
                    return upgrades[i];
            }

            return null;
        }

        static void ApplyPreviewImage(Image image, Sprite sprite, Material material)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
            image.preserveAspect = true;
            image.material = material;
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
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        void ApplySlotHighlight(int slot, bool highlighted)
        {
            Image frame = _slotFrames[slot];
            if (frame == null)
                return;

            Sprite sprite = highlighted && SlotSelectedSprite != null ? SlotSelectedSprite : SlotEmptySprite;
            if (sprite != null)
                frame.sprite = sprite;

            frame.color = Color.white;
            frame.preserveAspect = true;
        }

        Sprite ResolveWorkpiece(Match3TileTypeDefinition tile)
        {
            if (tile == null || WorkpieceSprites == null)
                return null;

            string key = tile.name;
            if (string.IsNullOrEmpty(key))
                return null;

            for (int i = 0; i < WorkpieceSprites.Length; i++)
            {
                Sprite sprite = WorkpieceSprites[i];
                if (sprite == null || string.IsNullOrEmpty(sprite.name))
                    continue;

                if (sprite.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return sprite;
            }

            return null;
        }

        void EnsureResolved()
        {
            if (_resolved)
                return;

            Transform searchRoot = FindPanelRoot();

            Transform baseTransform = FindNamed(searchRoot, "Base");
            if (baseTransform != null)
                _baseImage = baseTransform.GetComponent<Image>();

            _beforeImage = FindImage(searchRoot, "BeforeImg");
            _afterImage = FindImage(searchRoot, "AfterImg");
            _afterBonus = FindImage(searchRoot, "AfterBonusImg");
            _effectName = FindText(searchRoot, "LabelEffectName");
            _effectDescription = FindText(searchRoot, "LabelEffectDescription");

            int foundSlots = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                Transform slot = FindNamed(searchRoot, SlotObjectNames[i]);
                if (slot == null)
                    continue;

                _slotFrames[i] = slot.GetComponent<Image>();
                if (_slotFrames[i] != null)
                    foundSlots++;

                Transform bonus = FindNamed(slot, "BonusImg");
                if (bonus != null)
                    _slotIcons[i] = bonus.GetComponent<Image>();

                if (_slotIcons[i] != null)
                    _slotIcons[i].raycastTarget = false;
            }

            // Slots live on the enchantment panel, not under this forge object. Wait until
            // the panel root is available so a too-early Awake does not cache a miss forever.
            _resolved = foundSlots == SlotCount;
        }

        Transform FindPanelRoot()
        {
            UIPanelTileCrafting panel = GetComponentInParent<UIPanelTileCrafting>(true);
            if (panel != null)
                return panel.transform;

            return transform.root != null ? transform.root : transform;
        }

        void BindSlotButtons()
        {
            if (_buttonsBound)
                return;

            int bound = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotFrames[i] == null)
                    continue;

                Button button = _slotFrames[i].GetComponent<Button>();
                if (button == null)
                    button = _slotFrames[i].gameObject.AddComponent<Button>();

                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.RemoveAllListeners();

                int slot = i;
                button.onClick.AddListener(() => SlotClicked?.Invoke(slot));
                bound++;
            }

            _buttonsBound = bound == SlotCount;
        }

        static Image FindImage(Transform root, string childName)
        {
            Transform found = FindNamed(root, childName);
            return found != null ? found.GetComponent<Image>() : null;
        }

        static TextMeshProUGUI FindText(Transform root, string childName)
        {
            Transform found = FindNamed(root, childName);
            return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
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
