using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>One row in the properties column: icon, name and the value the profile currently has.</summary>
    public sealed class UIPanelStatsPropertyBar : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _nameLabel;
        [SerializeField] TextMeshProUGUI _valueLabel;

        LayoutElement _iconLayout;
        LayoutElement _nameLayout;
        LayoutElement _valueLayout;

        void Awake()
        {
            ResolveRefs();
            ConfigureLayout();
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        public void Show(PlayerProperty property, int value, Sprite icon)
        {
            ResolveRefs();

            if (_nameLabel != null)
            {
                _nameLabel.text = PlayerProperties.DisplayName(property);
                _nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }

            if (_valueLabel != null)
            {
                _valueLabel.text = value.ToString();
                _valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            }

            bool showIcon = icon != null;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.preserveAspect = true;
                _icon.raycastTarget = false;
                _icon.enabled = showIcon;
                if (!_icon.gameObject.activeSelf)
                    _icon.gameObject.SetActive(true);
            }

            if (_iconLayout == null && _icon != null)
                _iconLayout = GetOrAddLayout(_icon.gameObject);

            if (_iconLayout != null)
            {
                float width = showIcon ? 36f : 0f;
                _iconLayout.minWidth = width;
                _iconLayout.preferredWidth = width;
                _iconLayout.preferredHeight = 36f;
                _iconLayout.flexibleWidth = 0f;
            }

            if (transform is RectTransform row)
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }

        void ResolveRefs()
        {
            if (_icon == null)
            {
                Transform icon = transform.Find("Image");
                if (icon != null)
                    _icon = icon.GetComponent<Image>();
            }

            if (_nameLabel == null)
                _nameLabel = FindLabel("PropertyName");

            if (_valueLabel == null)
                _valueLabel = FindLabel("PropertyValue");
        }

        TextMeshProUGUI FindLabel(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        void ConfigureLayout()
        {
            LayoutElement row = GetOrAddLayout(gameObject);
            row.flexibleWidth = 1f;
            row.flexibleHeight = 1f;
            row.minHeight = 36f;

            if (_icon != null)
            {
                _iconLayout = GetOrAddLayout(_icon.gameObject);
                _iconLayout.preferredWidth = 0f;
                _iconLayout.preferredHeight = 36f;
                _iconLayout.flexibleWidth = 0f;
            }

            if (_nameLabel != null)
            {
                _nameLayout = GetOrAddLayout(_nameLabel.gameObject);
                _nameLayout.flexibleWidth = 1f;
                _nameLayout.minWidth = 80f;
            }

            if (_valueLabel != null)
            {
                _valueLayout = GetOrAddLayout(_valueLabel.gameObject);
                _valueLayout.preferredWidth = 64f;
                _valueLayout.flexibleWidth = 0f;
            }
        }

        static LayoutElement GetOrAddLayout(GameObject target)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
                layout = target.AddComponent<LayoutElement>();

            return layout;
        }
    }
}
