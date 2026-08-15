using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: owned cards on the left, the current battle deck on the right.
    /// Clicking an owned card adds that copy to the deck; clicking a deck card removes it.
    /// </summary>
    public sealed class UIPanelPlayerCards : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Button _closeButton;
        [SerializeField] Transform _ownedCardsGroup;
        [SerializeField] Transform _cardsInDeckGroup;
        [SerializeField] Button _cardInDeckPrefab;
        [SerializeField] UIBoardActionCard _cardPrefab;

        readonly List<UIBoardActionCard> _ownedViews = new List<UIBoardActionCard>();
        readonly List<Button> _deckViews = new List<Button>();

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

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);
        }

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

        void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
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

        static CardConfig Cards => GameManager.Instance != null ? GameManager.Instance.Config?.Cards : null;

        void ResolveRefs()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            if (_closeButton == null)
                _closeButton = FindDescendantButton("CloseButton");

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
        }

        void HandleCloseClicked()
        {
            Hide();
        }

        void Refresh()
        {
            ClearViews();

            PlayerProfile profile = Profiles?.CurrentProfile;
            CardConfig cardConfig = Cards;
            if (profile?.Cards == null || cardConfig == null)
                return;

            BuildOwnedCards(profile, cardConfig);
            BuildDeckCards(profile, cardConfig);
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

            for (int i = 0; i < profile.Cards.Count; i++)
            {
                if (!cardConfig.TryGetCard(profile.Cards[i].CardId, out BoardActionCardDefinition card))
                    continue;

                int ownedIndex = i;
                bool inDeck = profile.IsOwnedCardInDeck(ownedIndex);

                UIBoardActionCard view = Instantiate(_cardPrefab, _ownedCardsGroup);
                view.name = $"Owned_{card.name}_{ownedIndex + 1}";
                view.Configure(card, () => HandleOwnedCardClicked(ownedIndex));
                view.SetInteractable(!inDeck);
                view.SetFrameMaskEnabled(inDeck);
                _ownedViews.Add(view);
            }
        }

        void BuildDeckCards(PlayerProfile profile, CardConfig cardConfig)
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
            for (int i = 0; i < deck.Count; i++)
            {
                int ownedIndex = deck[i];
                if (ownedIndex < 0 || ownedIndex >= profile.Cards.Count)
                    continue;

                if (!cardConfig.TryGetCard(profile.Cards[ownedIndex].CardId, out BoardActionCardDefinition card))
                    continue;

                int deckIndex = i;
                Button view = Instantiate(_cardInDeckPrefab, _cardsInDeckGroup);
                view.name = $"Deck_{card.name}_{deckIndex + 1}";
                ConfigureDeckButton(view, card);
                view.onClick.AddListener(() => HandleDeckCardClicked(deckIndex));
                _deckViews.Add(view);
            }
        }

        static void ConfigureDeckButton(Button button, BoardActionCardDefinition card)
        {
            UICardVisuals visuals = button.GetComponentInChildren<UICardVisuals>(true);
            if (visuals != null)
            {
                visuals.SetCardData(card);
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = card != null ? card.DisplayName : string.Empty;
        }

        void HandleOwnedCardClicked(int ownedIndex)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || !profile.TryAddOwnedCardToDeck(ownedIndex))
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
