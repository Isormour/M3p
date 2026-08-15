using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The registry of every board-action card in the game and the one place card ids come from. Ids
    /// are handed out once and never reused, so a card referenced by a saved profile survives the
    /// list being reordered or trimmed.
    /// </summary>
    [CreateAssetMenu(fileName = "CardConfig", menuName = "M3P/Card Config", order = 22)]
    public class CardConfig : ScriptableObject
    {
        /// <summary>Id of a card that this config does not know about.</summary>
        public const int InvalidCardId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans the card in every profile that saved it.")]
            public int Id;
            public BoardActionCardDefinition Card;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Only ever counts up, so removing a card does not free its id for the next one.</summary>
        [SerializeField, HideInInspector] int _nextCardId = InvalidCardId + 1;

        Dictionary<BoardActionCardDefinition, int> _idsByCard;
        Dictionary<int, BoardActionCardDefinition> _cardsById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Id of a registered card, or <see cref="InvalidCardId"/> when it is not in this config.</summary>
        public int GetCardId(BoardActionCardDefinition card)
        {
            if (card == null)
                return InvalidCardId;

            EnsureLookups();
            return _idsByCard.TryGetValue(card, out int id) ? id : InvalidCardId;
        }

        public bool TryGetCard(int cardId, out BoardActionCardDefinition card)
        {
            EnsureLookups();
            return _cardsById.TryGetValue(cardId, out card);
        }

        public BoardActionCardDefinition GetCard(int cardId)
        {
            return TryGetCard(cardId, out BoardActionCardDefinition card) ? card : null;
        }

        /// <summary>Turns owned copies back into the card assets a battle draws.</summary>
        public void ResolveOwnedCards(IReadOnlyList<OwnedCard> owned, List<BoardActionCardDefinition> destination)
        {
            destination.Clear();
            AppendOwnedCards(owned, destination);
        }

        /// <summary>Turns the profile's current deck into the card assets a battle draws.</summary>
        public void ResolveDeck(PlayerProfile profile, List<BoardActionCardDefinition> destination)
        {
            destination.Clear();

            if (profile?.Cards == null)
                return;

            IReadOnlyList<int> deck = profile.GetDeckIndices();
            for (int i = 0; i < deck.Count; i++)
            {
                int ownedIndex = deck[i];
                if (ownedIndex < 0 || ownedIndex >= profile.Cards.Count)
                    continue;

                AppendCard(profile.Cards[ownedIndex].CardId, destination);
            }
        }

        void AppendOwnedCards(IReadOnlyList<OwnedCard> owned, List<BoardActionCardDefinition> destination)
        {
            if (owned == null)
                return;

            for (int i = 0; i < owned.Count; i++)
                AppendCard(owned[i].CardId, destination);
        }

        void AppendCard(int cardId, List<BoardActionCardDefinition> destination)
        {
            if (TryGetCard(cardId, out BoardActionCardDefinition card))
                destination.Add(card);
            else
                Debug.LogWarning(
                    $"{nameof(CardConfig)} '{name}': profile references card id {cardId}, which is missing from this registry.",
                    this);
        }

        void EnsureLookups()
        {
            if (_idsByCard != null)
                return;

            Entry[] entries = Entries;
            _idsByCard = new Dictionary<BoardActionCardDefinition, int>(entries.Length);
            _cardsById = new Dictionary<int, BoardActionCardDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Card == null || entry.Id == InvalidCardId)
                    continue;

                if (_cardsById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(CardConfig)} '{name}': card id {entry.Id} is used twice. Give '{entry.Card.name}' a unique id.",
                        this);
                    continue;
                }

                _idsByCard[entry.Card] = entry.Id;
                _cardsById[entry.Id] = entry.Card;
            }
        }

        /// <summary>
        /// Hands the next free id to every newly added card. Existing ids are left alone so nothing
        /// already written to a save changes meaning.
        /// </summary>
        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Length; i++)
                _nextCardId = Mathf.Max(_nextCardId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Card == null || _entries[i].Id > InvalidCardId)
                    continue;

                _entries[i].Id = _nextCardId++;
            }
        }

        void OnEnable()
        {
            _idsByCard = null;
            _cardsById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsByCard = null;
            _cardsById = null;
        }
    }
}
