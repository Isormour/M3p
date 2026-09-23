using System.Collections.Generic;
using Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: owned cards on the left, the current battle deck on the right.
    /// Clicking an owned card adds that copy to the deck. Deck rows step the copy count.
    /// </summary>
    public sealed class UIPanelPlayerCards : UIPanelClosable
    {
        enum CardShelf
        {
            All,
            Swaps,
            Shifts,
            Colors
        }

        const int PageSize = 10;

        [SerializeField] Transform _ownedCardsGroup;
        [SerializeField] Transform _cardsInDeckGroup;
        [SerializeField] UIDeckCardButton _cardInDeckPrefab;
        [SerializeField] UIBoardActionCard _cardPrefab;
        [SerializeField] TextMeshProUGUI _deckCountLabel;
        [SerializeField] Button _saveButton;
        [SerializeField] Button _filterAll;
        [SerializeField] Button _filterSwaps;
        [SerializeField] Button _filterShifts;
        [SerializeField] Button _filterColors;
        [SerializeField] Sprite _filterSpriteNormal;
        [SerializeField] Sprite _filterSpriteSelected;
        [SerializeField] Button _pagePrev;
        [SerializeField] Button _pageNext;
        [SerializeField] TextMeshProUGUI _pageLabel;
        [SerializeField] int _deckLimit = 20;

        readonly List<UIBoardActionCard> _ownedViews = new List<UIBoardActionCard>();
        readonly List<UIDeckCardButton> _deckViews = new List<UIDeckCardButton>();

        CardShelf _shelf = CardShelf.All;
        int _page;

        void OnEnable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= Refresh;

            ClearViews();
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static CardConfig Cards => GameManager.Instance != null ? GameManager.Instance.Config?.Cards : null;

        protected override void OnInitialize()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(Hide);
            if (_filterAll != null)
                _filterAll.onClick.AddListener(() => SetShelf(CardShelf.All));
            if (_filterSwaps != null)
                _filterSwaps.onClick.AddListener(() => SetShelf(CardShelf.Swaps));
            if (_filterShifts != null)
                _filterShifts.onClick.AddListener(() => SetShelf(CardShelf.Shifts));
            if (_filterColors != null)
                _filterColors.onClick.AddListener(() => SetShelf(CardShelf.Colors));
            if (_pagePrev != null)
                _pagePrev.onClick.AddListener(ShowPreviousPage);
            if (_pageNext != null)
                _pageNext.onClick.AddListener(ShowNextPage);
        }

        protected override void OnDestroy()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(Hide);
            if (_pagePrev != null)
                _pagePrev.onClick.RemoveListener(ShowPreviousPage);
            if (_pageNext != null)
                _pageNext.onClick.RemoveListener(ShowNextPage);

            base.OnDestroy();
        }

        protected override void ResolveRefs()
        {
            base.ResolveRefs();

            Transform ownedRoot = FindDescendant("OwnedCards");
            if (ownedRoot != null)
            {
                Transform content = ownedRoot.Find("Content");
                if (content != null)
                    _ownedCardsGroup = content;
                else if (_ownedCardsGroup == null)
                    _ownedCardsGroup = ownedRoot;
            }

            Transform deckRoot = FindDescendant("CardsInDeck");
            if (deckRoot != null)
            {
                Transform content = deckRoot.Find("Content");
                if (content != null)
                    _cardsInDeckGroup = content;
                else if (_cardsInDeckGroup == null)
                    _cardsInDeckGroup = deckRoot;
            }

            if (_deckCountLabel == null)
                _deckCountLabel = FindDescendantComponent<TextMeshProUGUI>("DeckCount");
            if (_pageLabel == null)
                _pageLabel = FindDescendantComponent<TextMeshProUGUI>("PageLabel");
            if (_saveButton == null)
                _saveButton = FindDescendantButton("SaveDeckButton");
            if (_filterAll == null)
                _filterAll = FindDescendantButton("FilterAll");
            if (_filterSwaps == null)
                _filterSwaps = FindDescendantButton("FilterSwaps");
            if (_filterShifts == null)
                _filterShifts = FindDescendantButton("FilterShifts");
            if (_filterColors == null)
                _filterColors = FindDescendantButton("FilterColors");
            if (_pagePrev == null)
                _pagePrev = FindDescendantButton("PagePrev");
            if (_pageNext == null)
                _pageNext = FindDescendantButton("PageNext");
        }

        void SetShelf(CardShelf shelf)
        {
            _shelf = shelf;
            _page = 0;
            Refresh();
        }

        void ShowPreviousPage()
        {
            if (_page <= 0)
                return;

            _page--;
            Refresh();
        }

        void ShowNextPage()
        {
            _page++;
            Refresh();
        }

        void Refresh()
        {
            ClearViews();
            ApplyFilterVisuals();

            PlayerProfile profile = Profiles?.CurrentProfile;
            CardConfig cardConfig = Cards;
            int deckCount = profile != null ? profile.GetDeckIndices().Count : 0;
            if (_deckCountLabel != null)
                _deckCountLabel.text = $"<color=#E8B44A>{deckCount}</color>  /  {_deckLimit}";

            if (profile?.Cards == null || cardConfig == null)
            {
                SetPageLabel(1, 1);
                return;
            }

            BuildOwnedCards(profile, cardConfig);
            BuildDeckCards(profile, cardConfig, deckCount);
        }

        void BuildOwnedCards(PlayerProfile profile, CardConfig cardConfig)
        {
            if (_ownedCardsGroup == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerCards)}: assign {nameof(_ownedCardsGroup)} on the prefab.", this);
                return;
            }

            if (_cardPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerCards)}: assign {nameof(_cardPrefab)} on the prefab.", this);
                return;
            }

            var visible = new List<int>();
            for (int i = 0; i < profile.Cards.Count; i++)
            {
                if (!cardConfig.TryGetCard(profile.Cards[i].CardId, out BoardActionCardDefinition card))
                    continue;
                if (!MatchesShelf(card, _shelf))
                    continue;

                visible.Add(i);
            }

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(visible.Count / (float)PageSize));
            _page = Mathf.Clamp(_page, 0, pageCount - 1);
            SetPageLabel(_page + 1, pageCount);

            int start = _page * PageSize;
            int end = Mathf.Min(start + PageSize, visible.Count);
            for (int i = start; i < end; i++)
            {
                int ownedIndex = visible[i];
                if (!cardConfig.TryGetCard(profile.Cards[ownedIndex].CardId, out BoardActionCardDefinition card))
                    continue;

                bool inDeck = profile.IsOwnedCardInDeck(ownedIndex);
                UIBoardActionCard view = Instantiate(_cardPrefab, _ownedCardsGroup);
                view.name = $"Owned_{card.name}_{ownedIndex + 1}";
                view.Configure(card, () => HandleOwnedCardClicked(ownedIndex));
                view.SetInteractable(!inDeck);
                view.SetFrameMaskEnabled(inDeck);
                view.SetSelectedHighlight(false);
                _ownedViews.Add(view);
            }
        }

        void BuildDeckCards(PlayerProfile profile, CardConfig cardConfig, int deckCount)
        {
            if (_cardsInDeckGroup == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerCards)}: assign {nameof(_cardsInDeckGroup)} on the prefab.", this);
                return;
            }

            if (_cardInDeckPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerCards)}: assign {nameof(_cardInDeckPrefab)} on the prefab.", this);
                return;
            }

            IReadOnlyList<int> deck = profile.GetDeckIndices();
            var groups = new List<DeckGroup>();
            for (int i = 0; i < deck.Count; i++)
            {
                int ownedIndex = deck[i];
                if (ownedIndex < 0 || ownedIndex >= profile.Cards.Count)
                    continue;

                if (!cardConfig.TryGetCard(profile.Cards[ownedIndex].CardId, out BoardActionCardDefinition card))
                    continue;

                DeckGroup group = null;
                for (int g = 0; g < groups.Count; g++)
                {
                    if (groups[g].Card == card)
                    {
                        group = groups[g];
                        break;
                    }
                }

                if (group == null)
                {
                    group = new DeckGroup { Card = card };
                    groups.Add(group);
                }

                group.DeckPositions.Add(i);
            }

            bool deckFull = deckCount >= _deckLimit;
            for (int i = 0; i < groups.Count; i++)
            {
                DeckGroup group = groups[i];
                int removeAt = group.DeckPositions[group.DeckPositions.Count - 1];
                bool canAdd = !deckFull && FindSpareCopy(profile, cardConfig, group.Card) >= 0;
                UIDeckCardButton view = Instantiate(_cardInDeckPrefab, _cardsInDeckGroup);
                view.name = $"Deck_{group.Card.name}";
                view.Configure(
                    group.Card,
                    group.DeckPositions.Count,
                    canAdd,
                    () => HandleDeckCardClicked(removeAt),
                    () => HandleAddCopy(group.Card));
                _deckViews.Add(view);
            }
        }

        void HandleOwnedCardClicked(int ownedIndex)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || profile.GetDeckIndices().Count >= _deckLimit)
                return;

            if (!profile.TryAddOwnedCardToDeck(ownedIndex))
                return;

            Profiles.Save();
        }

        void HandleAddCopy(BoardActionCardDefinition card)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            CardConfig cardConfig = Cards;
            if (profile == null || cardConfig == null || profile.GetDeckIndices().Count >= _deckLimit)
                return;

            int ownedIndex = FindSpareCopy(profile, cardConfig, card);
            if (ownedIndex < 0 || !profile.TryAddOwnedCardToDeck(ownedIndex))
                return;

            Profiles.Save();
        }

        void HandleDeckCardClicked(int deckIndex)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || !profile.TryRemoveDeckCardAt(deckIndex))
                return;

            Profiles.Save();
        }

        static int FindSpareCopy(PlayerProfile profile, CardConfig cardConfig, BoardActionCardDefinition card)
        {
            if (profile.Cards == null || card == null)
                return -1;

            for (int i = 0; i < profile.Cards.Count; i++)
            {
                if (profile.IsOwnedCardInDeck(i))
                    continue;

                if (cardConfig.TryGetCard(profile.Cards[i].CardId, out BoardActionCardDefinition owned)
                    && owned == card)
                    return i;
            }

            return -1;
        }

        void SetPageLabel(int page, int pageCount)
        {
            if (_pageLabel != null)
                _pageLabel.text = $"{page} / {pageCount}";
            if (_pagePrev != null)
                _pagePrev.interactable = page > 1;
            if (_pageNext != null)
                _pageNext.interactable = page < pageCount;
        }

        void ApplyFilterVisuals()
        {
            StyleFilter(_filterAll, _shelf == CardShelf.All);
            StyleFilter(_filterSwaps, _shelf == CardShelf.Swaps);
            StyleFilter(_filterShifts, _shelf == CardShelf.Shifts);
            StyleFilter(_filterColors, _shelf == CardShelf.Colors);
        }

        void StyleFilter(Button button, bool selected)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image != null)
            {
                Sprite sprite = selected ? _filterSpriteSelected : _filterSpriteNormal;
                if (sprite != null)
                    image.sprite = sprite;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.color = selected ? Color.white : new Color(0.78f, 0.82f, 0.88f, 1f);
        }

        static bool MatchesShelf(BoardActionCardDefinition card, CardShelf shelf)
        {
            if (shelf == CardShelf.All || card == null)
                return shelf == CardShelf.All;

            BoardActionLogic logic = card.Logic;
            switch (shelf)
            {
                case CardShelf.Swaps:
                    return logic is SwapTilesLogic || logic is Cycle2x2Logic || logic is TriangleRotationLogic;
                case CardShelf.Shifts:
                    return logic is ShiftRowLogic || logic is GravityShiftLogic;
                case CardShelf.Colors:
                    return logic is PaintTileLogic;
                default:
                    return true;
            }
        }

        void ClearViews()
        {
            for (int i = 0; i < _ownedViews.Count; i++)
            {
                if (_ownedViews[i] != null)
                    Destroy(_ownedViews[i].gameObject);
            }

            _ownedViews.Clear();

            for (int i = 0; i < _deckViews.Count; i++)
            {
                if (_deckViews[i] != null)
                    Destroy(_deckViews[i].gameObject);
            }

            _deckViews.Clear();
        }

        sealed class DeckGroup
        {
            public BoardActionCardDefinition Card;
            public readonly List<int> DeckPositions = new List<int>();
        }
    }
}
