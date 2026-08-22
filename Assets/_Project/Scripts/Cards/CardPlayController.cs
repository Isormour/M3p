using System;
using System.Collections;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Drives the planning half of the player's turn: which card is selected, which cells it still needs,
    /// and the queue those cards build. Playing a card costs stamina and adds a command to the queue —
    /// the board itself does not move until <see cref="ResolveSequenceRoutine"/> runs.
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        readonly BattleDeck _deck = new BattleDeck();
        readonly CardQueue _queue = new CardQueue();
        readonly List<BoardActionCardDefinition> _resolvedDeck = new List<BoardActionCardDefinition>();
        readonly List<Vector2Int> _pickedTargets = new List<Vector2Int>();
        readonly List<Match3Tile> _highlightedTiles = new List<Match3Tile>();
        readonly List<CardChoiceOption> _choiceBuffer = new List<CardChoiceOption>();
        readonly List<BoardOp> _opsBuffer = new List<BoardOp>();

        Match3Board _board;
        PlayerBattleCharacter _player;
        BoardActionCardDefinition _selectedCard;
        int _selectedHandIndex = -1;
        bool _isResolving;
        bool _awaitingChoice;
        UICardChoiceOverlay _choiceOverlay;
        System.Random _seedSource;

        /// <summary>Raised when the hand, the selection, the queue or the stamina pool changes.</summary>
        public event Action Changed;

        public BattleDeck Deck => _deck;

        /// <summary>The sequence being built this turn, and the predicted board it produces.</summary>
        public CardQueue Queue => _queue;

        public BoardActionCardDefinition SelectedCard => _selectedCard;

        public int SelectedHandIndex => _selectedHandIndex;

        /// <summary>True while a Resolve is running or a card is waiting on a colour/direction prompt.</summary>
        public bool IsBusy => _isResolving || _awaitingChoice;

        /// <summary>Cells already picked for the selected card.</summary>
        public IReadOnlyList<Vector2Int> PickedTargets => _pickedTargets;

        /// <summary>The real board plus every queued command. What later cards target and the preview draws.</summary>
        public SimBoard PredictedBoard => _queue.Predicted;

        public int QueuedCardCount => _queue.Count;

        public bool HasQueuedCards => !_queue.IsEmpty;

        public int MaxHandSize => _player?.Stats?.Soft?.MaxHandSize ?? 0;

        /// <summary>Player-facing name for action points, which the design calls Stamina.</summary>
        public int CurrentStamina => _player?.Stats?.Soft?.CurrentActionPoints ?? 0;

        public void BeginBattle(Match3Board board, PlayerBattleCharacter player)
        {
            UnbindBoard();

            _board = board;
            _player = player;
            _selectedCard = null;
            _selectedHandIndex = -1;
            _pickedTargets.Clear();
            _highlightedTiles.Clear();
            _queue.Clear();
            _seedSource = new System.Random(Environment.TickCount);

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
            _queue.Clear();
            _player = null;
            Changed?.Invoke();
        }

        /// <summary>Draws a fresh hand and opens the first sequence. Stamina is reset by the character.</summary>
        public void BeginTurn()
        {
            CancelSelection();
            _deck.DrawUpTo(MaxHandSize);
            BeginSequence();
            Changed?.Invoke();
        }

        /// <summary>Throws the unplayed hand away, so hand size never doubles as storage.</summary>
        public void EndTurn()
        {
            CancelSelection();
            _deck.DiscardSequence();
            _queue.Clear();
            _deck.DiscardHand();
            Changed?.Invoke();
        }

        /// <summary>
        /// Starts a new queue from the current board. Called at the start of a turn and after every
        /// Resolve, because a turn may contain several sequences.
        /// </summary>
        public void BeginSequence()
        {
            if (_board == null)
            {
                _queue.Clear();
                return;
            }

            _queue.Reset(_board.CaptureSimBoard());
        }

        /// <summary>Draws extra cards mid-turn, for reward runes and tile upgrades that grant a draw.</summary>
        public void DrawCards(int count)
        {
            if (count <= 0)
                return;

            _deck.DrawUpTo(_deck.Hand.Count + count);
            Changed?.Invoke();
        }

        public bool CanQueue(BoardActionCardDefinition card)
        {
            if (card == null || card.Logic == null || _board == null || IsBusy || _board.IsResolving)
            {
                return false;
            }

            BattleManager manager = BattleManager.Instance;
            if (manager != null && manager.IsAwaitingSkillChoice)
                return false;

            if (!_queue.CanEnqueue(card.Logic))
            {
                return false;
            }

            SoftStats soft = _player?.Stats?.Soft;
            return soft != null && soft.HasActionPoints(card.ActionPointCost);
        }

        /// <summary>True while at least one card in hand can still join the queue.</summary>
        public bool HasQueueableCard()
        {
            IReadOnlyList<BoardActionCardDefinition> hand = _deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (CanQueue(hand[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A queued sequence can always be resolved, which is itself a legal action.</summary>
        public bool CanResolve()
        {
            return _board != null && !_board.IsResolving && !IsBusy && !_queue.IsEmpty;
        }

        public bool CanUndo()
        {
            return _board != null && !_board.IsResolving && !IsBusy && !_queue.IsEmpty;
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
            if (!CanQueue(card))
                return;

            _selectedHandIndex = handIndex;
            _selectedCard = card;
            Changed?.Invoke();

            if (card.Targeting == CardTargeting.None)
            {
                if (card.Logic.ExtraChoice != CardExtraChoice.None)
                    PromptExtraChoice(handIndex, new List<Vector2Int>());
                else
                    EnqueueCard(handIndex, new List<Vector2Int>(), 0);
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

        /// <summary>
        /// Takes the last card back out of the queue: it returns to hand, its stamina is refunded and the
        /// predicted board is recomputed. Only the last card can go, because pulling one from the middle
        /// would invalidate the targets every card behind it was planned against.
        /// </summary>
        public bool UndoLastCard()
        {
            if (!CanUndo())
                return false;

            CancelSelection();

            QueuedCard entry = _queue.RemoveLast();
            if (entry == null)
                return false;

            _player?.Stats?.Soft?.AddActionPoints(entry.StaminaPaid);
            _deck.ReturnFromSequenceToHand(entry.Card, entry.HandIndex);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Runs the queued sequence on the real board. Resolve ends the sequence, not the turn: once the
        /// payout has settled the player gets control back and may queue another sequence.
        /// </summary>
        public IEnumerator ResolveSequenceRoutine()
        {
            if (!CanResolve())
                yield break;

            CancelSelection();
            _isResolving = true;

            int cardCount = _queue.Count;
            _queue.CollectOps(_opsBuffer);
            _deck.DiscardSequence();
            _queue.Clear();
            Changed?.Invoke();

            yield return _board.ResolveSequenceRoutine(_opsBuffer, cardCount);

            _isResolving = false;
            BeginSequence();
            Changed?.Invoke();
        }

        void HandleTileClicked(Match3Tile tile)
        {
            if (_selectedCard == null || IsBusy || tile == null)
            {
                return;
            }

            SimBoard predicted = _queue.Predicted;
            if (predicted == null)
            {
                return;
            }

            BoardActionLogic logic = _selectedCard.Logic;
            Vector2Int candidate = new Vector2Int(tile.X, tile.Y);

            if (!logic.IsValidTarget(predicted, _pickedTargets, candidate))
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
                EnqueueCard(handIndex, targets, 0);
        }

        void PromptExtraChoice(int handIndex, List<Vector2Int> targets)
        {
            BoardActionLogic logic = _selectedCard != null ? _selectedCard.Logic : null;
            if (logic == null)
                return;

            logic.CollectExtraChoices(_queue.Predicted, _choiceBuffer);
            if (_choiceBuffer.Count == 0)
            {
                EnqueueCard(handIndex, targets, 0);
                return;
            }

            Transform parent = FindOverlayParent();
            if (parent == null)
            {
                Debug.LogError($"{nameof(CardPlayController)}: no Canvas found for the extra card choice.", this);
                EnqueueCard(handIndex, targets, _choiceBuffer[0].Value);
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
            EnqueueCard(handIndex, targets, extraChoice);
        }

        /// <summary>
        /// Pays for the card and appends its commands to the queue. Commands are compiled here, against
        /// the predicted board, and stored — the Resolve replays exactly these, so a random card that
        /// fixes its seed now cannot surprise the player later.
        /// </summary>
        void EnqueueCard(int handIndex, List<Vector2Int> targets, int extraChoice)
        {
            if (_board == null)
                return;

            IReadOnlyList<BoardActionCardDefinition> hand = _deck.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
                return;

            BoardActionCardDefinition card = hand[handIndex];
            SimBoard predicted = _queue.Predicted;
            if (card?.Logic == null || predicted == null || !_queue.CanEnqueue(card.Logic))
                return;

            SoftStats soft = _player?.Stats?.Soft;
            if (soft == null || !soft.TrySpendActionPoint(card.ActionPointCost))
                return;

            int seed = card.Logic.NeedsSeed ? NextSeed() : 0;
            _opsBuffer.Clear();
            card.Logic.BuildOps(predicted, targets, extraChoice, seed, _opsBuffer);

            if (_opsBuffer.Count == 0)
            {
                soft.AddActionPoints(card.ActionPointCost);
                CancelSelection();
                return;
            }

            _deck.MoveFromHandToSequence(handIndex);
            _queue.Enqueue(new QueuedCard(
                card,
                handIndex,
                card.ActionPointCost,
                targets.ToArray(),
                extraChoice,
                seed,
                _opsBuffer.ToArray()));

            HideChoiceOverlay();
            ClearHighlights();
            _pickedTargets.Clear();
            _selectedHandIndex = -1;
            _selectedCard = null;
            _awaitingChoice = false;
            Changed?.Invoke();
        }

        int NextSeed()
        {
            _seedSource ??= new System.Random(Environment.TickCount);
            return _seedSource.Next(int.MinValue, int.MaxValue);
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
