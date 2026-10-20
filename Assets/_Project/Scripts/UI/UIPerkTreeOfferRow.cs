using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>One threshold on an unlock offer: icon, the level that grants it, and the perk's short effect.</summary>
    public sealed class UIPerkTreeOfferRow : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _threshold;
        [SerializeField] TextMeshProUGUI _effect;
        [SerializeField] GameObject _link;

        public void Bind(PerkDefinition perk, int statLevel, string thresholdFormat)
        {
            if (_icon != null)
            {
                _icon.sprite = perk != null ? perk.Artwork : null;
                _icon.enabled = _icon.sprite != null;
                _icon.preserveAspect = true;
            }

            if (_threshold != null)
                _threshold.text = string.Format(string.IsNullOrEmpty(thresholdFormat) ? "Próg {0}" : thresholdFormat, statLevel);

            if (_effect != null)
                _effect.text = perk != null ? perk.OfferText : string.Empty;
        }

        /// <summary>The line that runs down to the next threshold. The last row has nothing below it.</summary>
        public void SetContinue(bool visible)
        {
            if (_link != null)
                _link.SetActive(visible);
        }
    }
}
