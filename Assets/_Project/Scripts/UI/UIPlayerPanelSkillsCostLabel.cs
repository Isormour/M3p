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
            Configure(icon, amount.ToString());
        }

        public void ConfigureDistinctColors(int colorCount, int manaPerColor)
        {
            Configure(null, $"{manaPerColor}×{colorCount} colors");
        }

        void Configure(Sprite icon, string text)
        {
            if (_icon == null || _amountText == null)
            {
                Debug.LogError($"{nameof(UIPlayerPanelSkillsCostLabel)}: assign {nameof(_icon)} and {nameof(_amountText)} on the prefab.", this);
                return;
            }

            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.gameObject.SetActive(icon != null);
            _amountText.text = text;
            _amountText.textWrappingMode = TextWrappingModes.NoWrap;

            float textWidth = Mathf.Max(24f, Mathf.Ceil(_amountText.GetPreferredValues(text).x));
            _amountText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);

            float width = textWidth;
            if (icon != null)
                width += _icon.rectTransform.rect.width;

            HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
                width += layout.padding.horizontal + (icon != null ? layout.spacing : 0f);

            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
