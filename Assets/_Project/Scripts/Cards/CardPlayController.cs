using System;
using System.Collections;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Drives the player's turn: which card is selected, which board cells it still needs, and paying
    /// the action points once a play is complete.
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        [Tooltip("Cards the player starts a battle with.")]
        [SerializeField] DeckDefinition _startingDeck;

        readonly BattleDeck _deck = new BattleDeck();
        readonly List<Vector2Int> _pickedTargets = new List<Vector2Int>();
        readonly List<Match3Tile> _highlightedTiles = new List<Match3Tile>();

        Match3Board _board;
        PlayerBattleCharacter _player;
        BoardActionCardDefinition _selectedCard;
        bool _isPlaying;

        /// <summary>Raised when the hand, the selection or the action point pool changes.</summary>
        public event Action Changed;

        public BattleDeck Deck => _deck;

        public BoardActionCardDefinition SelectedCard => _selectedCard;

        public bool IsPlaying => _isPlaying;

        /// <summary>Cells already picked for the selected card.</summary>
        public IReadOnlyList<Vector2Int> PickedTargets => _pickedTargets;

        public void BeginBattle(Match3Board board, PlayerBattleCharacter player)
        {
            UnbindBoard();

            _board = board;
            _player = player;
            _selectedCard = null;
            _pickedTargets.Clear();
            _highlightedTiles.Clear();

            if (_board != null)
            {
                _board.TileClicked += HandleTileClicked;
            }

            _deck.Reset(_startingDeck);

            if (_startingDeck == null)
            {
                Debug.LogError($"{nameof(CardPlayController)}: assign {nameof(_startingDeck)} or the player will have no cards.", this);
            }

            Changed?.Invoke();
        }

        public void EndBattle()
        {
            UnbindBoard();
            CancelSelection();
            _player = null;
            Changed?.Invoke();
        }

        /// <summary>Draws a fresh hand. Action points are reset separately by the character.</summary>
        public void BeginTurn()
        {
            CancelSelection();
            _deck.DrawUpTo(MaxHandSize);
            Changed?.Invoke();
        }

        /// <summary>Throws the unplayed hand away, so hand size never doubles as storage.</summary>
        public void EndTurn()
        {
            CancelSelection();
            _deck.DiscardHand();
            Changed?.Invoke();
        }

        public int MaxHandSize => _player?.Stats?.Soft?.MaxHandSize ?? 0;

        public int CurrentActionPoints => _player?.Stats?.Soft?.CurrentActionPoints ?? 0;

        public bool CanPlay(BoardActionCardDefinition card)
        {
            if (card == null || card.Logic == null || _board == null || _isPlaying || _board.IsResolving)
            {
                return false;
            }

            SoftStats soft = _player?.Stats?.Soft;
            return soft != null && soft.HasActionPoints(card.ActionPointCost);
        }

        /// <summary>True while at least one card in hand is affordable.</summary>
        public bool HasPlayableCard()
        {
            IReadOnlyList<BoardActionCardDefinition> hand = _deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (CanPlay(hand[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public void SelectCard(BoardActionCardDefinition card)
        {
            if (_selectedCard == card)
            {
                CancelSelection();
                return;
            }

            CancelSelection();

            if (!CanPlay(card))
            {
                return;
            }

            _selectedCard = card;
            Changed?.Invoke();

            if (card.Targeting == CardTargeting.None)
            {
                StartCoroutine(PlayRoutine(card, new List<Vector2Int>()));
            }
        }

        public void CancelSelection()
        {
            ClearHighlights();
            _pickedTargets.Clear();

            if (_selectedCard == null)
            {
                return;
            }

            _selectedCard = null;
            Changed?.Invoke();
        }

        void HandleTileClicked(Match3Tile tile)
        {
            if (_selectedCard == null || _isPlaying || tile == null)
            {
                return;
            }

            BoardActionLogic logic = _selectedCard.Logic;
            Vector2Int candidate = new Vector2Int(tile.X, tile.Y);

            if (!logic.IsValidTarget(_board, _pickedTargets, candidate))
            {
                return;
            }

            _pickedTargets.Add(candidate);
            tile.SetSelected(true);
            _highlightedTiles.Add(tile);

            if (_pickedTargets.Count < logic.RequiredTargetCount)
            {
                Changed?.Invoke();
                return;
            }

            StartCoroutine(PlayRoutine(_selectedCard, new List<Vector2Int>(_pickedTargets)));
        }

        IEnumerator PlayRoutine(BoardActionCardDefinition card, List<Vector2Int> targets)
        {
            if (_board == null)
            {
                yield break;
            }

            _isPlaying = true;

            _player?.Stats?.Soft?.TrySpendActionPoint(card.ActionPointCost);
            _deck.TryDiscardFromHand(card);

            ClearHighlights();
            _pickedTargets.Clear();
            _selectedCard = null;
            Changed?.Invoke();

            yield return _board.ExecuteActionRoutine(card.Logic, targets);

            _isPlaying = false;
            Changed?.Invoke();
        }

        void ClearHighlights()
        {
            for (int i = 0; i < _highlightedTiles.Count; i++)
            {
                if (_highlightedTiles[i] != null)
                {
                    _highlightedTiles[i].SetSelected(false);
                }
            }

            _highlightedTiles.Clear();
        }

        void UnbindBoard()
        {
            if (_board != null)
            {
                _board.TileClicked -= HandleTileClicked;
            }

            _board = null;
        }

        void OnDestroy()
        {
            UnbindBoard();
        }
    }
}
