using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelPlayerManaBar : MonoBehaviour
    {
        [SerializeField] Image _manaIcon;
        [SerializeField] TextMeshProUGUI _amountText;

        int _tileTypeId;

        public int TileTypeId => _tileTypeId;

        public void Configure(int tileTypeId, Sprite icon)
        {
            _tileTypeId = tileTypeId;

            if (_manaIcon == null || _amountText == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerManaBar)}: assign {nameof(_manaIcon)} and {nameof(_amountText)} on the prefab.", this);
                return;
            }

            _manaIcon.sprite = icon != null
                ? icon
                : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _manaIcon.enabled = true;

            SetAmount(0);
        }

        public void SetAmount(int amount)
        {
            if (_amountText == null)
                return;

            _amountText.text = amount.ToString();
        }
    }
}
