using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace M3P
{
    /// <summary>
    /// Unlocks the perk tree the player confirms.
    /// A starter panel lists every tree in the starting pool. A reward panel shows a random
    /// handful of the remaining locked trees. Names, icons, descriptions and the first
    /// thresholds come from the trees and <see cref="PerkConfig"/>.
    /// </summary>
    public sealed class UIPerkTreeUnlockPanel : UIPanelClosable
    {
        [SerializeField] UIPerkTreeOfferCard _cardTemplate;
        [SerializeField] Transform _cardParent;
        [SerializeField] UIPerkTreeCountPip _pipTemplate;
        [SerializeField] Transform _pipParent;

        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] TextMeshProUGUI _confirmLabel;
        [SerializeField] TextMeshProUGUI _hint;
        [SerializeField] TextMeshProUGUI _ownershipLabel;
        [SerializeField] TextMeshProUGUI _ownershipCount;
        [SerializeField] Button _confirmButton;

        [Tooltip("Lists every tree in the starting pool. Reward panels leave this off and draw a random handful instead.")]
        [SerializeField] bool _offerEntirePool;
        [SerializeField, Min(1)] int _offerCount = 3;
        [SerializeField, Min(1)] int _previewPerkCount = 3;

        [SerializeField] string _eyebrowText = "NOWA ŚCIEŻKA ROZWOJU";
        [SerializeField] string _titleWhenChosen = "Drzewko odblokowane";
        [SerializeField] string _titleWhenEmpty = "Wybierz drzewko";
        [SerializeField] string _subtitleWhenChosen = "Możesz teraz rozwijać drzewko: {0}.";
        [SerializeField] string _subtitleWhenEmpty = "Wybierz jedną z oferowanych ścieżek.";
        [SerializeField] string _confirmWhenChosen = "Odblokowano drzewko: {0}.";
        [SerializeField] string _confirmWhenEmpty = "Wybierz drzewko.";
        [SerializeField] string _chooseStatus = "Wybierz drzewko";
        [SerializeField] string _chosenStatus = "Drzewko odblokowane";
        [SerializeField] string _thresholdFormat = "Próg {0}";
        [SerializeField] string _hintText = "Po odblokowaniu możesz inwestować punkty i aktywować kolejne perki.";
        [SerializeField] string _ownershipCaption = "Twoje drzewka";

        readonly List<UIPerkTreeOfferCard> _cards = new List<UIPerkTreeOfferCard>();
        readonly List<UIPerkTreeCountPip> _pips = new List<UIPerkTreeCountPip>();

        UIPerkTreeOfferCard _selected;
        int _unlockedCount;
        int _maxTrees;
        float _authoredPanelWidth = -1f;
        GameObject _overlayRoot;
        Transform _hostParent;
        int _hostSibling = -1;
        ScrollRect _offerScroll;
        RectTransform _offerViewport;
        bool _offersLayoutCaptured;
        bool _offersScrolling;
        Vector2 _offersAnchorMin;
        Vector2 _offersAnchorMax;
        Vector2 _offersPivot;
        Vector2 _offersAnchoredPosition;
        Vector2 _offersSizeDelta;
        TextAnchor _offersAlignment = TextAnchor.MiddleCenter;
        int _offersPaddingLeft;
        int _offersPaddingRight;
        int _offersPaddingTop;
        int _offersPaddingBottom;

        /// <summary>True on the opening choice, which lists the whole starting pool.</summary>
        public bool OffersEntirePool => _offerEntirePool;

        /// <summary>Raised when the panel closes after a confirmed unlock.</summary>
        public event Action Closed;

        /// <summary>Raised after the chosen tree has been written into the profile.</summary>
        public event Action<PerkTree> Confirmed;

        /// <summary>True when the profile still has a locked tree the offer can show.</summary>
        public bool HasOffer
        {
            get
            {
                StatProgressionConfig progression = Progression != null ? Progression.StatProgression : null;
                PlayerProfile profile = Profiles != null ? Profiles.CurrentProfile : null;
                if (progression == null)
                    return false;

                PerkTree[] trees = OfferSource(progression);
                for (int i = 0; i < trees.Length; i++)
                {
                    PerkTree tree = trees[i];
                    if (tree == null || tree.Id == PerkTree.InvalidId)
                        continue;

                    if (profile == null || !profile.IsPerkTreeUnlocked(tree.Id))
                        return true;
                }

                return false;
            }
        }

        protected override void OnInitialize()
        {
            if (_cardTemplate != null)
                _cardTemplate.gameObject.SetActive(false);

            if (_pipTemplate != null)
                _pipTemplate.gameObject.SetActive(false);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);

            ReleaseDecorativeRaycasts();

            if (GameManager.Instance != null)
                RebuildFromProfile();
        }

        protected override void OnDestroy()
        {
            RestoreHost();
            base.OnDestroy();

            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public override void Show()
        {
            base.Show();
            MountAboveScene();

            if (GameManager.Instance != null)
                RebuildFromProfile();
        }

        public override void Hide()
        {
            bool wasOpen = IsOpen;
            RestoreHost();
            base.Hide();
            if (wasOpen)
                Closed?.Invoke();
        }

        /// <summary>
        /// Fills the columns from an explicit offer. Used by the editor preview and by <see cref="Show"/>.
        /// <paramref name="selectedIndex"/> of -1 leaves every column unchosen.
        /// </summary>
        public void Bind(
            IReadOnlyList<PerkTree> offer,
            PerkConfig catalog,
            int selectedIndex,
            int unlockedCount,
            int maxTrees)
        {
            _unlockedCount = Mathf.Max(0, unlockedCount);
            _maxTrees = Mathf.Max(0, maxTrees);
            _selected = null;

            ClearCards();

            if (_cardTemplate != null && _cardParent != null && offer != null)
            {
                _cardTemplate.gameObject.SetActive(false);

                for (int i = 0; i < offer.Count; i++)
                {
                    PerkTree tree = offer[i];
                    if (tree == null)
                        continue;

                    UIPerkTreeOfferCard card = Instantiate(_cardTemplate, _cardParent);
                    card.gameObject.SetActive(true);
                    card.name = $"Offer_{tree.DisplayName}";
                    card.Bind(tree, catalog, _previewPerkCount, _thresholdFormat, _chooseStatus, _chosenStatus);
                    card.Clicked += HandleCardClicked;
                    _cards.Add(card);
                }
            }

            if (selectedIndex >= 0 && selectedIndex < _cards.Count)
                _selected = _cards[selectedIndex];

            FitOfferWidth(_cards.Count);
            RaiseInteractive();
            ApplyChrome();
        }

        protected override void ResolveRefs()
        {
            base.ResolveRefs();

            if (_title == null)
                _title = FindDirectLabel("Title");

            if (_subtitle == null)
                _subtitle = FindDirectLabel("Subtitle");
        }

        TextMeshProUGUI FindDirectLabel(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static ProgressionService Progression => GameManager.Instance != null ? GameManager.Instance.Progression : null;

        void RebuildFromProfile()
        {
            StatProgressionConfig progression = Progression != null ? Progression.StatProgression : null;
            PlayerProfile profile = Profiles != null ? Profiles.CurrentProfile : null;
            if (progression == null)
                return;

            var locked = new List<PerkTree>();
            int unlocked = 0;
            int max = 0;
            PerkTree[] catalog = progression.PerkTrees;
            for (int i = 0; i < catalog.Length; i++)
            {
                PerkTree tree = catalog[i];
                if (tree == null || tree.Id == PerkTree.InvalidId)
                    continue;

                max++;
                if (profile != null && profile.IsPerkTreeUnlocked(tree.Id))
                    unlocked++;
            }

            PerkTree[] source = OfferSource(progression);
            for (int i = 0; i < source.Length; i++)
            {
                PerkTree tree = source[i];
                if (tree == null || tree.Id == PerkTree.InvalidId)
                    continue;

                if (profile != null && profile.IsPerkTreeUnlocked(tree.Id))
                    continue;

                if (ContainsTree(locked, tree.Id))
                    continue;

                locked.Add(tree);
            }

            IReadOnlyList<PerkTree> offer = _offerEntirePool
                ? locked
                : TakeRandom(locked, Mathf.Max(1, _offerCount));
            Bind(offer, progression.Perks, -1, unlocked, max);
        }

        PerkTree[] OfferSource(StatProgressionConfig progression)
        {
            return _offerEntirePool ? progression.StarterPerkTrees : progression.PerkTrees;
        }

        /// <summary>
        /// Picks <paramref name="count"/> trees and keeps their authored order.
        /// </summary>
        static List<PerkTree> TakeRandom(List<PerkTree> source, int count)
        {
            int take = Mathf.Min(count, source.Count);
            if (take >= source.Count)
                return source;

            var order = new int[source.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;

            for (int i = 0; i < take; i++)
            {
                int swap = Random.Range(i, order.Length);
                int held = order[i];
                order[i] = order[swap];
                order[swap] = held;
            }

            Array.Sort(order, 0, take);

            var offer = new List<PerkTree>(take);
            for (int i = 0; i < take; i++)
                offer.Add(source[order[i]]);

            return offer;
        }

        static bool ContainsTree(List<PerkTree> trees, int treeId)
        {
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null && trees[i].Id == treeId)
                    return true;
            }

            return false;
        }

        void HandleCardClicked(UIPerkTreeOfferCard card)
        {
            if (card == null || card.Tree == null)
                return;

            _selected = card;
            Sfx.Play(SfxId.CardSelect);
            ApplyChrome();
        }

        void HandleConfirmClicked()
        {
            PerkTree tree = _selected != null ? _selected.Tree : null;
            if (tree == null)
                return;

            ProgressionService progression = Progression;
            if (progression != null && !progression.TryUnlockPerkTree(tree))
                return;

            Sfx.Play(SfxId.NewSkillChoose);
            Confirmed?.Invoke(tree);
            Hide();
        }

        void ApplyChrome()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(_cards[i] == _selected);

            PerkTree chosen = _selected != null ? _selected.Tree : null;
            string chosenName = chosen != null ? chosen.DisplayName : string.Empty;
            bool hasChoice = chosen != null;

            if (_title != null)
                _title.text = hasChoice ? _titleWhenChosen : _titleWhenEmpty;

            if (_subtitle != null)
                _subtitle.text = hasChoice
                    ? string.Format(_subtitleWhenChosen, chosenName)
                    : _subtitleWhenEmpty;

            if (_confirmLabel != null)
                _confirmLabel.text = hasChoice
                    ? string.Format(_confirmWhenChosen, chosenName)
                    : _confirmWhenEmpty;

            if (_hint != null)
                _hint.text = _hintText;

            if (_ownershipLabel != null)
                _ownershipLabel.text = _ownershipCaption;

            int shown = _unlockedCount + (hasChoice ? 1 : 0);
            if (_maxTrees > 0)
                shown = Mathf.Clamp(shown, 0, _maxTrees);

            if (_ownershipCount != null)
                _ownershipCount.text = _maxTrees > 0 ? $"{shown} / {_maxTrees}" : shown.ToString();

            if (_confirmButton != null)
                _confirmButton.interactable = hasChoice;

            RebuildPips(shown);
        }

        void FitOfferWidth(int cardCount)
        {
            if (transform is not RectTransform panel || cardCount <= 0)
                return;

            if (_authoredPanelWidth < 0f)
                _authoredPanelWidth = panel.sizeDelta.x;

            float cardWidth = 318f;
            float cardHeight = 610f;
            if (_cardTemplate != null && _cardTemplate.transform is RectTransform card)
            {
                cardWidth = Mathf.Max(1f, card.sizeDelta.x);
                cardHeight = Mathf.Max(1f, card.sizeDelta.y);
            }

            float spacing = 16f;
            if (_cardParent != null)
            {
                HorizontalLayoutGroup layout = _cardParent.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                    spacing = layout.spacing;
            }

            float inset = 56f;
            if (_cardParent is RectTransform offers && !_offersScrolling)
                inset = Mathf.Max(0f, -offers.sizeDelta.x);
            else if (_offersLayoutCaptured)
                inset = Mathf.Max(0f, -_offersSizeDelta.x);

            float content = cardCount * cardWidth + Mathf.Max(0, cardCount - 1) * spacing;
            float maxWidth = _offerEntirePool ? 1840f : 1760f;
            float available = Mathf.Max(1f, maxWidth - inset);
            bool scroll = _offerEntirePool && Application.isPlaying && content > available;
            if (scroll)
            {
                panel.sizeDelta = new Vector2(Mathf.Max(_authoredPanelWidth, maxWidth), panel.sizeDelta.y);
                SetCardScale(1f);
                SetOfferScrolling(true, content, cardHeight);
                return;
            }

            SetOfferScrolling(false);
            float scale = 1f;
            if (content > available)
                scale = available / content;

            if (_cardParent != null)
            {
                HorizontalLayoutGroup layout = _cardParent.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.childScaleWidth = scale < 0.999f;
                    layout.childScaleHeight = scale < 0.999f;
                }
            }

            SetCardScale(scale);

            float needed = inset + content * scale;
            panel.sizeDelta = new Vector2(Mathf.Max(_authoredPanelWidth, Mathf.Min(maxWidth, needed)), panel.sizeDelta.y);
        }

        void SetCardScale(float scale)
        {
            var cardScale = new Vector3(scale, scale, 1f);
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                    _cards[i].transform.localScale = cardScale;
            }
        }

        /// <summary>
        /// The opening row is wider than the panel, so the cards stay at full size and scroll
        /// instead of shrinking until the text cannot be read.
        /// </summary>
        void SetOfferScrolling(bool scroll, float contentWidth = 0f, float cardHeight = 0f)
        {
            if (_cardParent is not RectTransform offers)
                return;

            if (!scroll)
            {
                if (_offersScrolling)
                    RestoreOffersLayout();

                _offersScrolling = false;
                return;
            }

            EnsureOfferViewport(offers);
            if (_offerViewport != null && !_offerViewport.gameObject.activeSelf)
                _offerViewport.gameObject.SetActive(true);

            if (!_offersScrolling && _offerViewport != null)
            {
                offers.SetParent(_offerViewport, false);
                _offersScrolling = true;
            }

            HorizontalLayoutGroup layout = offers.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childScaleWidth = false;
                layout.childScaleHeight = false;
                layout.padding.left = 8;
                layout.padding.right = 8;
            }

            offers.anchorMin = new Vector2(0f, 0.5f);
            offers.anchorMax = new Vector2(0f, 0.5f);
            offers.pivot = new Vector2(0f, 0.5f);
            offers.anchoredPosition = Vector2.zero;
            offers.localScale = Vector3.one;
            offers.sizeDelta = new Vector2(contentWidth + 16f, cardHeight);

            if (_offerScroll != null)
            {
                _offerScroll.content = offers;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(offers);
                _offerScroll.horizontalNormalizedPosition = 0f;
            }
        }

        void EnsureOfferViewport(RectTransform offers)
        {
            CaptureOffersLayout(offers);
            if (_offerViewport != null)
                return;

            var viewportObject = new GameObject("OfferViewport", typeof(RectTransform));
            viewportObject.layer = offers.gameObject.layer;
            _offerViewport = viewportObject.GetComponent<RectTransform>();
            _offerViewport.SetParent(offers.parent, false);
            _offerViewport.SetSiblingIndex(offers.GetSiblingIndex());
            _offerViewport.anchorMin = _offersAnchorMin;
            _offerViewport.anchorMax = _offersAnchorMax;
            _offerViewport.pivot = _offersPivot;
            _offerViewport.anchoredPosition = _offersAnchoredPosition;
            _offerViewport.sizeDelta = _offersSizeDelta;

            Image hit = viewportObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            viewportObject.AddComponent<RectMask2D>();

            _offerScroll = viewportObject.AddComponent<ScrollRect>();
            _offerScroll.horizontal = true;
            _offerScroll.vertical = false;
            _offerScroll.movementType = ScrollRect.MovementType.Clamped;
            _offerScroll.inertia = true;
            _offerScroll.scrollSensitivity = 40f;
            _offerScroll.viewport = _offerViewport;
            _offerScroll.content = offers;
        }

        void CaptureOffersLayout(RectTransform offers)
        {
            if (_offersLayoutCaptured)
                return;

            _offersLayoutCaptured = true;
            _offersAnchorMin = offers.anchorMin;
            _offersAnchorMax = offers.anchorMax;
            _offersPivot = offers.pivot;
            _offersAnchoredPosition = offers.anchoredPosition;
            _offersSizeDelta = offers.sizeDelta;

            HorizontalLayoutGroup layout = offers.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                return;

            _offersAlignment = layout.childAlignment;
            _offersPaddingLeft = layout.padding.left;
            _offersPaddingRight = layout.padding.right;
            _offersPaddingTop = layout.padding.top;
            _offersPaddingBottom = layout.padding.bottom;
        }

        void RestoreOffersLayout()
        {
            if (!_offersLayoutCaptured || _cardParent is not RectTransform offers)
                return;

            if (_offerViewport != null && offers.parent == _offerViewport)
            {
                offers.SetParent(_offerViewport.parent, false);
                offers.SetSiblingIndex(_offerViewport.GetSiblingIndex());
            }

            offers.anchorMin = _offersAnchorMin;
            offers.anchorMax = _offersAnchorMax;
            offers.pivot = _offersPivot;
            offers.anchoredPosition = _offersAnchoredPosition;
            offers.sizeDelta = _offersSizeDelta;
            offers.localScale = Vector3.one;

            HorizontalLayoutGroup layout = offers.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = _offersAlignment;
                layout.padding.left = _offersPaddingLeft;
                layout.padding.right = _offersPaddingRight;
                layout.padding.top = _offersPaddingTop;
                layout.padding.bottom = _offersPaddingBottom;
            }

            if (_offerViewport != null)
                _offerViewport.gameObject.SetActive(false);
        }

        void RebuildPips(int filled)
        {
            ClearPips();

            if (_pipTemplate == null || _pipParent == null || _maxTrees <= 0)
                return;

            _pipTemplate.gameObject.SetActive(false);

            for (int i = 0; i < _maxTrees; i++)
            {
                UIPerkTreeCountPip pip = Instantiate(_pipTemplate, _pipParent);
                pip.gameObject.SetActive(true);
                pip.name = $"Pip{i}";
                pip.SetFilled(i < filled);
                _pips.Add(pip);
            }
        }

        void ClearCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                UIPerkTreeOfferCard card = _cards[i];
                if (card == null)
                    continue;

                card.Clicked -= HandleCardClicked;
                DestroyOwned(card.gameObject);
            }

            _cards.Clear();

            if (_cardParent == null)
                return;

            for (int i = _cardParent.childCount - 1; i >= 0; i--)
            {
                Transform child = _cardParent.GetChild(i);
                if (_cardTemplate != null && child == _cardTemplate.transform)
                    continue;

                if (child.GetComponent<UIPerkTreeOfferCard>() != null)
                    DestroyOwned(child.gameObject);
            }
        }

        void ClearPips()
        {
            for (int i = 0; i < _pips.Count; i++)
            {
                if (_pips[i] != null)
                    DestroyOwned(_pips[i].gameObject);
            }

            _pips.Clear();

            if (_pipParent == null)
                return;

            for (int i = _pipParent.childCount - 1; i >= 0; i--)
            {
                Transform child = _pipParent.GetChild(i);
                if (_pipTemplate != null && child == _pipTemplate.transform)
                    continue;

                if (child.GetComponent<UIPerkTreeCountPip>() != null)
                    DestroyOwned(child.gameObject);
            }
        }

        void ReleaseDecorativeRaycasts()
        {
            Transform frame = transform.Find("BackgroundFrame");
            if (frame == null)
                return;

            Image image = frame.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }

        void RaiseInteractive()
        {
            Transform row = _offersScrolling && _offerViewport != null ? _offerViewport : _cardParent;
            if (row != null)
                row.SetAsLastSibling();

            if (_confirmButton != null)
                _confirmButton.transform.SetAsLastSibling();
        }

        /// <summary>
        /// The map highlight is a transparent line drawn after the camera-space canvas, so it covers
        /// this panel. An overlay canvas is composited after that pass.
        /// </summary>
        void MountAboveScene()
        {
            if (_overlayRoot != null)
                return;

            Canvas host = GetComponentInParent<Canvas>();
            if (host == null || host.renderMode == RenderMode.ScreenSpaceOverlay)
                return;

            _hostParent = transform.parent;
            _hostSibling = transform.GetSiblingIndex();

            _overlayRoot = new GameObject("PerkTreeOfferOverlay");
            _overlayRoot.layer = gameObject.layer;
            _overlayRoot.SetActive(false);

            Canvas overlay = _overlayRoot.AddComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 200;
            overlay.additionalShaderChannels = host.additionalShaderChannels;

            CanvasScaler hostScaler = host.GetComponent<CanvasScaler>();
            CanvasScaler scaler = _overlayRoot.AddComponent<CanvasScaler>();
            if (hostScaler != null)
            {
                scaler.uiScaleMode = hostScaler.uiScaleMode;
                scaler.referenceResolution = hostScaler.referenceResolution;
                scaler.screenMatchMode = hostScaler.screenMatchMode;
                scaler.matchWidthOrHeight = hostScaler.matchWidthOrHeight;
                scaler.referencePixelsPerUnit = hostScaler.referencePixelsPerUnit;
            }

            _overlayRoot.AddComponent<GraphicRaycaster>();
            _overlayRoot.SetActive(true);
            transform.SetParent(_overlayRoot.transform, false);
        }

        void RestoreHost()
        {
            if (_overlayRoot == null)
                return;

            GameObject overlay = _overlayRoot;
            _overlayRoot = null;

            if (_hostParent != null)
            {
                transform.SetParent(_hostParent, false);
                if (_hostSibling >= 0 && _hostSibling < _hostParent.childCount)
                    transform.SetSiblingIndex(_hostSibling);
            }

            _hostParent = null;
            _hostSibling = -1;

            if (overlay != null)
                DestroyOwned(overlay);
        }

        static void DestroyOwned(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
