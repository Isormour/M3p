using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace M3P
{
    public class UISimpleIndicator : MonoBehaviour
    {
        [SerializeField] protected Image _fill;

        [FormerlySerializedAs("_healthText")]
        [SerializeField] protected TextMeshProUGUI _valueText;

        [SerializeField] ParticleSystem _increaseParticles;
        [SerializeField] ParticleSystem _decreaseParticles;
        [SerializeField] Rotator _rotation;

        Func<int> _getCurrent;
        Func<int> _getMax;
        Func<int, int, string> _formatText;
        Action<Action> _subscribeChanged;
        Action<Action> _unsubscribeChanged;
        int _previousValue = -1;
        bool _hasPreviousValue;

        //TODO: fix same value execution
        public void Bind(
            Func<int> getCurrent,
            Func<int> getMax,
            Action<Action> subscribeChanged = null,
            Action<Action> unsubscribeChanged = null,
            Func<int, int, string> formatText = null)
        {
            Unbind();

            _getCurrent = getCurrent;
            _getMax = getMax;
            _formatText = formatText;
            _subscribeChanged = subscribeChanged;
            _unsubscribeChanged = unsubscribeChanged;

            _subscribeChanged?.Invoke(HandleStatsChanged);

            ResetValueTracking();
            RefreshDisplay(playParticles: false);
        }

        public virtual void Unbind()
        {
            _unsubscribeChanged?.Invoke(HandleStatsChanged);

            _getCurrent = null;
            _getMax = null;
            _formatText = null;
            _subscribeChanged = null;
            _unsubscribeChanged = null;
            ResetValueTracking();
            RefreshDisplay(playParticles: false);
        }

        public void ManualRefresh()
        {
            HandleStatsChanged();
        }

        public void SetIcon(Sprite icon)
        {
            if (_fill == null)
                return;

            _fill.sprite = icon;
            _fill.enabled = icon != null;
        }

        void HandleStatsChanged()
        {
            int newValue = GetCurrentValue();
            
            if (_hasPreviousValue && newValue == _previousValue)
            {
                return;
            }
            
            if (_rotation != null)
                _rotation.StartRotate();

            RefreshDisplay(playParticles: true);
        }

        int GetCurrentValue() => _getCurrent != null ? _getCurrent() : 0;

        int GetMaxValue() => _getMax != null ? _getMax() : 0;

        void ResetValueTracking()
        {
            _hasPreviousValue = false;
            _previousValue = -1;
        }

        void RefreshDisplay(bool playParticles)
        {
            int current = GetCurrentValue();
            int max = GetMaxValue();

            if (_fill != null)
                _fill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            if (_valueText != null)
            {
                _valueText.text = _formatText != null 
                    ? _formatText(current, max) 
                    : $"{current}/{max}";
            }

            if (playParticles && _hasPreviousValue && current != _previousValue)
            {
                if (current > _previousValue)
                    PlayParticles(_increaseParticles);
                else
                    PlayParticles(_decreaseParticles);
            }

            _previousValue = current;
            _hasPreviousValue = true;
        }

        static void PlayParticles(ParticleSystem particles)
        {
            if (particles == null)
                return;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play();
        }

        void OnDestroy()
        {
            Unbind();
        }
    }
}
