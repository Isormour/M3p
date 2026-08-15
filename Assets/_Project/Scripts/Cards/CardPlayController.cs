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
        readonly BattleDeck _deck = new BattleDeck();
        readonly List<BoardActionCardDefinition> _resolvedDeck = new List<BoardActionCardDefinition>();
        readonly List<Vector2Int> _pickedTargets = new List<Vector2Int>();
        readonly List<Match3Tile> _highlightedTiles = new List<Match3Tile>();

        Match3Board _board;
        PlayerBattleCharacter _player;
        BoardActionCardDefinition _selectedCard;
        int _selectedHandIndex = -1;
        bool _isPlaying;
        bool _awaitingChoice;
        UICardChoiceOverlay _choiceOverlay;
        readonly List<CardChoiceOption> _choiceBuffer = new List<CardChoiceOption>();

        /// <summary>Raised when the hand, the selection or the action point pool changes.</summary>
        public event Action Changed;

        public BattleDeck Deck => _deck;

        public BoardActionCardDefinition SelectedCard => _selectedCard;

        public int SelectedHandIndex => _selectedHandIndex;

        public bool IsPlaying => _isPlaying || _awaitingChoice;

        /// <summary>Cells already picked for the selected card.</summary>
        public IReadOnlyList<Vector2Int> PickedTargets => _pickedTargets;

        public void BeginBattle(Match3Board board, PlayerBattleCharacter player)
        {
            UnbindBoard();

            _board = board;
            _player = player;
            _selectedCard = null;
            _selectedHandIndex = -1;
            _pickedTargets.Clear();
            _highlightedTiles.Clear();

            if (_board != null)
            {
                _board.TileClicked += HandleTileClicked;
            }

            LoadDeckFromProfile();
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

        public void SelectCardAt(int handIndex)
        {
            IReadOnlyList<BoardActionCardDefinition> hand = _deck.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
                return;

            if (_selectedHandIndex == handIndex)
            {
                CancelSelection();
                return;
            }

            CancelSelection();

            BoardActionCardDefinition card = hand[handIndex];
            if (!CanPlay(card))
                return;

            _selectedHandIndex = handIndex;
            _selectedCard = card;
            Changed?.Invoke();

            if (card.Targeting == CardTargeting.None)
            {
                if (card.Logic.ExtraChoice != CardExtraChoice.None)
                    PromptExtraChoice(handIndex, new List<Vector2Int>());
                else
                    StartCoroutine(PlayRoutine(handIndex, new List<Vector2Int>(), 0));
            }
        }

        public void CancelSelection()
        {
            HideChoiceOverlay();
            ClearHighlights();
            _pickedTargets.Clear();
            _awaitingChoice = false;

            if (_selectedHandIndex < 0)
                return;

            _selectedHandIndex = -1;
            _selectedCard = null;
            Changed?.Invoke();
        }

        void HandleTileClicked(Match3Tile tile)
        {
            if (_selectedCard == null || _isPlaying || _awaitingChoice || tile == null)
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

            int handIndex = _selectedHandIndex;
            List<Vector2Int> targets = new List<Vector2Int>(_pickedTargets);
            if (logic.ExtraChoice != CardExtraChoice.None)
                PromptExtraChoice(handIndex, targets);
            else
                StartCoroutine(PlayRoutine(handIndex, targets, 0));
        }

        void PromptExtraChoice(int handIndex, List<Vector2Int> targets)
        {
            BoardActionLogic logic = _selectedCard != null ? _selectedCard.Logic : null;
            if (logic == null)
                return;

            logic.CollectExtraChoices(_board, _choiceBuffer);
            if (_choiceBuffer.Count == 0)
            {
                StartCoroutine(PlayRoutine(handIndex, targets, 0));
                return;
            }

            Transform parent = FindOverlayParent();
            if (parent == null)
            {
                Debug.LogError($"{nameof(CardPlayController)}: no Canvas found for the extra card choice.", this);
                StartCoroutine(PlayRoutine(handIndex, targets, _choiceBuffer[0].Value));
                return;
            }

            HideChoiceOverlay();
            _awaitingChoice = true;
            string title = logic.ExtraChoice == CardExtraChoice.GravityDirection ? "Kierunek" : "Kolor";
            _choiceOverlay = UICardChoiceOverlay.Show(
                parent,
                title,
                _choiceBuffer,
                value => HandleExtraChoicePicked(handIndex, targets, value),
                CancelSelection);
            Changed?.Invoke();
        }

        void HandleExtraChoicePicked(int handIndex, List<Vector2Int> targets, int extraChoice)
        {
            HideChoiceOverlay();
            _awaitingChoice = false;
            StartCoroutine(PlayRoutine(handIndex, targets, extraChoice));
        }

        void HideChoiceOverlay()
        {
            if (_choiceOverlay == null)
                return;

            _choiceOverlay.Dismiss();
            _choiceOverlay = null;
        }

        Transform FindOverlayParent()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Canvas best = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;

                if (best == null || canvas.sortingOrder >= best.sortingOrder)
                    best = canvas;
            }

            return best != null ? best.transform : null;
        }

        IEnumerator PlayRoutine(int handIndex, List<Vector2Int> targets, int extraChoice)
        {
            if (_board == null)
                yield break;

            IReadOnlyList<BoardActionCardDefinition> hand = _deck.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
                yield break;

            BoardActionCardDefinition card = hand[handIndex];
            _isPlaying = true;
            _awaitingChoice = false;

            _player?.Stats?.Soft?.TrySpendActionPoint(card.ActionPointCost);
            _deck.TryDiscardFromHandAt(handIndex);

            HideChoiceOverlay();
            ClearHighlights();
            _pickedTargets.Clear();
            _selectedHandIndex = -1;
            _selectedCard = null;
            Changed?.Invoke();

            yield return _board.ExecuteActionRoutine(card.Logic, targets, extraChoice);

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

        void LoadDeckFromProfile()
        {
            PlayerProfile profile = _player != null ? _player.Profile : null;
            CardConfig cardConfig = GameManager.Instance != null ? GameManager.Instance.Config?.Cards : null;

            if (profile == null || cardConfig == null)
            {
                Debug.LogError(
                    $"{nameof(CardPlayController)}: the battle deck comes from the player profile. Load through {nameof(GameManager)} with a {nameof(CardConfig)} assigned, or the player will have no cards.",
                    this);
                _deck.Reset(null);
                return;
            }

            cardConfig.ResolveDeck(profile, _resolvedDeck);

            if (_resolvedDeck.Count == 0)
            {
                Debug.LogError(
                    $"{nameof(CardPlayController)}: the current deck has no cards that {nameof(CardConfig)} can resolve. Add cards in the cards panel, or start a new profile from {nameof(PlayerStartConfig)}.",
                    this);
            }

            _deck.Reset(_resolvedDeck);
        }

        void OnDestroy()
        {
            HideChoiceOverlay();
            UnbindBoard();
        }
    }
}
