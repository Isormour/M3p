using Match3;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: pick a card from the catalogue, preview its craft cost, and buy a copy with shards.
    /// </summary>
    public sealed class UIPanelCardCrafting : UIPanelClosable
    {
        [SerializeField] Transform CardTypesParent;
        [SerializeField] Button CardTypePrefab;
        [SerializeField] UIBoardActionCard CardToBeCrafted;
        [SerializeField] Transform CostParent;
        [SerializeField] UIPanelPlayerManaBar CostPrefab;
        [SerializeField] Button ConfirmCraftButton;
        [SerializeField] TextMeshProUGUI _selectedTitle;
        [SerializeField] TextMeshProUGUI _selectedDescription;
        [SerializeField] Sprite _recipeNormalSprite;
        [SerializeField] Sprite _recipeSelectedSprite;

        readonly List<Button> _typeViews = new List<Button>();
        readonly List<BoardActionCardDefinition> _typeCards = new List<BoardActionCardDefinition>();
        readonly List<UIPanelPlayerManaBar> _costViews = new List<UIPanelPlayerManaBar>();

        BoardActionCardDefinition _selectedCard;

        protected override void OnInitialize()
        {
            HideTemplate(CardTypePrefab);
            HideTemplate(CostPrefab);

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

        protected override void OnDestroy()
        {
            base.OnDestroy();
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

        static CardConfig Cards => Config != null ? Config.Cards : null;

        protected override void ResolveRefs()
        {
            base.ResolveRefs();
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

            if (_selectedTitle == null)
                _selectedTitle = FindDescendantComponent<TextMeshProUGUI>("SelectedTitle");

            if (_selectedDescription == null)
                _selectedDescription = FindDescendantComponent<TextMeshProUGUI>("SelectedDescription");

            if (_recipeNormalSprite == null)
                _recipeNormalSprite = SpriteOn("RecipeSpriteNormal");

            if (_recipeSelectedSprite == null)
                _recipeSelectedSprite = SpriteOn("RecipeSpriteSelected");
        }

        Sprite SpriteOn(string childName)
        {
            Transform child = FindDescendant(childName);
            if (child == null)
                return null;

            Image image = child.GetComponent<Image>();
            return image != null ? image.sprite : null;
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
            BoardActionCardDefinition first = null;
            for (int i = 0; i < entries.Length; i++)
            {
                BoardActionCardDefinition card = entries[i].Card;
                if (card == null || entries[i].Id == CardConfig.InvalidCardId)
                    continue;

                if (first == null)
                    first = card;

                Button view = Instantiate(CardTypePrefab, CardTypesParent);
                view.gameObject.SetActive(true);
                view.name = $"CraftType_{card.name}";

                BoardActionCardDefinition captured = card;
                ConfigureTypeButton(view, captured, () => HandleTypeClicked(captured));
                _typeViews.Add(view);
                _typeCards.Add(captured);
            }

            if (_selectedCard == null)
                _selectedCard = first;
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

            if (_selectedTitle != null)
                _selectedTitle.text = _selectedCard != null ? _selectedCard.DisplayName : string.Empty;

            if (_selectedDescription != null)
                _selectedDescription.text = _selectedCard != null ? _selectedCard.Description : string.Empty;
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
                view.Configure(typeId, icon, runeGraphics.SpriteMaterial);
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

                bool selected = i < _typeCards.Count && _typeCards[i] == _selectedCard;
                UIBoardActionCard cardView = view.GetComponent<UIBoardActionCard>();
                if (cardView != null)
                    cardView.SetSelected(cardView.Card == _selectedCard);

                Image image = view.GetComponent<Image>();
                if (image != null && _recipeNormalSprite != null)
                    image.sprite = selected && _recipeSelectedSprite != null ? _recipeSelectedSprite : _recipeNormalSprite;
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

            Transform iconTransform = button.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = card != null ? card.Artwork : null;
                    icon.enabled = icon.sprite != null;
                    icon.preserveAspect = true;
                }
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
            _typeCards.Clear();
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
    }
}
