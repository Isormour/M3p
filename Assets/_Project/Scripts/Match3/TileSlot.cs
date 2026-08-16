using UnityEngine;

namespace Match3
{
    internal class TileSlot : MonoBehaviour
    {
        [SerializeField] SpriteRenderer upgrade;

        public void Show(Sprite icon)
        {
            gameObject.SetActive(true);
            if (upgrade == null)
                return;

            upgrade.sprite = icon;
            upgrade.enabled = icon != null;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
