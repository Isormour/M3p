using Match3;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: pick a tile type, fill the four upgrade slots under the gem, preview the
    /// combined shard cost, and buy a copy with those upgrades attached.
    /// </summary>
    public sealed class UIPanelTileCrafting : UIPanelClosable
    {
        [SerializeField] Transform TileTypesParent;
        [SerializeField] Transform UpgradeTypesParent;
        [SerializeField] UIPanelCraftTilesTileTypeButton TileTypePrefab;
        [SerializeField] Button UpgradeTypePrefab;
        [SerializeField] Transform CostParent;
        [SerializeField] UIPanelPlayerManaBar CostPrefab;
        [SerializeField] UIPanelCraftTileShowTile _ShowTile;
        [SerializeField] Button ConfirmCraftButton;

        [SerializeField] Transform ShardsParent;
        [SerializeField] UISimpleIndicator ShardsIndicatorPrefab;
        [SerializeField] Sprite TileFrameNormal;
        [SerializeField] Sprite TileFrameSelected;
        [SerializeField] Sprite EffectFrameNormal;
        [SerializeField] Sprite EffectFrameSelected;
        [SerializeField] Sprite[] CatalogueTileSprites;
        [SerializeField] Sprite[] CatalogueEffectSprites;
        [SerializeField] Sprite[] ShardSprites;

        readonly List<UIPanelCraftTilesTileTypeButton> _typeViews = new List<UIPanelCraftTilesTileTypeButton>();
        readonly List<Match3TileTypeDefinition> _typeTiles = new List<Match3TileTypeDefinition>();
        readonly List<Button> _upgradeViews = new List<Button>();
        readonly List<TileUpgradeDefinition> _upgradeTypes = new List<TileUpgradeDefinition>();
        readonly List<UIPanelPlayerManaBar> _costViews = new List<UIPanelPlayerManaBar>();
        readonly List<UISimpleIndicator> _shardIndicators = new List<UISimpleIndicator>();
        readonly TileUpgradeDefinition[] _slotUpgrades = new TileUpgradeDefinition[OwnedTile.MaxUpgradeCount];
        readonly List<TileTypeShardCost> _combinedCost = new List<TileTypeShardCost>();

        Match3TileTypeDefinition _selectedTile;
        int _selectedSlot = -1;

        protected override void OnInitialize()
        {
            HideTemplate(TileTypePrefab);
            HideTemplate(UpgradeTypePrefab);
            HideTemplate(CostPrefab);
            HideTemplate(ShardsIndicatorPrefab);

            if (_ShowTile != null)
                _ShowTile.SlotClicked += HandleSlotClicked;

            if (ConfirmCraftButton != null)
                ConfirmCraftButton.onClick.AddListener(HandleConfirmClicked);
        }

        void OnEnable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged += HandleProfileChanged;

            Refresh();
        }

        void OnDisable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= HandleProfileChanged;

            ClearTypeViews();
            ClearUpgradeViews();
            ClearCostViews();
            ClearShardIndicators();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_ShowTile != null)
                _ShowTile.SlotClicked -= HandleSlotClicked;
            if (ConfirmCraftButton != null)
                ConfirmCraftButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static GameConfig Config => GameManager.Instance != null ? GameManager.Instance.Config : null;

        static TileConfig Tiles => Config != null ? Config.Tiles : null;

        static TileUpgradeConfig Upgrades => Config != null ? Config.TileUpgrades : null;

        protected override void ResolveRefs()
        {
            base.ResolveRefs();
            if (TileTypesParent == null)
                TileTypesParent = FindDescendant("TileTypesParent");

            if (UpgradeTypesParent == null)
                UpgradeTypesParent = FindDescendant("UpgradeTypesParent");

            if (CostParent == null)
                CostParent = FindDescendant("CostParent");

            if (ShardsParent == null)
                ShardsParent = FindDescendant("Shards");

            if (_ShowTile == null)
            {
                Transform showTile = FindDescendant("ShowTile");
                if (showTile != null)
                    _ShowTile = showTile.GetComponent<UIPanelCraftTileShowTile>();
            }

            if (ConfirmCraftButton == null)
                ConfirmCraftButton = FindDescendantButton("ConfirmCraftButton")
                    ?? FindDescendantButton("CraftButton");
        }

        void HandleProfileChanged()
        {
            RefreshSelection();
        }

        void Refresh()
        {
            BuildTileTypes();
            BuildUpgradeTypes();
            BuildShardIndicators();
            RefreshSelection();
        }

        void BuildTileTypes()
        {
            ClearTypeViews();

            if (TileTypesParent == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(TileTypesParent)} on the prefab.", this);
                return;
            }

            if (TileTypePrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(TileTypePrefab)} on the prefab.", this);
                return;
            }

            TileConfig tileConfig = Tiles;
            if (tileConfig == null)
                return;

            TileConfig.Entry[] entries = tileConfig.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                Match3TileTypeDefinition tile = entries[i].Tile;
                if (tile == null || entries[i].Id == TileConfig.InvalidTileId)
                    continue;

                UIPanelCraftTilesTileTypeButton view = Instantiate(TileTypePrefab, TileTypesParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftType_{tile.name}";

                Match3TileTypeDefinition captured = tile;
                ConfigureTileTypeButton(view, captured, () => HandleTypeClicked(captured));
                _typeViews.Add(view);
                _typeTiles.Add(captured);
            }
        }

        void BuildUpgradeTypes()
        {
            ClearUpgradeViews();

            if (UpgradeTypesParent == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(UpgradeTypesParent)} on the prefab.", this);
                return;
            }

            if (UpgradeTypePrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(UpgradeTypePrefab)} on the prefab.", this);
                return;
            }

            TileUpgradeConfig upgradeConfig = Upgrades;
            if (upgradeConfig == null)
                return;

            TileUpgradeConfig.Entry[] entries = upgradeConfig.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                TileUpgradeDefinition upgrade = entries[i].Upgrade;
                if (upgrade == null || entries[i].Id == TileUpgradeConfig.InvalidUpgradeId)
                    continue;

                Button view = Instantiate(UpgradeTypePrefab, UpgradeTypesParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftUpgrade_{upgrade.name}";

                TileUpgradeDefinition captured = upgrade;
                ConfigureUpgradeTypeButton(view, captured, () => HandleUpgradeClicked(captured));
                _upgradeViews.Add(view);
                _upgradeTypes.Add(captured);
            }
        }

        void HandleTypeClicked(Match3TileTypeDefinition tile)
        {
            _selectedTile = tile;
            RefreshSelection();
        }

        void HandleUpgradeClicked(TileUpgradeDefinition upgrade)
        {
            int slot = _selectedSlot;
            if (slot < 0 || slot >= _slotUpgrades.Length)
                slot = FindFirstEmptySlot();
            if (slot < 0)
                return;

            _slotUpgrades[slot] = upgrade;
            _selectedSlot = -1;
            RefreshSelection();
        }

        void HandleSlotClicked(int slot)
        {
            if (slot < 0 || slot >= _slotUpgrades.Length)
                return;

            if (_slotUpgrades[slot] != null)
            {
                _slotUpgrades[slot] = null;
                if (_selectedSlot == slot)
                    _selectedSlot = -1;
            }
            else
            {
                _selectedSlot = _selectedSlot == slot ? -1 : slot;
            }

            RefreshSelection();
        }

        void RefreshSelection()
        {
            RebuildCombinedCost();
            ApplySelectedTilePreview();
            BuildCosts();
            RefreshConfirmButton();
            RefreshTypeSelection();
            RefreshUpgradeSelection();
        }

        void ApplySelectedTilePreview()
        {
            if (_ShowTile == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(_ShowTile)} on the prefab.", this);
                return;
            }

            _ShowTile.Show(_selectedTile, _slotUpgrades, _selectedSlot);
        }

        void BuildCosts()
        {
            ClearCostViews();

            if (_selectedTile == null)
                return;

            if (CostParent == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(CostParent)} on the prefab.", this);
                return;
            }

            if (CostPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(CostPrefab)} on the prefab.", this);
                return;
            }

            GameConfig config = Config;
            for (int i = 0; i < _combinedCost.Count; i++)
            {
                TileTypeShardCost cost = _combinedCost[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                int typeId = config != null ? config.GetTileTypeId(cost.TileType) : -1;
                Sprite icon = typeId >= 0 && config != null
                    ? config.GetTileTypeShardIcon(typeId)
                    : cost.TileType.ResolveShardIcon();

                UIPanelPlayerManaBar view = Instantiate(CostPrefab, CostParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftCost_{cost.TileType.name}";
                view.Configure(typeId, icon, cost.TileType.UIMaterial);
                view.SetAmount(cost.Amount);
                _costViews.Add(view);
            }
        }

        void RefreshConfirmButton()
        {
            if (ConfirmCraftButton == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(ConfirmCraftButton)} on the prefab.", this);
                return;
            }

            ConfirmCraftButton.interactable = CanCraftSelected();
        }

        void RefreshTypeSelection()
        {
            for (int i = 0; i < _typeViews.Count; i++)
            {
                UIPanelCraftTilesTileTypeButton view = _typeViews[i];
                if (view == null)
                    continue;

                view.transform.localScale = Vector3.one;
                bool selected = i < _typeTiles.Count && _typeTiles[i] == _selectedTile;
                ApplyFrameSprite(view.background, selected, TileFrameNormal, TileFrameSelected);
            }
        }

        void RefreshUpgradeSelection()
        {
            for (int i = 0; i < _upgradeViews.Count; i++)
            {
                Button view = _upgradeViews[i];
                if (view == null)
                    continue;

                int stacked = i < _upgradeTypes.Count ? CountSlots(_upgradeTypes[i]) : 0;
                ApplyFrameSelection(view, stacked > 0, EffectFrameNormal, EffectFrameSelected);
                ApplyStackCount(view, stacked);
            }
        }

        static void ApplyStackCount(Button view, int count)
        {
            if (view == null)
                return;

            TextMeshProUGUI label = FindCountLabel(view.transform);
            if (label == null)
                return;

            bool show = count > 1;
            label.text = show ? count.ToString() : string.Empty;
            label.gameObject.SetActive(show);

            Transform name = view.transform.Find("Name");
            if (name is RectTransform nameRect)
            {
                Vector2 offsetMax = nameRect.offsetMax;
                offsetMax.x = show ? -36f : -12f;
                nameRect.offsetMax = offsetMax;
            }
        }

        static TextMeshProUGUI FindCountLabel(Transform button)
        {
            Transform count = button.Find("Count");
            return count != null ? count.GetComponent<TextMeshProUGUI>() : null;
        }

        static void ApplyFrameSelection(Button view, bool selected, Sprite normal, Sprite selectedSprite)
        {
            if (view == null)
                return;

            view.transform.localScale = Vector3.one;
            Image frame = view.targetGraphic as Image ?? view.GetComponent<Image>();
            ApplyFrameSprite(frame, selected, normal, selectedSprite);
        }

        static void ApplyFrameSprite(Image frame, bool selected, Sprite normal, Sprite selectedSprite)
        {
            if (frame == null)
                return;

            Sprite sprite = selected && selectedSprite != null ? selectedSprite : normal;
            if (sprite != null)
                frame.sprite = sprite;
        }

        bool CanCraftSelected()
        {
            if (_selectedTile == null)
                return false;

            PlayerProfile profile = Profiles?.CurrentProfile;
            return profile != null && profile.CanAffordCraftCost(_combinedCost);
        }

        void HandleConfirmClicked()
        {
            if (!CanCraftSelected())
                return;

            TileConfig tileConfig = Tiles;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (tileConfig == null || profile == null)
                return;

            int tileId = tileConfig.GetTileId(_selectedTile);
            if (!profile.TryCraftTile(tileId, _combinedCost, CollectSelectedUpgradeIds()))
                return;

            Profiles.Save();
            RefreshSelection();
        }

        int FindFirstEmptySlot()
        {
            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                if (_slotUpgrades[i] == null)
                    return i;
            }

            return -1;
        }

        int CountSlots(TileUpgradeDefinition upgrade)
        {
            if (upgrade == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                if (_slotUpgrades[i] == upgrade)
                    count++;
            }

            return count;
        }

        int[] CollectSelectedUpgradeIds()
        {
            TileUpgradeConfig upgradeConfig = Upgrades;
            if (upgradeConfig == null)
                return Array.Empty<int>();

            int count = 0;
            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                if (_slotUpgrades[i] != null)
                    count++;
            }

            if (count == 0)
                return Array.Empty<int>();

            int[] ids = new int[count];
            int write = 0;
            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                if (_slotUpgrades[i] == null)
                    continue;

                int id = upgradeConfig.GetUpgradeId(_slotUpgrades[i]);
                if (id == TileUpgradeConfig.InvalidUpgradeId)
                    continue;

                ids[write++] = id;
            }

            if (write == ids.Length)
                return ids;

            if (write == 0)
                return Array.Empty<int>();

            int[] trimmed = new int[write];
            Array.Copy(ids, trimmed, write);
            return trimmed;
        }

        void RebuildCombinedCost()
        {
            _combinedCost.Clear();
            if (_selectedTile != null)
                AccumulateCraftCost(_combinedCost, _selectedTile.CraftCost);

            int existing = 0;
            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                TileUpgradeDefinition upgrade = _slotUpgrades[i];
                if (upgrade == null)
                    continue;

                AccumulateCraftCost(_combinedCost, upgrade.GetCraftCost(existing));
                existing++;
            }
        }

        static void AccumulateCraftCost(List<TileTypeShardCost> destination, IReadOnlyList<TileTypeShardCost> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                TileTypeShardCost cost = source[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                int index = IndexOfTileType(destination, cost.TileType);
                if (index >= 0)
                    destination[index] = new TileTypeShardCost(cost.TileType, destination[index].Amount + cost.Amount);
                else
                    destination.Add(cost);
            }
        }

        static int IndexOfTileType(List<TileTypeShardCost> costs, Match3TileTypeDefinition tileType)
        {
            for (int i = 0; i < costs.Count; i++)
            {
                if (costs[i].TileType == tileType)
                    return i;
            }

            return -1;
        }

        void ConfigureTileTypeButton(UIPanelCraftTilesTileTypeButton button, Match3TileTypeDefinition tile, Action onClicked)
        {
            TileTypeGraphics graphics = tile != null ? tile.TileGraphics : null;
            Sprite icon = ResolveNamedSprite(CatalogueTileSprites, tile != null ? tile.name : null)
                ?? (graphics != null ? graphics.MainSprite : null);
            ApplyTileIcon(
                button,
                icon,
                CatalogueTileSprites == null || CatalogueTileSprites.Length == 0
                    ? (tile != null ? tile.UIMaterial : null)
                    : null);
            BindTileClick(button, onClicked);
        }

        void ConfigureUpgradeTypeButton(Button button, TileUpgradeDefinition upgrade, Action onClicked)
        {
            Sprite icon = ResolveNamedSprite(CatalogueEffectSprites, upgrade != null ? upgrade.DisplayName : null)
                ?? ResolveNamedSprite(CatalogueEffectSprites, upgrade != null ? upgrade.name : null)
                ?? (upgrade != null ? upgrade.Icon : null);
            ConfigureCatalogueButton(
                button,
                upgrade != null ? upgrade.DisplayName : string.Empty,
                icon,
                null,
                onClicked);
            EnsureStackCountLabel(button);
        }

        static void EnsureStackCountLabel(Button button)
        {
            if (button == null || button.transform.Find("Count") != null)
                return;

            TextMeshProUGUI name = null;
            TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].gameObject.name == "Name")
                {
                    name = labels[i];
                    break;
                }
            }

            var countObject = new GameObject("Count", typeof(RectTransform));
            countObject.transform.SetParent(button.transform, false);
            TextMeshProUGUI count = countObject.AddComponent<TextMeshProUGUI>();
            if (name != null)
            {
                count.font = name.font;
                count.fontSharedMaterial = name.fontSharedMaterial;
                count.color = name.color;
                count.fontSize = name.fontSize;
            }

            count.alignment = TextAlignmentOptions.MidlineRight;
            count.raycastTarget = false;
            count.text = string.Empty;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = count.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f, 0f);
            rect.sizeDelta = new Vector2(28f, -8f);
            countObject.SetActive(false);
        }

        static void ApplyTileIcon(UIPanelCraftTilesTileTypeButton button, Sprite icon, Material material)
        {
            if (button == null || button.icon == null)
                return;

            Image image = button.icon;
            image.sprite = icon;
            image.material = material;
            image.enabled = icon != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void BindTileClick(UIPanelCraftTilesTileTypeButton button, Action onClicked)
        {
            if (button == null || button.button == null)
                return;

            if (button.button.targetGraphic == null && button.background != null)
                button.button.targetGraphic = button.background;

            if (button.background != null)
                button.background.raycastTarget = true;

            button.button.onClick.AddListener(() => onClicked?.Invoke());
        }

        static void ConfigureCatalogueButton(Button button, string label, Sprite icon, Material iconMaterial, Action onClicked)
        {
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label ?? string.Empty;

            ApplyIcon(button, icon, iconMaterial);
            button.onClick.AddListener(() => onClicked?.Invoke());
        }

        static void ApplyIcon(Button button, Sprite icon, Material material)
        {
            Transform iconTransform = FindNamedChild(button.transform, "Icon");
            Image image = iconTransform != null
                ? iconTransform.GetComponent<Image>()
                : button.GetComponentInChildren<Image>(true);

            if (image == null || image == button.targetGraphic)
            {
                if (iconTransform == null)
                    return;
                image = iconTransform.GetComponent<Image>();
            }

            if (image == null)
                return;

            image.sprite = icon;
            image.material = material;
            image.enabled = icon != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static Sprite ResolveNamedSprite(Sprite[] sprites, string key)
        {
            if (sprites == null || string.IsNullOrEmpty(key))
                return null;

            Sprite fallback = null;
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null || string.IsNullOrEmpty(sprite.name))
                    continue;

                if (sprite.name.Equals(key, StringComparison.OrdinalIgnoreCase)
                    || sprite.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return sprite;

                string token = SpriteToken(sprite.name);
                if (token.Length == 0)
                    continue;

                if (key.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = sprite;
            }

            return fallback;
        }

        static string SpriteToken(string spriteName)
        {
            int dash = spriteName.LastIndexOf('-');
            return dash >= 0 && dash < spriteName.Length - 1
                ? spriteName.Substring(dash + 1)
                : spriteName;
        }

        static Transform FindNamedChild(Transform root, string childName)
        {
            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        void ClearTypeViews()
        {
            for (int i = 0; i < _typeViews.Count; i++)
            {
                if (_typeViews[i] != null)
                    Destroy(_typeViews[i].gameObject);
            }

            _typeViews.Clear();
            _typeTiles.Clear();
        }

        void ClearUpgradeViews()
        {
            for (int i = 0; i < _upgradeViews.Count; i++)
            {
                if (_upgradeViews[i] != null)
                    Destroy(_upgradeViews[i].gameObject);
            }

            _upgradeViews.Clear();
            _upgradeTypes.Clear();
        }

        void ClearCostViews()
        {
            for (int i = 0; i < _costViews.Count; i++)
            {
                if (_costViews[i] != null)
                    Destroy(_costViews[i].gameObject);
            }

            _costViews.Clear();
        }

        void BuildShardIndicators()
        {
            ClearShardIndicators();

            if (ShardsParent == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(ShardsParent)} on the prefab.", this);
                return;
            }

            if (ShardsIndicatorPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelTileCrafting)}: assign {nameof(ShardsIndicatorPrefab)} on the prefab.", this);
                return;
            }

            GameConfig config = Config;
            Match3TileTypeDefinition[] tileTypes = config != null ? config.TileTypes : null;
            if (tileTypes == null)
                return;

            ProfileManager profiles = Profiles;
            for (int i = 0; i < tileTypes.Length; i++)
            {
                Match3TileTypeDefinition tileType = tileTypes[i];
                if (tileType == null)
                    continue;

                UISimpleIndicator indicator = Instantiate(ShardsIndicatorPrefab, ShardsParent);
                indicator.gameObject.SetActive(true);
                indicator.name = $"Shard_{tileType.name}";
                Sprite shardIcon = ResolveNamedSprite(ShardSprites, tileType.name) ?? tileType.ResolveShardIcon();
                indicator.SetIcon(shardIcon);

                string tileKey = tileType.name;
                indicator.Bind(
                    getCurrent: () =>
                    {
                        PlayerProfile profile = Profiles?.CurrentProfile;
                        return profile != null ? profile.GetShards(tileKey) : 0;
                    },
                    getMax: () => 0,
                    subscribeChanged: handler =>
                    {
                        if (profiles != null)
                            profiles.ProfileChanged += handler;
                    },
                    unsubscribeChanged: handler =>
                    {
                        if (profiles != null)
                            profiles.ProfileChanged -= handler;
                    },
                    formatText: (current, _) => current.ToString());

                _shardIndicators.Add(indicator);
            }
        }

        void ClearShardIndicators()
        {
            for (int i = 0; i < _shardIndicators.Count; i++)
            {
                if (_shardIndicators[i] != null)
                    Destroy(_shardIndicators[i].gameObject);
            }

            _shardIndicators.Clear();
        }

        static void HideTemplate(Component template)
        {
            if (template != null && template.gameObject.scene.IsValid())
                template.gameObject.SetActive(false);
        }
    }
}
