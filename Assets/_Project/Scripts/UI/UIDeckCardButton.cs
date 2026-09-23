using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIDeckCardButton : MonoBehaviour
    {
        [SerializeField] Image cardIcon;
        [SerializeField] TextMeshProUGUI cardName;
        [SerializeField] TextMeshProUGUI cardCount;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;
        [SerializeField] Button _button;

        BoardActionCardDefinition _card;
        Action _clicked;
        Action _removeClicked;
        Action _addClicked;

        public BoardActionCardDefinition Card => _card;

        void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(HandleClick);

            if (minusButton != null)
                minusButton.onClick.AddListener(HandleRemove);
            if (plusButton != null)
                plusButton.onClick.AddListener(HandleAdd);
        }

        public void Configure(BoardActionCardDefinition card, Action clicked)
        {
            Configure(card, 1, false, clicked, null);
        }

        public void Configure(BoardActionCardDefinition card, int count, bool canAdd, Action removeClicked, Action addClicked)
        {
            _card = card;
            _clicked = null;
            _removeClicked = removeClicked;
            _addClicked = addClicked;

            if (cardName != null)
            {
                cardName.text = card != null ? card.DisplayName : string.Empty;
                cardName.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (cardCount != null)
                cardCount.text = count.ToString();

            if (cardIcon != null)
            {
                Sprite artwork = card != null ? card.Artwork : null;
                cardIcon.sprite = artwork;
                cardIcon.enabled = artwork != null;
            }

            if (plusButton != null)
                plusButton.interactable = canAdd;

            if (minusButton != null)
                minusButton.interactable = count > 0;

            UITooltipTrigger.Ensure(gameObject, () => TooltipBuilder.FromCard(_card));
        }

        void HandleClick()
        {
            _clicked?.Invoke();
        }

        void HandleRemove()
        {
            _removeClicked?.Invoke();
        }

        void HandleAdd()
        {
            _addClicked?.Invoke();
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
            if (minusButton != null)
                minusButton.onClick.RemoveListener(HandleRemove);
            if (plusButton != null)
                plusButton.onClick.RemoveListener(HandleAdd);
        }
    }
}
