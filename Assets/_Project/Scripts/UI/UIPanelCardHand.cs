using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Prototype hand UI: one button per card, an action point readout and an end turn button.
    /// Builds its own hierarchy so it can be dropped straight onto a canvas without prefab wiring.
    /// </summary>
    public sealed class UIPanelCardHand : MonoBehaviour
    {
        static readonly Color CardIdleColor = new Color(0.16f, 0.17f, 0.22f, 0.95f);
        static readonly Color CardSelectedColor = new Color(0.32f, 0.46f, 0.72f, 1f);

        [SerializeField] float _cardWidth = 130f;
        [SerializeField] float _cardHeight = 96f;

        readonly List<GameObject> _cardButtons = new List<GameObject>();

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
            ClearCardButtons();
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
                        _cardPlay.Changed += Refresh;

                    Refresh();
                }

                RefreshInteractable();
                yield return null;
            }
        }

        void Unbind()
        {
            if (_cardPlay != null)
                _cardPlay.Changed -= Refresh;

            _cardPlay = null;
        }

        void Refresh()
        {
            ClearCardButtons();

            if (_cardPlay == null)
                return;

            IReadOnlyList<BoardActionCardDefinition> hand = _cardPlay.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                BoardActionCardDefinition card = hand[i];
                if (card == null)
                    continue;

                CreateCardButton(card);
            }

            RefreshInteractable();
        }

        void CreateCardButton(BoardActionCardDefinition card)
        {
            GameObject root = new GameObject($"Card_{card.name}", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(_handContainer, false);

            Image background = root.GetComponent<Image>();
            background.color = _cardPlay.SelectedCard == card ? CardSelectedColor : CardIdleColor;

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = _cardWidth;
            layout.preferredHeight = _cardHeight;

            TextMeshProUGUI label = CreateLabel(root.transform, $"{card.DisplayName}\n<size=80%>{card.ActionPointCost} AP");
            label.alignment = TextAlignmentOptions.Center;

            Button button = root.GetComponent<Button>();
            BoardActionCardDefinition captured = card;
            button.onClick.AddListener(() => _cardPlay?.SelectCard(captured));

            _cardButtons.Add(root);
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
            int count = Mathf.Min(hand.Count, _cardButtons.Count);

            for (int i = 0; i < count; i++)
            {
                GameObject buttonRoot = _cardButtons[i];
                if (buttonRoot == null)
                    continue;

                BoardActionCardDefinition card = hand[i];
                buttonRoot.GetComponent<Button>().interactable = playerActing && _cardPlay.CanPlay(card);
                buttonRoot.GetComponent<Image>().color = _cardPlay.SelectedCard == card ? CardSelectedColor : CardIdleColor;
            }
        }

        void ClearCardButtons()
        {
            for (int i = 0; i < _cardButtons.Count; i++)
            {
                if (_cardButtons[i] != null)
                    Destroy(_cardButtons[i]);
            }

            _cardButtons.Clear();
        }

        void EnsureLayout()
        {
            if (_handContainer != null)
                return;

            HorizontalLayoutGroup rootLayout = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (rootLayout == null)
                rootLayout = gameObject.AddComponent<HorizontalLayoutGroup>();

            rootLayout.spacing = 12f;
            rootLayout.childAlignment = TextAnchor.MiddleCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            GameObject actionPoints = new GameObject("ActionPoints", typeof(RectTransform));
            actionPoints.transform.SetParent(transform, false);
            AddSize(actionPoints, 110f, _cardHeight);
            _actionPointsLabel = CreateLabel(actionPoints.transform, "AP -");
            _actionPointsLabel.alignment = TextAlignmentOptions.Center;

            GameObject hand = new GameObject("Hand", typeof(RectTransform));
            hand.transform.SetParent(transform, false);
            _handContainer = (RectTransform)hand.transform;

            HorizontalLayoutGroup handLayout = hand.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 8f;
            handLayout.childAlignment = TextAnchor.MiddleCenter;
            handLayout.childControlWidth = true;
            handLayout.childControlHeight = true;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            hand.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject endTurn = new GameObject("EndTurn", typeof(RectTransform), typeof(Image), typeof(Button));
            endTurn.transform.SetParent(transform, false);
            endTurn.GetComponent<Image>().color = new Color(0.42f, 0.2f, 0.2f, 0.95f);
            AddSize(endTurn, 110f, _cardHeight);
            CreateLabel(endTurn.transform, "End Turn").alignment = TextAlignmentOptions.Center;

            _endTurnButton = endTurn.GetComponent<Button>();
            _endTurnButton.onClick.AddListener(() => BattleManager.Instance?.RequestEndTurn());
        }

        static void AddSize(GameObject target, float width, float height)
        {
            LayoutElement layout = target.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
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
