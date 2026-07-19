using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPlayerPanelSkillsCostLabel : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _amountText;

        public void Configure(Sprite icon, int amount)
        {
            if (_icon == null || _amountText == null)
            {
                Debug.LogError($"{nameof(UIPlayerPanelSkillsCostLabel)}: assign {nameof(_icon)} and {nameof(_amountText)} on the prefab.", this);
                return;
            }

            _icon.sprite = icon != null
                ? icon
                : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _icon.enabled = true;
            _amountText.text = amount.ToString();
        }
    }
}
