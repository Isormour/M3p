using Match3;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: pick a tile type, optionally fill the four upgrade slots, preview the combined
    /// shard cost, and buy a copy with those upgrades attached.
    /// </summary>
    public sealed class UIPanelTileCrafting : UIPanelClosable
    {
        [SerializeField] Transform TileTypesParent;
        [SerializeField] Transform UpgradeTypesParent;
        [SerializeField] Button TileTypePrefab;
        [SerializeField] Button UpgradeTypePrefab;
        [SerializeField] Transform CostParent;
        [SerializeField] UIPanelPlayerManaBar CostPrefab;
        [SerializeField] UIPanelCraftTileShowTile _ShowTile;
        [SerializeField] Button ConfirmCraftButton;

        [SerializeField] Transform ShardsParent;
        [SerializeField] UISimpleIndicator ShardsIndicatorPrefab;

        readonly List<Button> _typeViews = new List<Button>();
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

                Button view = Instantiate(TileTypePrefab, TileTypesParent);
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
                Button view = _typeViews[i];
                if (view == null)
                    continue;

                bool selected = i < _typeTiles.Count && _typeTiles[i] == _selectedTile;
                view.transform.localScale = selected ? Vector3.one * 1.2f : Vector3.one;
            }
        }

        void RefreshUpgradeSelection()
        {
            for (int i = 0; i < _upgradeViews.Count; i++)
            {
                Button view = _upgradeViews[i];
                if (view == null)
                    continue;

                bool selected = i < _upgradeTypes.Count && SlotIndexOf(_upgradeTypes[i]) >= 0;
                view.transform.localScale = selected ? Vector3.one * 1.2f : Vector3.one;
            }
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

        int SlotIndexOf(TileUpgradeDefinition upgrade)
        {
            if (upgrade == null)
                return -1;

            for (int i = 0; i < _slotUpgrades.Length; i++)
            {
                if (_slotUpgrades[i] == upgrade)
                    return i;
            }

            return -1;
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

        static void ConfigureTileTypeButton(Button button, Match3TileTypeDefinition tile, Action onClicked)
        {
            TileTypeGraphics graphics = tile != null ? tile.TileGraphics : null;
            ConfigureCatalogueButton(
                button,
                tile != null ? tile.name : string.Empty,
                graphics != null ? graphics.MainSprite : null,
                tile != null ? tile.UIMaterial : null,
                onClicked);
        }

        static void ConfigureUpgradeTypeButton(Button button, TileUpgradeDefinition upgrade, Action onClicked)
        {
            ConfigureCatalogueButton(
                button,
                upgrade != null ? upgrade.DisplayName : string.Empty,
                upgrade != null ? upgrade.Icon : null,
                null,
                onClicked);
        }

        static void ConfigureCatalogueButton(Button button, string label, Sprite icon, Material iconMaterial, Action onClicked)
        {
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label ?? string.Empty;
            else
                ApplyIcon(button, icon, iconMaterial);

            button.onClick.AddListener(() => onClicked?.Invoke());
        }

        static void ApplyIcon(Button button, Sprite icon, Material material)
        {
            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null)
                image = button.GetComponentInChildren<Image>(true);
            if (image == null)
                return;

            image.sprite = icon;
            image.material = material;
            image.enabled = icon != null;
            image.preserveAspect = true;
            if (button.targetGraphic == null)
                button.targetGraphic = image;
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
                indicator.SetIcon(tileType.ResolveShardIcon());

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
