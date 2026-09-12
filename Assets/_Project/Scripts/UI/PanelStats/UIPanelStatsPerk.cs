using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One perk marker on a stat's ladder. It sits beside the fill bar at the height its threshold
    /// occupies and is lit once the stat reaches that threshold, dark while it is still out of reach.
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

        /// <summary>Normalized height on the bar, used when the ladder picks a side.</summary>
        public float HeightRatio { get; private set; }

        /// <param name="heightRatio">Threshold over the stat cap: where the marker sits on the bar.</param>
        public void Bind(PerkDefinition perk, int statLevel, float heightRatio)
        {
            StatLevel = statLevel;
            HeightRatio = Mathf.Clamp01(heightRatio);

            // An unauthored artwork keeps the prefab's placeholder rather than blanking the marker.
            if (_perkIcon != null && perk != null && perk.Artwork != null)
                _perkIcon.sprite = perk.Artwork;
        }

        /// <summary>Pins the icon beside the fill bar at this perk's threshold height.</summary>
        public void Place(bool onRight, float sideInset)
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, HeightRatio);
            rect.anchorMax = new Vector2(0.5f, HeightRatio);
            rect.pivot = new Vector2(onRight ? 0f : 1f, 0.5f);
            rect.anchoredPosition = new Vector2(onRight ? sideInset : -sideInset, 0f);
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
