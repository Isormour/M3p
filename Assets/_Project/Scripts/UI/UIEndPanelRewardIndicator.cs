using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One reward row on the end-of-battle panel: icon plus amount (EXP or a shard colour).
    /// </summary>
    public class UIEndPanelRewardIndicator : MonoBehaviour
    {
        [FormerlySerializedAs("Icon")]
        [SerializeField] Image _icon;

        [FormerlySerializedAs("labelAmount")]
        [SerializeField] TextMeshProUGUI _amountText;

        public void Configure(Sprite icon, int amount)
        {
            if (_icon == null || _amountText == null)
            {
                Debug.LogError($"{nameof(UIEndPanelRewardIndicator)}: assign icon and amount on the prefab.", this);
                return;
            }

            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _amountText.text = amount.ToString();
        }
    }
}
