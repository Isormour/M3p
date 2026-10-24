using System.Collections;
using System.Collections.Generic;
using Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: owned cards on the left, the current battle deck on the right.
    /// Clicking an owned card adds that copy to the deck. A visual copy jumps into the deck column
    /// and jumps back to the shelf when that copy is removed.
    /// Deck rows step the copy count.
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
        const float CardFlyDuration = 0.42f;
        const float CardFlyArcFactor = 0.5f;
        const float CardFlyMinArc = 150f;
        const float CardLandScale = 0.22f;
        const float CardLandInset = 8f;

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
        readonly List<int> _ownedViewIndices = new List<int>();
        readonly List<UIDeckCardButton> _deckViews = new List<UIDeckCardButton>();
        readonly List<GameObject> _flyGhosts = new List<GameObject>();

        RectTransform _flyLayer;

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

            ClearFlyGhosts();
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
            if (_shelf == shelf)
                return;

            Sfx.Play(SfxId.CardFilterChange);
            _shelf = shelf;
            _page = 0;
            Refresh();
        }

        void ShowPreviousPage()
        {
            if (_page <= 0)
                return;

            _page--;
            Sfx.Play(SfxId.CardPageChange);
            Refresh();
        }

        void ShowNextPage()
        {
            int before = _page;
            _page++;
            Refresh();
            if (_page != before)
                Sfx.Play(SfxId.CardPageChange);
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
                bool deckFull = profile.GetDeckIndices().Count >= _deckLimit;
                UIBoardActionCard view = Instantiate(_cardPrefab, _ownedCardsGroup);
                view.name = $"Owned_{card.name}_{ownedIndex + 1}";
                view.Configure(card, () => HandleOwnedCardClicked(ownedIndex, view));
                view.SetInteractable(inDeck || !deckFull);
                view.SetFrameMaskEnabled(inDeck);
                view.SetSelectedHighlight(false);
                _ownedViews.Add(view);
                _ownedViewIndices.Add(ownedIndex);
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

        void HandleOwnedCardClicked(int ownedIndex, UIBoardActionCard source)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null)
                return;

            RectTransform ghost = null;
            BoardActionCardDefinition flownCard = null;
            if (profile.IsOwnedCardInDeck(ownedIndex))
            {
                if (!TryRemoveOwnedCard(profile, ownedIndex))
                    return;

                Sfx.Play(SfxId.DeckCardRemove);
                PlayReturn(source != null ? source.Card : null, source, ownedIndex);
            }
            else
            {
                if (profile.GetDeckIndices().Count >= _deckLimit)
                    return;

                ghost = CreateFlyGhost(source);
                if (!profile.TryAddOwnedCardToDeck(ownedIndex))
                {
                    ReleaseGhost(ghost);
                    return;
                }

                flownCard = source != null ? source.Card : null;
                Sfx.Play(SfxId.DeckCardAdd);
            }

            Profiles.Save();
            if (ghost != null)
                StartCoroutine(FlyCardToDeck(ghost, flownCard));
        }

        static bool TryRemoveOwnedCard(PlayerProfile profile, int ownedIndex)
        {
            IReadOnlyList<int> deck = profile.GetDeckIndices();
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i] != ownedIndex)
                    continue;

                return profile.TryRemoveDeckCardAt(i);
            }

            return false;
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

            Sfx.Play(SfxId.DeckCardAdd);

            Profiles.Save();
        }

        void HandleDeckCardClicked(int deckIndex)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null)
                return;

            BoardActionCardDefinition card = null;
            int ownedIndex = -1;
            IReadOnlyList<int> deck = profile.GetDeckIndices();
            if (deckIndex >= 0 && deckIndex < deck.Count)
            {
                ownedIndex = deck[deckIndex];
                CardConfig cardConfig = Cards;
                if (cardConfig != null
                    && ownedIndex >= 0
                    && profile.Cards != null
                    && ownedIndex < profile.Cards.Count)
                    cardConfig.TryGetCard(profile.Cards[ownedIndex].CardId, out card);
            }

            if (!profile.TryRemoveDeckCardAt(deckIndex))
                return;

            Sfx.Play(SfxId.DeckCardRemove);
            PlayReturn(card, FindOwnedView(ownedIndex), ownedIndex);
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
            _ownedViewIndices.Clear();

            for (int i = 0; i < _deckViews.Count; i++)
            {
                if (_deckViews[i] != null)
                    Destroy(_deckViews[i].gameObject);
            }

            _deckViews.Clear();
        }

        RectTransform CreateFlyGhost(UIBoardActionCard source)
        {
            if (source == null || source.Card == null || !(source.transform is RectTransform sourceRect))
                return null;

            return CreateFlyGhost(source.Card, WorldCenter(sourceRect), SizeOnLayer(WorldSize(sourceRect)));
        }

        RectTransform CreateFlyGhost(BoardActionCardDefinition card, Vector3 position, Vector2 size)
        {
            if (card == null || _cardPrefab == null)
                return null;

            RectTransform layer = FlyLayer();
            UIBoardActionCard ghostCard = Instantiate(_cardPrefab, layer);
            ghostCard.name = "FlyCard";
            ghostCard.Configure(card, null);
            ghostCard.SetFrameMaskEnabled(false);
            ghostCard.SetSelectedHighlight(true);

            Button button = ghostCard.GetComponent<Button>();
            if (button != null)
                button.enabled = false;

            Graphic[] graphics = ghostCard.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;

            UITooltipTrigger tooltip = ghostCard.GetComponent<UITooltipTrigger>();
            if (tooltip != null)
                tooltip.enabled = false;

            RectTransform ghost = ghostCard.transform as RectTransform;
            ghost.anchorMin = new Vector2(0.5f, 0.5f);
            ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.pivot = new Vector2(0.5f, 0.5f);
            ghost.localRotation = Quaternion.identity;
            ghost.localScale = Vector3.one;
            ghost.sizeDelta = size;
            ghost.position = position;

            CanvasGroup group = ghost.GetComponent<CanvasGroup>();
            if (group == null)
                group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            _flyGhosts.Add(ghost.gameObject);
            return ghost;
        }

        RectTransform FlyLayer()
        {
            if (_flyLayer == null)
            {
                var layerObject = new GameObject("CardFlyLayer", typeof(RectTransform));
                layerObject.layer = gameObject.layer;
                _flyLayer = layerObject.GetComponent<RectTransform>();
                _flyLayer.SetParent(transform, false);
                _flyLayer.anchorMin = new Vector2(0.5f, 0.5f);
                _flyLayer.anchorMax = new Vector2(0.5f, 0.5f);
                _flyLayer.pivot = new Vector2(0.5f, 0.5f);
                _flyLayer.sizeDelta = Vector2.zero;
                _flyLayer.anchoredPosition = Vector2.zero;
            }

            _flyLayer.SetAsLastSibling();
            return _flyLayer;
        }

        IEnumerator FlyCardToDeck(RectTransform ghost, BoardActionCardDefinition card)
        {
            if (ghost == null)
                yield break;

            Canvas.ForceUpdateCanvases();
            if (_cardsInDeckGroup is RectTransform deckContent)
                LayoutRebuilder.ForceRebuildLayoutImmediate(deckContent);

            Vector3 start = ghost.position;
            Vector2 startSize = ghost.sizeDelta;
            Vector3 end = LandingPoint(FindDeckRow(card));
            Vector2 endSize = startSize * CardLandScale;
            Vector3 control = JumpControl(start, end);
            CanvasGroup group = ghost.GetComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < CardFlyDuration)
            {
                if (ghost == null)
                    yield break;

                PoseFlyCard(ghost, group, start, control, end, startSize, endSize, elapsed / CardFlyDuration, false);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ghost != null)
                PoseFlyCard(ghost, group, start, control, end, startSize, endSize, 1f, false);

            ReleaseGhost(ghost);

            UIDeckCardButton row = FindDeckRow(card);
            if (row == null)
                yield break;

            RectTransform icon = row.IconRect;
            bool punchIcon = icon != null && icon != row.transform;
            Transform punched = punchIcon ? icon : row.transform;
            yield return PunchLanding(punched, punchIcon ? 0.28f : 0.08f, punchIcon);
        }

        void PlayReturn(BoardActionCardDefinition card, UIBoardActionCard shelf, int ownedIndex)
        {
            if (card == null)
                return;

            Vector3 to;
            Vector2 toSize;
            if (shelf != null && shelf.transform is RectTransform shelfRect)
            {
                to = WorldCenter(shelfRect);
                toSize = SizeOnLayer(WorldSize(shelfRect));
                RectTransform shelfViewport = ShelfViewport();
                if (shelfViewport != null)
                    to = ClampInside(shelfViewport, to, CardLandInset);
            }
            else
            {
                RectTransform shelfViewport = ShelfViewport();
                if (shelfViewport == null)
                    return;

                to = WorldCenter(shelfViewport);
                toSize = ShelfCellSize();
            }

            Vector3 from = LandingPoint(FindDeckRow(card));
            Vector2 fromSize = toSize * CardLandScale;
            RectTransform ghost = CreateFlyGhost(card, from, fromSize);
            if (ghost == null)
                return;

            StartCoroutine(FlyCardBack(ghost, from, fromSize, to, toSize, ownedIndex));
        }

        IEnumerator FlyCardBack(
            RectTransform ghost,
            Vector3 from,
            Vector2 fromSize,
            Vector3 to,
            Vector2 toSize,
            int ownedIndex)
        {
            if (ghost == null)
                yield break;

            Vector3 control = JumpControl(from, to);
            CanvasGroup group = ghost.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            while (elapsed < CardFlyDuration)
            {
                if (ghost == null)
                    yield break;

                PoseFlyCard(ghost, group, from, control, to, fromSize, toSize, elapsed / CardFlyDuration, true);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ghost != null)
                PoseFlyCard(ghost, group, from, control, to, fromSize, toSize, 1f, true);

            ReleaseGhost(ghost);

            UIBoardActionCard shelf = FindOwnedView(ownedIndex);
            if (shelf == null)
                yield break;

            yield return PunchLanding(shelf.transform, 0.06f, true);
        }

        IEnumerator PunchLanding(Transform target, float strength, bool uniform)
        {
            const float punchDuration = 0.16f;
            float punch = 0f;
            while (punch < punchDuration)
            {
                if (target == null)
                    yield break;

                punch += Time.unscaledDeltaTime;
                float amount = Mathf.Sin(Mathf.Clamp01(punch / punchDuration) * Mathf.PI) * strength;
                target.localScale = uniform
                    ? Vector3.one * (1f + amount)
                    : new Vector3(1f, 1f + amount, 1f);
                yield return null;
            }

            if (target != null)
                target.localScale = Vector3.one;
        }

        static void PoseFlyCard(
            RectTransform ghost,
            CanvasGroup group,
            Vector3 start,
            Vector3 control,
            Vector3 end,
            Vector2 startSize,
            Vector2 endSize,
            float t,
            bool returning)
        {
            t = Mathf.Clamp01(t);
            float move = Mathf.SmoothStep(0f, 1f, t);
            ghost.position = Bezier(start, control, end, move);

            float reshape = returning
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.62f, t))
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, t));
            ghost.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, reshape);

            float hop = 1f + Mathf.Sin(t * Mathf.PI) * 0.07f;
            ghost.localScale = new Vector3(hop, hop, 1f);
            float tilt = Mathf.Sin(t * Mathf.PI) * (returning ? 12f : -12f);
            ghost.localRotation = Quaternion.Euler(0f, 0f, tilt);
            if (group != null)
            {
                float fade = returning
                    ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, t))
                    : reshape;
                group.alpha = 1f - fade;
            }
        }

        Vector3 LandingPoint(UIDeckCardButton row)
        {
            RectTransform viewport = DeckViewport();
            if (row == null)
                return viewport != null ? WorldCenter(viewport) : transform.position;

            Vector3 point = WorldCenter(row.IconRect);
            if (viewport != null)
                point = ClampInside(viewport, point, CardLandInset);
            return point;
        }

        RectTransform DeckViewport()
        {
            if (_cardsInDeckGroup == null)
                return null;

            return _cardsInDeckGroup.parent as RectTransform;
        }

        RectTransform ShelfViewport()
        {
            if (_ownedCardsGroup == null)
                return null;

            return _ownedCardsGroup.parent as RectTransform;
        }

        Vector2 ShelfCellSize()
        {
            if (_ownedCardsGroup != null)
            {
                GridLayoutGroup grid = _ownedCardsGroup.GetComponent<GridLayoutGroup>();
                if (grid != null)
                    return grid.cellSize;
            }

            return new Vector2(164f, 246f);
        }

        Vector2 SizeOnLayer(Vector2 worldSize)
        {
            Vector3 layerScale = FlyLayer().lossyScale;
            return new Vector2(
                worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(layerScale.x)),
                worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(layerScale.y)));
        }

        UIBoardActionCard FindOwnedView(int ownedIndex)
        {
            int count = Mathf.Min(_ownedViews.Count, _ownedViewIndices.Count);
            for (int i = 0; i < count; i++)
            {
                if (_ownedViewIndices[i] == ownedIndex)
                    return _ownedViews[i];
            }

            return null;
        }

        Vector3 JumpControl(Vector3 start, Vector3 end)
        {
            Vector3 up = _flyLayer != null ? _flyLayer.up : Vector3.up;
            float scale = _flyLayer != null ? Mathf.Abs(_flyLayer.lossyScale.y) : 1f;
            float arc = Mathf.Max(Vector3.Distance(start, end) * CardFlyArcFactor, CardFlyMinArc * scale);
            return (start + end) * 0.5f + up * arc;
        }

        UIDeckCardButton FindDeckRow(BoardActionCardDefinition card)
        {
            if (card == null)
                return null;

            for (int i = 0; i < _deckViews.Count; i++)
            {
                UIDeckCardButton view = _deckViews[i];
                if (view != null && view.Card == card)
                    return view;
            }

            return null;
        }

        void ReleaseGhost(RectTransform ghost)
        {
            if (ghost == null)
                return;

            _flyGhosts.Remove(ghost.gameObject);
            Destroy(ghost.gameObject);
        }

        void ClearFlyGhosts()
        {
            for (int i = 0; i < _flyGhosts.Count; i++)
            {
                if (_flyGhosts[i] != null)
                    Destroy(_flyGhosts[i]);
            }

            _flyGhosts.Clear();
        }

        static Vector3 WorldCenter(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        static Vector2 WorldSize(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Vector2(
                Vector3.Distance(corners[0], corners[3]),
                Vector3.Distance(corners[0], corners[1]));
        }

        static Vector3 ClampInside(RectTransform rect, Vector3 world, float inset)
        {
            Vector3 local = rect.InverseTransformPoint(world);
            Rect area = rect.rect;
            local.x = Mathf.Clamp(local.x, area.xMin + inset, area.xMax - inset);
            local.y = Mathf.Clamp(local.y, area.yMin + inset, area.yMax - inset);
            return rect.TransformPoint(local);
        }

        static Vector3 Bezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float remain = 1f - t;
            return remain * remain * start + 2f * remain * t * control + t * t * end;
        }

        sealed class DeckGroup
        {
            public BoardActionCardDefinition Card;
            public readonly List<int> DeckPositions = new List<int>();
        }
    }
}
