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
        [SerializeField] Button _button;

        BoardActionCardDefinition _card;
        Action _clicked;

        public BoardActionCardDefinition Card => _card;

        void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
            {
                Debug.LogError($"{nameof(UIDeckCardButton)}: add a {nameof(Button)} component.", this);
                return;
            }

            _button.onClick.AddListener(HandleClick);
        }

        public void Configure(BoardActionCardDefinition card, Action clicked)
        {
            _card = card;
            _clicked = clicked;

            if (cardName != null)
                cardName.text = card != null ? card.DisplayName : string.Empty;

            if (cardIcon != null)
            {
                Sprite artwork = card != null ? card.Artwork : null;
                cardIcon.sprite = artwork;
                cardIcon.enabled = artwork != null;
            }

            UITooltipTrigger.Ensure(gameObject, () => TooltipBuilder.FromCard(_card));
        }

        void HandleClick()
        {
            _clicked?.Invoke();
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
