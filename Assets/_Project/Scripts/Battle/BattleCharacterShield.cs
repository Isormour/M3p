using System.Collections;
using UnityEngine;

public class BattleCharacterShield : MonoBehaviour
{
    static readonly int MultAndPowId = Shader.PropertyToID("_MultAndPow");

    [SerializeField] MeshRenderer[] spheres;
    [SerializeField] float _visiblePower = 1f;
    [SerializeField] float _hitPower = 6f;
    [SerializeField] float _hitPulseDuration = 0.22f;
    [SerializeField] AnimationCurve _hitPulseCurve;
    [SerializeField] float _fadeDuration = 0.25f;
    [SerializeField] AnimationCurve _appearScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] AnimationCurve _disappearScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    MaterialPropertyBlock _block;
    Vector3 _baseScale = Vector3.one;
    float _multX;
    float _currentPower;
    bool _visible;
    Coroutine _pulseRoutine;
    Coroutine _fadeRoutine;
    float _fadeTarget;
    M3P.SoftStats _boundStats;

    public bool IsVisible => _visible;

    void Awake()
    {
        _baseScale = transform.localScale;
        CacheMultX();
        ApplyPower(0f);
        ApplyScale(0f);
    }

    void OnEnable()
    {
        RefreshFromStats();
    }

    void OnDisable()
    {
        StopPulse();
        StopFade();
        ApplyPower(0f);
        ApplyScale(0f);
    }

    void OnDestroy()
    {
        Unbind();
    }

    public void Bind(M3P.SoftStats stats)
    {
        if (_boundStats != null)
            _boundStats.Changed -= HandleStatsChanged;

        _boundStats = stats;

        if (_boundStats != null)
            _boundStats.Changed += HandleStatsChanged;

        RefreshFromStats();
    }

    public void Unbind()
    {
        if (_boundStats != null)
            _boundStats.Changed -= HandleStatsChanged;

        _boundStats = null;
        SetVisible(false);
    }

    public void Pulse()
    {
        if (!isActiveAndEnabled || spheres == null || spheres.Length == 0)
            return;

        StopPulse();
        StopFade();
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    void HandleStatsChanged()
    {
        RefreshFromStats();
    }

    void RefreshFromStats()
    {
        SetVisible(_boundStats != null && _boundStats.CurrentShield > 0);
    }

    void SetVisible(bool visible)
    {
        _visible = visible;
        if (_pulseRoutine != null)
            return;

        FadeTo(visible ? _visiblePower : 0f);
    }

    void FadeTo(float target)
    {
        if (!isActiveAndEnabled)
        {
            StopFade();
            ApplyPower(target);
            ApplyScale(target > 0f ? 1f : 0f);
            return;
        }

        if (_fadeRoutine != null && Mathf.Approximately(_fadeTarget, target))
            return;

        if (_fadeRoutine == null && Mathf.Approximately(_currentPower, target))
            return;

        StopFade();
        _fadeTarget = target;
        _fadeRoutine = StartCoroutine(FadeRoutine(target, target > _currentPower));
    }

    IEnumerator FadeRoutine(float target, bool appearing)
    {
        float start = _currentPower;
        float duration = Mathf.Max(0.01f, _fadeDuration);
        float elapsed = 0f;
        AnimationCurve scaleCurve = appearing ? _appearScaleCurve : _disappearScaleCurve;
        float fallbackStart = appearing ? 0f : 1f;
        float fallbackEnd = appearing ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float t = Mathf.SmoothStep(0f, 1f, normalized);
            ApplyPower(Mathf.Lerp(start, target, t));
            ApplyScale(EvaluateScale(scaleCurve, normalized, fallbackStart, fallbackEnd));
            yield return null;
        }

        ApplyPower(target);
        ApplyScale(EvaluateScale(scaleCurve, 1f, fallbackStart, fallbackEnd));
        _fadeRoutine = null;
    }

    IEnumerator PulseRoutine()
    {
        float duration = Mathf.Max(0.01f, _hitPulseDuration);
        float elapsed = 0f;
        float peak = Mathf.Max(_hitPower, _visiblePower);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float weight = EvaluatePulse(Mathf.Clamp01(elapsed / duration));
            float rest = _visible ? _visiblePower : 0f;
            ApplyPower(Mathf.LerpUnclamped(rest, peak, weight));
            yield return null;
        }

        _pulseRoutine = null;
        ApplyPower(_visible ? _visiblePower : 0f);
        ApplyScale(_visible ? 1f : 0f);
    }

    float EvaluatePulse(float normalizedTime)
    {
        if (_hitPulseCurve != null && _hitPulseCurve.length > 0)
            return _hitPulseCurve.Evaluate(normalizedTime);

        return Mathf.Sin(normalizedTime * Mathf.PI);
    }

    static float EvaluateScale(AnimationCurve curve, float normalizedTime, float fallbackStart, float fallbackEnd)
    {
        if (curve != null && curve.length > 0)
            return curve.Evaluate(normalizedTime);

        return Mathf.Lerp(fallbackStart, fallbackEnd, normalizedTime);
    }

    void CacheMultX()
    {
        if (spheres == null)
            return;

        for (int i = 0; i < spheres.Length; i++)
        {
            MeshRenderer renderer = spheres[i];
            if (renderer == null)
                continue;

            Material material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty(MultAndPowId))
                continue;

            Vector4 current = material.GetVector(MultAndPowId);
            _multX = current.x;
            if (_visiblePower <= 0f && current.y > 0f)
                _visiblePower = current.y;
            return;
        }
    }

    void ApplyPower(float powerY)
    {
        if (spheres == null)
            return;

        if (_block == null)
            _block = new MaterialPropertyBlock();

        _currentPower = Mathf.Max(0f, powerY);
        Vector4 value = new Vector4(_multX, _currentPower, 0f, 0f);
        for (int i = 0; i < spheres.Length; i++)
        {
            MeshRenderer renderer = spheres[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_block);
            _block.SetVector(MultAndPowId, value);
            renderer.SetPropertyBlock(_block);
        }
    }

    void ApplyScale(float multiplier)
    {
        transform.localScale = _baseScale * Mathf.Max(0f, multiplier);
    }

    void StopPulse()
    {
        if (_pulseRoutine == null)
            return;

        StopCoroutine(_pulseRoutine);
        _pulseRoutine = null;
    }

    void StopFade()
    {
        if (_fadeRoutine == null)
            return;

        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }
}
