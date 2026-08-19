using System.Collections;
using TMPro;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// World-space combo label that stays up for a short time, then fades. Showing it again
    /// restarts the timer so a longer cascade or a bigger match keeps the same instance alive.
    /// </summary>
    public class BattleIndicator : MonoBehaviour
    {
        [SerializeField] protected TextMeshPro _tittleText;
        [SerializeField] protected TextMeshPro _amountText;
        [SerializeField] ParticleSystem _particle;

        [Header("Lifetime")]
        [SerializeField] float _lifetime = 2f;
        [SerializeField] float _fadeOutDuration = 0.4f;

        [Header("Pulse")]
        [SerializeField] float _pulseScale = 1.35f;
        [SerializeField] float _pulseDuration = 0.22f;
        [SerializeField] AnimationCurve _pulseCurve;
        [SerializeField] bool _faceCamera;

        Vector3 _titleBaseScale;
        Vector3 _amountBaseScale;
        Color _titleColor;
        Color _amountColor;
        Coroutine _lifetimeRoutine;
        Coroutine _pulseRoutine;
        int _currentAmount;
        bool _isShowing;
        bool _cachedVisuals;

        public int CurrentAmount => _currentAmount;

        public bool IsShowing => _isShowing && isActiveAndEnabled;

        protected virtual void Awake()
        {
            CacheVisuals();
            SetFade(0f);
        }

        /// <summary>
        /// Writes the amount, plays the burst and starts a fresh lifetime without moving the object.
        /// </summary>
        public void Present(int amount)
        {
            CacheVisuals();
            _currentAmount = amount;
            _isShowing = true;
            gameObject.SetActive(true);
            ApplyAmountText();
            SetFade(1f);
            SetPulseScale(0f);
            PlayBurst();
            Pulse();
            RestartLifetime();
        }

        /// <summary>Scale punch used when a new combo lands and when the matching attack hits.</summary>
        public void Pulse()
        {
            if (!_isShowing || !isActiveAndEnabled)
                return;

            if (_pulseRoutine != null)
                StopCoroutine(_pulseRoutine);

            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        public void Hide()
        {
            StopOwnedRoutines();
            _isShowing = false;
            SetFade(0f);
            SetPulseScale(0f);

            if (_particle != null)
                _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (!_faceCamera || !_isShowing)
                return;

            Camera cameraRef = Camera.main;
            if (cameraRef == null)
                return;

            transform.rotation = cameraRef.transform.rotation;
        }

        void CacheVisuals()
        {
            if (_cachedVisuals)
                return;

            _titleBaseScale = _tittleText != null ? _tittleText.transform.localScale : Vector3.one;
            _amountBaseScale = _amountText != null ? _amountText.transform.localScale : Vector3.one;
            _titleColor = _tittleText != null ? _tittleText.color : Color.white;
            _amountColor = _amountText != null ? _amountText.color : Color.white;
            _cachedVisuals = true;
        }

        void ApplyAmountText()
        {
            if (_amountText != null)
                _amountText.text = _currentAmount.ToString();
        }

        protected void SetTitle(string title)
        {
            if (_tittleText != null && !string.IsNullOrEmpty(title))
                _tittleText.text = title;
        }

        void PlayBurst()
        {
            if (_particle == null)
                return;

            _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particle.Play(true);
        }

        void RestartLifetime()
        {
            if (_lifetimeRoutine != null)
                StopCoroutine(_lifetimeRoutine);

            _lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }

        IEnumerator LifetimeRoutine()
        {
            float visibleDuration = Mathf.Max(0f, _lifetime);
            if (visibleDuration > 0f)
                yield return new WaitForSeconds(visibleDuration);

            float fadeDuration = Mathf.Max(0.01f, _fadeOutDuration);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetFade(1f - Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            _lifetimeRoutine = null;
            Hide();
        }

        IEnumerator PulseRoutine()
        {
            float duration = Mathf.Max(0.01f, _pulseDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetPulseScale(EvaluatePulse(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetPulseScale(0f);
            _pulseRoutine = null;
        }

        float EvaluatePulse(float normalizedTime)
        {
            if (_pulseCurve != null && _pulseCurve.length > 0)
                return _pulseCurve.Evaluate(normalizedTime);

            return Mathf.Sin(normalizedTime * Mathf.PI);
        }

        void SetPulseScale(float weight)
        {
            float scale = Mathf.LerpUnclamped(1f, _pulseScale, weight);
            if (_tittleText != null)
                _tittleText.transform.localScale = _titleBaseScale * scale;
            if (_amountText != null)
                _amountText.transform.localScale = _amountBaseScale * scale;
        }

        void SetFade(float alpha)
        {
            if (_tittleText != null)
            {
                Color color = _titleColor;
                color.a = _titleColor.a * alpha;
                _tittleText.color = color;
            }

            if (_amountText != null)
            {
                Color color = _amountColor;
                color.a = _amountColor.a * alpha;
                _amountText.color = color;
            }
        }

        void StopOwnedRoutines()
        {
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
                _lifetimeRoutine = null;
            }

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }
        }

        void OnDisable()
        {
            StopOwnedRoutines();
        }
    }
}
