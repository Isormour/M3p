using System;
using System.Collections.Generic;
using Match3;

namespace M3P
{
    /// <summary>
    /// Draw pile, hand, queued sequence and discard pile for one battle. The hand is thrown away at end
    /// of turn and redrawn, so hand size measures how many options a turn opens with rather than storage.
    /// Cards committed to the queue sit in their own list: they have left the hand but are not discarded
    /// yet, because Undo can still pull the last one back.
    /// </summary>
    public sealed class BattleDeck
    {
        readonly List<BoardActionCardDefinition> _drawPile = new List<BoardActionCardDefinition>();
        readonly List<BoardActionCardDefinition> _hand = new List<BoardActionCardDefinition>();
        readonly List<BoardActionCardDefinition> _sequence = new List<BoardActionCardDefinition>();
        readonly List<BoardActionCardDefinition> _discardPile = new List<BoardActionCardDefinition>();

        System.Random _random = new System.Random(Environment.TickCount);

        /// <summary>Raised whenever cards move between piles.</summary>
        public event Action Changed;

        public IReadOnlyList<BoardActionCardDefinition> Hand => _hand;

        public int DrawPileCount => _drawPile.Count;

        public int DiscardPileCount => _discardPile.Count;

        /// <summary>Cards committed to the queue: out of hand, not yet discarded, still recoverable by Undo.</summary>
        public int SequenceCount => _sequence.Count;

        public int TotalCardCount => _drawPile.Count + _hand.Count + _sequence.Count + _discardPile.Count;

        /// <summary>Fixes the shuffle stream so a battle can be replayed from a seed.</summary>
        public void SetRandomSeed(int seed)
        {
            _random = new System.Random(seed);
        }

        /// <summary>Rebuilds every pile from the cards a profile owns and shuffles. Call once when a battle starts.</summary>
        public void Reset(IReadOnlyList<BoardActionCardDefinition> cards)
        {
            _drawPile.Clear();
            _hand.Clear();
            _sequence.Clear();
            _discardPile.Clear();

            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null)
                        _drawPile.Add(cards[i]);
                }
            }

            Shuffle(_drawPile);
            Changed?.Invoke();
        }

        /// <summary>Draws until the hand holds <paramref name="handSize"/> cards or the deck runs dry.</summary>
        public void DrawUpTo(int handSize)
        {
            bool changed = false;

            while (_hand.Count < handSize)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        break;
                    }

                    RecycleDiscardPile();
                }

                int last = _drawPile.Count - 1;
                _hand.Add(_drawPile[last]);
                _drawPile.RemoveAt(last);
                changed = true;
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public void DiscardHand()
        {
            if (_hand.Count == 0)
            {
                return;
            }

            _discardPile.AddRange(_hand);
            _hand.Clear();
            Changed?.Invoke();
        }

        /// <summary>Moves one copy of a card from hand to the discard pile.</summary>
        public bool TryDiscardFromHand(BoardActionCardDefinition card)
        {
            int index = _hand.IndexOf(card);
            if (index < 0)
                return false;

            return TryDiscardFromHandAt(index);
        }

        /// <summary>Moves the card at <paramref name="handIndex"/> from hand to the discard pile.</summary>
        public bool TryDiscardFromHandAt(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _hand.Count)
                return false;

            BoardActionCardDefinition card = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            _discardPile.Add(card);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Commits a card from hand to the queued sequence.</summary>
        public BoardActionCardDefinition MoveFromHandToSequence(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _hand.Count)
                return null;

            BoardActionCardDefinition card = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            _sequence.Add(card);
            Changed?.Invoke();
            return card;
        }

        /// <summary>
        /// Puts an undone card back in hand. The original slot is honoured where it still exists, so the
        /// hand does not reshuffle itself under the player's cursor.
        /// </summary>
        public void ReturnFromSequenceToHand(BoardActionCardDefinition card, int handIndex)
        {
            if (card == null)
                return;

            int inSequence = _sequence.LastIndexOf(card);
            if (inSequence >= 0)
                _sequence.RemoveAt(inSequence);

            if (handIndex < 0 || handIndex > _hand.Count)
                handIndex = _hand.Count;

            _hand.Insert(handIndex, card);
            Changed?.Invoke();
        }

        /// <summary>Sends every card of a resolved sequence to the discard pile.</summary>
        public void DiscardSequence()
        {
            if (_sequence.Count == 0)
                return;

            _discardPile.AddRange(_sequence);
            _sequence.Clear();
            Changed?.Invoke();
        }

        void RecycleDiscardPile()
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        void Shuffle(List<BoardActionCardDefinition> cards)
        {
            SimBoard.ShuffleDeterministic(cards, _random.Next());
        }
    }
}
