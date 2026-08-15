using System;
using System.Collections.Generic;
using Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: pick a card from the catalogue, preview its craft cost, and buy a copy with shards.
    /// </summary>
    public sealed class UIPanelCardCrafting : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Button _closeButton;

        [SerializeField] Transform CardTypesParent;
        [SerializeField] Button CardTypePrefab;
        [SerializeField] UIBoardActionCard CardToBeCrafted;
        [SerializeField] Transform CostParent;
        [SerializeField] UIPanelPlayerManaBar CostPrefab;
        [SerializeField] Button ConfirmCraftButton;

        readonly List<Button> _typeViews = new List<Button>();
        readonly List<UIPanelPlayerManaBar> _costViews = new List<UIPanelPlayerManaBar>();

        BoardActionCardDefinition _selectedCard;
        bool _initialized;

        void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Also runs from <see cref="Show"/>, because a panel that starts inactive never reaches
        /// <see cref="Awake"/> until something opens it.
        /// </summary>
        void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ResolveRefs();
            HideTemplate(CardTypePrefab);
            HideTemplate(CostPrefab);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);

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
            ClearCostViews();
        }

        void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (ConfirmCraftButton != null)
                ConfirmCraftButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        public void Show()
        {
            Initialize();
            Root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            Root.SetActive(false);
        }

        public void Toggle()
        {
            if (Root.activeSelf)
                Hide();
            else
                Show();
        }

        GameObject Root => _panelRoot != null ? _panelRoot : gameObject;

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static GameConfig Config => GameManager.Instance != null ? GameManager.Instance.Config : null;

        static CardConfig Cards => Config != null ? Config.Cards : null;

        void ResolveRefs()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            if (_closeButton == null)
                _closeButton = FindDescendantButton("CloseButton");

            if (CardTypesParent == null)
                CardTypesParent = FindDescendant("CardTypesParent");

            if (CostParent == null)
                CostParent = FindDescendant("CostParent");

            if (CardToBeCrafted == null)
            {
                Transform crafted = FindDescendant("CardToBeCrafted");
                if (crafted != null)
                    CardToBeCrafted = crafted.GetComponent<UIBoardActionCard>();
            }

            if (ConfirmCraftButton == null)
                ConfirmCraftButton = FindDescendantButton("ConfirmCraftButton");
        }

        void HandleCloseClicked()
        {
            Hide();
        }

        void HandleProfileChanged()
        {
            RefreshSelection();
        }

        void Refresh()
        {
            BuildCardTypes();
            RefreshSelection();
        }

        void BuildCardTypes()
        {
            ClearTypeViews();

            if (CardTypesParent == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(CardTypesParent)} on the prefab.", this);
                return;
            }

            if (CardTypePrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(CardTypePrefab)} on the prefab.", this);
                return;
            }

            CardConfig cardConfig = Cards;
            if (cardConfig == null)
                return;

            CardConfig.Entry[] entries = cardConfig.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                BoardActionCardDefinition card = entries[i].Card;
                if (card == null || entries[i].Id == CardConfig.InvalidCardId)
                    continue;

                Button view = Instantiate(CardTypePrefab, CardTypesParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftType_{card.name}";

                BoardActionCardDefinition captured = card;
                ConfigureTypeButton(view, captured, () => HandleTypeClicked(captured));
                _typeViews.Add(view);
            }
        }

        void HandleTypeClicked(BoardActionCardDefinition card)
        {
            _selectedCard = card;
            RefreshSelection();
        }

        void RefreshSelection()
        {
            ApplySelectedCardPreview();
            BuildCosts();
            RefreshConfirmButton();
            RefreshTypeSelection();
        }

        void ApplySelectedCardPreview()
        {
            if (CardToBeCrafted == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(CardToBeCrafted)} on the prefab.", this);
                return;
            }

            CardToBeCrafted.gameObject.SetActive(_selectedCard != null);
            if (_selectedCard == null)
                return;

            CardToBeCrafted.Configure(_selectedCard, null);
            CardToBeCrafted.SetInteractable(false);
            CardToBeCrafted.SetSelected(false);
        }

        void BuildCosts()
        {
            ClearCostViews();

            if (_selectedCard == null)
                return;

            if (CostParent == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(CostParent)} on the prefab.", this);
                return;
            }

            if (CostPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(CostPrefab)} on the prefab.", this);
                return;
            }

            GameConfig config = Config;
            TileTypeShardCost[] costs = _selectedCard.CraftCost;
            for (int i = 0; i < costs.Length; i++)
            {
                TileTypeShardCost cost = costs[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                int typeId = config != null ? config.GetTileTypeId(cost.TileType) : -1;
                Sprite icon = typeId >= 0 && config != null ? config.GetTileTypeSprite(typeId) : cost.TileType.Sprite;
                TileTypeGraphics runeGraphics = typeId >= 0 && config != null
                    ? config.GetTileTypeRuneGraphics(typeId)
                    : cost.TileType.RuneGraphics;

                UIPanelPlayerManaBar view = Instantiate(CostPrefab, CostParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftCost_{cost.TileType.name}";
                view.Configure(typeId, icon, runeGraphics?.SpriteMaterial);
                view.SetAmount(cost.Amount);
                _costViews.Add(view);
            }
        }

        void RefreshConfirmButton()
        {
            if (ConfirmCraftButton == null)
            {
                Debug.LogError($"{nameof(UIPanelCardCrafting)}: assign {nameof(ConfirmCraftButton)} on the prefab.", this);
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

                UIBoardActionCard cardView = view.GetComponent<UIBoardActionCard>();
                if (cardView != null)
                    cardView.SetSelected(cardView.Card == _selectedCard);
            }
        }

        bool CanCraftSelected()
        {
            if (_selectedCard == null)
                return false;

            PlayerProfile profile = Profiles?.CurrentProfile;
            return profile != null && profile.CanAffordCraftCost(_selectedCard.CraftCost);
        }

        void HandleConfirmClicked()
        {
            if (!CanCraftSelected())
                return;

            CardConfig cardConfig = Cards;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (cardConfig == null || profile == null)
                return;

            int cardId = cardConfig.GetCardId(_selectedCard);
            if (!profile.TryCraftCard(cardId, _selectedCard.CraftCost))
                return;

            Profiles.Save();
        }

        static void ConfigureTypeButton(Button button, BoardActionCardDefinition card, Action onClicked)
        {
            UIBoardActionCard cardView = button.GetComponent<UIBoardActionCard>();
            if (cardView != null)
            {
                cardView.Configure(card, onClicked);
                return;
            }

            UICardVisuals visuals = button.GetComponentInChildren<UICardVisuals>(true);
            if (visuals != null)
                visuals.SetCardData(card);
            else
            {
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = card != null ? card.DisplayName : string.Empty;
            }

            button.onClick.AddListener(() => onClicked?.Invoke());
        }

        void ClearTypeViews()
        {
            for (int i = 0; i < _typeViews.Count; i++)
            {
                if (_typeViews[i] != null)
                    Destroy(_typeViews[i].gameObject);
            }

            _typeViews.Clear();
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

        static void HideTemplate(Component template)
        {
            if (template != null && template.gameObject.scene.IsValid())
                template.gameObject.SetActive(false);
        }

        Transform FindDescendant(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i];
            }

            return null;
        }

        Button FindDescendantButton(string childName)
        {
            Transform child = FindDescendant(childName);
            if (child == null)
                return null;

            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;

            return child.GetComponentInChildren<Button>(true);
        }
    }
}
