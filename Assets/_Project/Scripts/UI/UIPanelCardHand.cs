using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Hand UI: instantiates card prefabs from each <see cref="BoardActionCardDefinition"/>, plus a
    /// stamina readout, the queue the player is building and the Resolve, Undo and End Turn commands.
    /// </summary>
    public sealed class UIPanelCardHand : MonoBehaviour
    {
        [SerializeField] UIBoardActionCard _defaultCardPrefab;
        [SerializeField] float _sidePanelWidth = 110f;
        [SerializeField] float _cardSpacing = 8f;
        [SerializeField] float _sectionSpacing = 12f;
        [SerializeField] float _queueStripHeight = 26f;

        readonly List<UIBoardActionCard> _cardViews = new List<UIBoardActionCard>();
        readonly StringBuilder _queueText = new StringBuilder();

        CardPlayController _cardPlay;
        RectTransform _handContainer;
        TextMeshProUGUI _staminaLabel;
        TextMeshProUGUI _queueLabel;
        Button _resolveButton;
        Button _undoButton;
        Button _endTurnButton;
        TextMeshProUGUI _endTurnLabel;
        Coroutine _watchRoutine;

        void OnEnable()
        {
            EnsureLayout();

            if (_watchRoutine == null)
                _watchRoutine = StartCoroutine(WatchControllerRoutine());
        }

        void OnDisable()
        {
            if (_watchRoutine != null)
            {
                StopCoroutine(_watchRoutine);
                _watchRoutine = null;
            }

            Unbind();
            ClearCardViews();
        }

        void OnRectTransformDimensionsChange()
        {
            if (_handContainer != null)
                ApplyCardLayout();
        }

        IEnumerator WatchControllerRoutine()
        {
            while (true)
            {
                CardPlayController active = BattleManager.Instance != null ? BattleManager.Instance.CardPlay : null;

                if (active != _cardPlay)
                {
                    Unbind();
                    _cardPlay = active;

                    if (_cardPlay != null)
                        _cardPlay.Changed += HandleCardPlayChanged;

                    HandleCardPlayChanged();
                }

                RefreshInteractable();
                yield return null;
            }
        }

        void Unbind()
        {
            if (_cardPlay != null)
                _cardPlay.Changed -= HandleCardPlayChanged;

            _cardPlay = null;
        }

        void HandleCardPlayChanged()
        {
            if (NeedsHandRebuild())
                Refresh();
            else
                RefreshInteractable();
        }

        bool NeedsHandRebuild()
        {
            if (_cardPlay == null)
                return _cardViews.Count > 0;

            IReadOnlyList<BoardActionCardDefinition> hand = _cardPlay.Deck.Hand;
            if (hand.Count != _cardViews.Count)
                return true;

            for (int i = 0; i < hand.Count; i++)
            {
                UIBoardActionCard view = _cardViews[i];
                if (view == null || view.HandIndex != i || view.Card != hand[i])
                    return true;
            }

            return false;
        }

        void Refresh()
        {
            ClearCardViews();

            if (_cardPlay == null)
                return;

            IReadOnlyList<BoardActionCardDefinition> hand = _cardPlay.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                BoardActionCardDefinition card = hand[i];
                if (card == null)
                    continue;

                CreateCardView(card, i);
            }

            ApplyCardLayout();
            RefreshInteractable();
        }

        void CreateCardView(BoardActionCardDefinition card, int handIndex)
        {
            UIBoardActionCard prefab = card.CardPrefab != null ? card.CardPrefab : _defaultCardPrefab;
            if (prefab == null)
            {
                Debug.LogError(
                    $"{nameof(UIPanelCardHand)}: card '{card.name}' has no {nameof(BoardActionCardDefinition.CardPrefab)} and no {nameof(_defaultCardPrefab)} is assigned.",
                    this);
                return;
            }

            UIBoardActionCard view = Instantiate(prefab, _handContainer);
            view.name = $"Card_{card.name}_{handIndex + 1}";
            view.Configure(card, _cardPlay, handIndex);
            _cardViews.Add(view);
        }

        void RefreshInteractable()
        {
            if (_cardPlay == null)
            {
                if (_staminaLabel != null)
                    _staminaLabel.text = "Stamina -";

                if (_queueLabel != null)
                    _queueLabel.text = string.Empty;

                SetCommandsInteractable(false, false, false);
                return;
            }

            if (_staminaLabel != null)
            {
                int max = _cardPlay.MaxHandSize;
                _staminaLabel.text =
                    $"Stamina {_cardPlay.CurrentStamina}\n<size=70%>ręka {_cardPlay.Deck.Hand.Count}/{max}";
            }

            if (_queueLabel != null)
                _queueLabel.text = BuildQueueText();

            BattleManager manager = BattleManager.Instance;
            bool playerActing = manager != null && manager.IsPlayerTurn && !_cardPlay.IsBusy;

            SetCommandsInteractable(
                playerActing && _cardPlay.CanResolve(),
                playerActing && _cardPlay.CanUndo(),
                playerActing);

            if (_endTurnLabel != null)
            {
                _endTurnLabel.text = _cardPlay.HasQueuedCards
                    ? "Resolve &\nEnd Turn"
                    : "End Turn";
            }

            IReadOnlyList<BoardActionCardDefinition> hand = _cardPlay.Deck.Hand;
            int count = Mathf.Min(hand.Count, _cardViews.Count);

            for (int i = 0; i < count; i++)
            {
                UIBoardActionCard view = _cardViews[i];
                if (view == null)
                    continue;

                BoardActionCardDefinition card = hand[i];
                bool selected = _cardPlay.SelectedHandIndex == i;
                view.SetSelected(selected);
                view.SetInteractable(playerActing && (selected || _cardPlay.CanQueue(card)));
                view.SetFrameMaskEnabled(card != null && !_cardPlay.CanQueue(card) && !selected);
            }
        }

        void SetCommandsInteractable(bool resolve, bool undo, bool endTurn)
        {
            if (_resolveButton != null)
                _resolveButton.interactable = resolve;

            if (_undoButton != null)
                _undoButton.interactable = undo;

            if (_endTurnButton != null)
                _endTurnButton.interactable = endTurn;
        }

        /// <summary>
        /// Numbered list of the sequence, so the player can read back the order their commands will run in.
        /// </summary>
        string BuildQueueText()
        {
            IReadOnlyList<QueuedCard> entries = _cardPlay.Queue.Entries;
            if (entries.Count == 0)
                return "<alpha=#80>Kolejka pusta — zagraj karty, potem Resolve.";

            _queueText.Clear();
            _queueText.Append("Kolejka: ");

            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    _queueText.Append("  ");

                BoardActionCardDefinition card = entries[i].Card;
                _queueText.Append(i + 1).Append(". ").Append(card != null ? card.DisplayName : "?");
            }

            return _queueText.ToString();
        }

        void ClearCardViews()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null)
                    Destroy(_cardViews[i].gameObject);
            }

            _cardViews.Clear();
        }

        void EnsureLayout()
        {
            if (_handContainer != null)
                return;

            RemoveLayoutGroup(gameObject);

            GameObject stamina = new GameObject("Stamina", typeof(RectTransform));
            stamina.transform.SetParent(transform, false);
            SetupSidePanel((RectTransform)stamina.transform, true);
            _staminaLabel = CreateLabel(stamina.transform, "Stamina -");
            _staminaLabel.alignment = TextAlignmentOptions.Center;

            GameObject hand = new GameObject("Hand", typeof(RectTransform));
            hand.transform.SetParent(transform, false);
            _handContainer = (RectTransform)hand.transform;
            SetupHandContainer();

            BuildQueueStrip();
            BuildCommandColumn();
        }

        /// <summary>Reads back the queued sequence in execution order, above the hand.</summary>
        void BuildQueueStrip()
        {
            GameObject strip = new GameObject("Queue", typeof(RectTransform));
            strip.transform.SetParent(transform, false);

            float horizontalInset = _sectionSpacing + _sidePanelWidth + _sectionSpacing;
            RectTransform rect = (RectTransform)strip.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -_sectionSpacing * 0.5f);
            rect.sizeDelta = new Vector2(-horizontalInset * 2f, _queueStripHeight);

            _queueLabel = CreateLabel(strip.transform, string.Empty);
            _queueLabel.alignment = TextAlignmentOptions.Left;
            _queueLabel.enableAutoSizing = false;
            _queueLabel.fontSize = 16f;
            _queueLabel.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>
        /// Resolve, Undo and End Turn stacked on the right. Undo only reaches the last queued card, and
        /// End Turn folds a Resolve into itself when the queue is not empty.
        /// </summary>
        void BuildCommandColumn()
        {
            GameObject column = new GameObject("Commands", typeof(RectTransform));
            column.transform.SetParent(transform, false);
            RectTransform columnRect = (RectTransform)column.transform;
            SetupSidePanel(columnRect, false);

            _resolveButton = CreateCommandButton(
                columnRect,
                "Resolve",
                "Resolve",
                new Color(0.2f, 0.4f, 0.24f, 0.95f),
                0,
                out _);
            _resolveButton.onClick.AddListener(() => BattleManager.Instance?.RequestResolve());

            _undoButton = CreateCommandButton(
                columnRect,
                "Undo",
                "Undo",
                new Color(0.28f, 0.28f, 0.34f, 0.95f),
                1,
                out _);
            _undoButton.onClick.AddListener(() => _cardPlay?.UndoLastCard());

            _endTurnButton = CreateCommandButton(
                columnRect,
                "EndTurn",
                "End Turn",
                new Color(0.42f, 0.2f, 0.2f, 0.95f),
                2,
                out _endTurnLabel);
            _endTurnButton.onClick.AddListener(() => BattleManager.Instance?.RequestEndTurn());
        }

        Button CreateCommandButton(
            RectTransform parent,
            string name,
            string text,
            Color color,
            int slot,
            out TextMeshProUGUI label)
        {
            const int slotCount = 3;

            GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            button.GetComponent<Image>().color = color;

            float slotHeight = 1f / slotCount;
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 1f - slotHeight * (slot + 1));
            rect.anchorMax = new Vector2(1f, 1f - slotHeight * slot);
            rect.offsetMin = new Vector2(0f, 2f);
            rect.offsetMax = new Vector2(0f, -2f);

            label = CreateLabel(button.transform, text);
            label.alignment = TextAlignmentOptions.Center;
            return button.GetComponent<Button>();
        }

        void SetupSidePanel(RectTransform rect, bool left)
        {
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0.5f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.anchoredPosition = new Vector2(left ? _sectionSpacing : -_sectionSpacing, 0f);
            rect.sizeDelta = new Vector2(_sidePanelWidth, GetReferenceCardHeight());
        }

        void SetupHandContainer()
        {
            float horizontalInset = _sectionSpacing + _sidePanelWidth + _sectionSpacing;

            _handContainer.anchorMin = new Vector2(0f, 0.5f);
            _handContainer.anchorMax = new Vector2(1f, 0.5f);
            _handContainer.pivot = new Vector2(0.5f, 0.5f);
            _handContainer.anchoredPosition = new Vector2(0f, -_queueStripHeight * 0.5f);
            _handContainer.sizeDelta = new Vector2(-horizontalInset * 2f, GetReferenceCardHeight());
        }

        void ApplyCardLayout()
        {
            int count = _cardViews.Count;
            if (count == 0)
                return;

            float totalWidth = 0f;
            for (int i = 0; i < count; i++)
            {
                UIBoardActionCard view = _cardViews[i];
                if (view == null)
                    continue;

                totalWidth += ((RectTransform)view.transform).sizeDelta.x;
            }

            totalWidth += (count - 1) * _cardSpacing;

            float x = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                UIBoardActionCard view = _cardViews[i];
                if (view == null)
                    continue;

                RectTransform rect = (RectTransform)view.transform;
                float cardWidth = rect.sizeDelta.x;

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x + cardWidth * 0.5f, 0f);

                x += cardWidth + _cardSpacing;
            }
        }

        float GetReferenceCardHeight()
        {
            if (_defaultCardPrefab == null)
                return 96f;

            return ((RectTransform)_defaultCardPrefab.transform).sizeDelta.y;
        }

        static void RemoveLayoutGroup(GameObject target)
        {
            HorizontalLayoutGroup layoutGroup = target.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
                Destroy(layoutGroup);
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string text)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 4f);
            rect.offsetMax = new Vector2(-6f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 22f;
            label.raycastTarget = false;
            return label;
        }
    }
}
