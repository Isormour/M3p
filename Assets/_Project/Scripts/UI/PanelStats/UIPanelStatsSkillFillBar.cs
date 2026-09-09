using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The vertical track behind a stat's perk ladder. It shows how far the stat has climbed towards
    /// its cap by scaling a bottom-pivoted child, so the fill lines up with the perks on the same bar.
    /// </summary>
    public sealed class UIPanelStatsSkillFillBar : MonoBehaviour
    {
        [Tooltip("Bottom-pivoted child scaled between empty and full.")]
        [SerializeField] Transform _fill;

        /// <param name="normalized">Stat value over the stat cap, clamped to the bar.</param>
        public void SetFill(float normalized)
        {
            if (_fill == null)
                return;

            Vector3 scale = _fill.localScale;
            scale.y = Mathf.Clamp01(normalized);
            _fill.localScale = scale;
        }

        void OnValidate()
        {
            if (_fill == null)
                _fill = transform.Find("Fill");
        }
    }
}
