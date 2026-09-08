using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UICharacterStatusIndicator : MonoBehaviour
    {
        [SerializeField] Image _icon;

        void OnValidate()
        {
            if (_icon == null)
                _icon = GetComponentInChildren<Image>(true);
        }

        public void Configure(StatusInstance status)
        {
            Sprite icon = status != null && status.Definition != null
                ? status.Definition.Icon
                : null;
            SetIcon(icon);
        }

        public void SetIcon(Sprite icon)
        {
            if (_icon == null)
                _icon = GetComponentInChildren<Image>(true);

            if (_icon == null)
                return;

            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }
    }
}
