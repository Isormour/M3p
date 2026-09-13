using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: owned cards on the left, the current battle deck on the right.
    /// Clicking an owned card adds that copy to the deck; clicking a deck card removes it.
    /// </summary>
    public sealed class UIPanelPlayerCards : UIPanelClosable
    {
        [SerializeField] Transform _ownedCardsGroup;
        [SerializeField] Transform _cardsInDeckGroup;
        [SerializeField] UIDeckCardButton _cardInDeckPrefab;
        [SerializeField] UIBoardActionCard _cardPrefab;

        readonly List<UIBoardActionCard> _ownedViews = new List<UIBoardActionCard>();
        readonly List<UIDeckCardButton> _deckViews = new List<UIDeckCardButton>();

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
                view.SetSelectedHighlight(inDeck);
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
                UIDeckCardButton view = Instantiate(_cardInDeckPrefab, _cardsInDeckGroup);
                view.name = $"Deck_{card.name}_{deckIndex + 1}";
                view.Configure(card, () => HandleDeckCardClicked(deckIndex));
                _deckViews.Add(view);
            }
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
    }
}
