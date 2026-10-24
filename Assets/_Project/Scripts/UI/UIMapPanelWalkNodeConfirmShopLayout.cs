using TMPro;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Description preview inside the walk-to-node panel. Shown for forge, shop and chest nodes.
    /// </summary>
    public sealed class UIMapPanelWalkNodeConfirmShopLayout : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI labelDescription;

        public void Show(string description)
        {
            gameObject.SetActive(true);
            if (labelDescription != null)
                labelDescription.text = description ?? string.Empty;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
