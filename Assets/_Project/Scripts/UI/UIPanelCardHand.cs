using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Hand UI: instantiates card prefabs from each <see cref="BoardActionCardDefinition"/>,
    /// plus an action point readout and an end turn button.
    /// </summary>
    public sealed class UIPanelCardHand : MonoBehaviour
    {
        [SerializeField] UIBoardActionCard _defaultCardPrefab;
        [SerializeField] float _sidePanelWidth = 110f;
        [SerializeField] float _cardSpacing = 8f;
        [SerializeField] float _sectionSpacing = 12f;

        readonly List<UIBoardActionCard> _cardViews = new List<UIBoardActionCard>();

        CardPlayController _cardPlay;
        RectTransform _handContainer;
        TextMeshProUGUI _actionPointsLabel;
        Button _endTurnButton;
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
                if (_actionPointsLabel != null)
                    _actionPointsLabel.text = "AP -";

                if (_endTurnButton != null)
                    _endTurnButton.interactable = false;

                return;
            }

            if (_actionPointsLabel != null)
            {
                int max = _cardPlay.MaxHandSize;
                _actionPointsLabel.text = $"AP {_cardPlay.CurrentActionPoints}\n<size=70%>hand {_cardPlay.Deck.Hand.Count}/{max}";
            }

            BattleManager manager = BattleManager.Instance;
            bool playerActing = manager != null && manager.IsPlayerTurn && !_cardPlay.IsPlaying;

            if (_endTurnButton != null)
                _endTurnButton.interactable = playerActing;

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
                view.SetInteractable(playerActing && (selected || _cardPlay.CanPlay(card)));
                view.SetFrameMaskEnabled(card != null && _cardPlay.CurrentActionPoints < card.ActionPointCost);
            }
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

            GameObject actionPoints = new GameObject("ActionPoints", typeof(RectTransform));
            actionPoints.transform.SetParent(transform, false);
            SetupSidePanel((RectTransform)actionPoints.transform, true);
            _actionPointsLabel = CreateLabel(actionPoints.transform, "AP -");
            _actionPointsLabel.alignment = TextAlignmentOptions.Center;

            GameObject hand = new GameObject("Hand", typeof(RectTransform));
            hand.transform.SetParent(transform, false);
            _handContainer = (RectTransform)hand.transform;
            SetupHandContainer();

            GameObject endTurn = new GameObject("EndTurn", typeof(RectTransform), typeof(Image), typeof(Button));
            endTurn.transform.SetParent(transform, false);
            endTurn.GetComponent<Image>().color = new Color(0.42f, 0.2f, 0.2f, 0.95f);
            SetupSidePanel((RectTransform)endTurn.transform, false);
            CreateLabel(endTurn.transform, "End Turn").alignment = TextAlignmentOptions.Center;

            _endTurnButton = endTurn.GetComponent<Button>();
            _endTurnButton.onClick.AddListener(() => BattleManager.Instance?.RequestEndTurn());
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
            _handContainer.anchoredPosition = Vector2.zero;
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
