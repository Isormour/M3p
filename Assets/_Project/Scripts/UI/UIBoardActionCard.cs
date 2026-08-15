using System;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIBoardActionCard : MonoBehaviour
    {
        const float SelectedScale = 1.2f;

        [SerializeField] Button _button;
        [SerializeField] UICardVisuals _visuals;

        BoardActionCardDefinition _card;
        CardPlayController _controller;
        Action _clicked;
        int _handIndex = -1;

        public BoardActionCardDefinition Card => _card;
        public int HandIndex => _handIndex;

        void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_visuals == null)
                _visuals = GetComponentInChildren<UICardVisuals>(true);

            if (_button == null)
            {
                Debug.LogError($"{nameof(UIBoardActionCard)}: assign {nameof(_button)} or add a {nameof(Button)} component.", this);
                return;
            }

            _button.onClick.AddListener(HandleClick);
        }

        public void Configure(BoardActionCardDefinition card, CardPlayController controller, int handIndex)
        {
            _clicked = null;
            ApplyCard(card, controller, handIndex);
            SetFrameMaskEnabled(false);
        }

        public void Configure(BoardActionCardDefinition card, Action clicked)
        {
            _clicked = clicked;
            ApplyCard(card, null, -1);
        }

        void ApplyCard(BoardActionCardDefinition card, CardPlayController controller, int handIndex)
        {
            _card = card;
            _controller = controller;
            _handIndex = handIndex;
            SetSelected(false);

            if (_visuals == null)
                _visuals = GetComponentInChildren<UICardVisuals>(true);

            if (_visuals != null)
                _visuals.SetCardData(card);
            else
                Debug.LogError($"{nameof(UIBoardActionCard)}: assign {nameof(_visuals)} on the prefab.", this);
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        public void SetFrameMaskEnabled(bool enabled)
        {
            if (_visuals == null)
                _visuals = GetComponentInChildren<UICardVisuals>(true);

            if (_visuals != null)
                _visuals.SetFrameMaskEnabled(enabled);
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
        }

        void HandleClick()
        {
            if (_clicked != null)
            {
                _clicked.Invoke();
                return;
            }

            if (_card == null || _controller == null)
                return;

            _controller.SelectCardAt(_handIndex);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
