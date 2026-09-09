using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One perk marker on a stat's ladder. It sits at the height its threshold occupies on the bar
    /// and is lit once the stat reaches that threshold, dark while it is still out of reach.
    /// </summary>
    public sealed class UIPanelStatsPerk : MonoBehaviour
    {
        [SerializeField] Image _perkIcon;
        [Tooltip("Ring drawn around the icon; tinted along with it.")]
        [SerializeField] Image _frame;

        [SerializeField] Color _unlockedTint = Color.white;
        [SerializeField] Color _lockedTint = new Color(0.22f, 0.22f, 0.26f, 1f);

        /// <summary>Stat value this perk needs, so the ladder can re-light itself without the config.</summary>
        public int StatLevel { get; private set; }

        /// <param name="heightRatio">Threshold over the stat cap: where the marker sits on the bar.</param>
        public void Bind(PerkDefinition perk, int statLevel, float heightRatio)
        {
            StatLevel = statLevel;

            // An unauthored artwork keeps the prefab's placeholder rather than blanking the marker.
            if (_perkIcon != null && perk != null && perk.Artwork != null)
                _perkIcon.sprite = perk.Artwork;

            RectTransform rect = (RectTransform)transform;
            float height = Mathf.Clamp01(heightRatio);
            rect.anchorMin = new Vector2(0.5f, height);
            rect.anchorMax = new Vector2(0.5f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        public void SetUnlocked(bool unlocked)
        {
            Color tint = unlocked ? _unlockedTint : _lockedTint;

            if (_perkIcon != null)
                _perkIcon.color = tint;

            if (_frame != null)
                _frame.color = tint;
        }

        void OnValidate()
        {
            if (_frame == null)
            {
                Transform border = transform.Find("Border");
                if (border != null)
                    _frame = border.GetComponent<Image>();
            }
        }
    }
}
