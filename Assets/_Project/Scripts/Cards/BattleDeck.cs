using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace M3P
{
    /// <summary>
    /// Draw pile, hand and discard pile for one battle. The hand is thrown away at end of turn and
    /// redrawn, so hand size measures how many options a turn opens with rather than storage.
    /// </summary>
    public sealed class BattleDeck
    {
        readonly List<BoardActionCardDefinition> _drawPile = new List<BoardActionCardDefinition>();
        readonly List<BoardActionCardDefinition> _hand = new List<BoardActionCardDefinition>();
        readonly List<BoardActionCardDefinition> _discardPile = new List<BoardActionCardDefinition>();

        /// <summary>Raised whenever cards move between piles.</summary>
        public event Action Changed;

        public IReadOnlyList<BoardActionCardDefinition> Hand => _hand;

        public int DrawPileCount => _drawPile.Count;

        public int DiscardPileCount => _discardPile.Count;

        public int TotalCardCount => _drawPile.Count + _hand.Count + _discardPile.Count;

        /// <summary>Rebuilds every pile from a deck asset and shuffles. Call once when a battle starts.</summary>
        public void Reset(DeckDefinition definition)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();

            definition?.BuildCardList(_drawPile);
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
            {
                return false;
            }

            _hand.RemoveAt(index);
            _discardPile.Add(card);
            Changed?.Invoke();
            return true;
        }

        void RecycleDiscardPile()
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        static void Shuffle(List<BoardActionCardDefinition> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }
    }
}
