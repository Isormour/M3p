using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIHPIndicator : MonoBehaviour
    {
        [SerializeField] Image _fill;
        [SerializeField] TextMeshProUGUI _healthText;

        SoftStats _boundStats;
        int _maxHealth = 1;

        public void Bind(SoftStats softStats, int maxHealth)
        {
            Unbind();

            _boundStats = softStats;
            _maxHealth = Mathf.Max(1, maxHealth);

            if (_boundStats != null)
                _boundStats.Changed += HandleStatsChanged;

            Refresh();
        }

        public void Unbind()
        {
            if (_boundStats != null)
                _boundStats.Changed -= HandleStatsChanged;

            _boundStats = null;
            _maxHealth = 1;
            Refresh();
        }

        void HandleStatsChanged()
        {
            Refresh();
        }

        void Refresh()
        {
            int currentHealth = _boundStats != null ? _boundStats.CurrentHealth : 0;

            if (_fill != null)
                _fill.fillAmount = _maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / _maxHealth) : 0f;

            if (_healthText != null)
                _healthText.text = $"{currentHealth}/{_maxHealth}";
        }
    }
}
