using M3P;
using UnityEngine;

public class UIHealthIndicator : UISimpleIndicator
{
    [SerializeField] float _restingBeatsPerMinute = 50f;
    [SerializeField] float _criticalBeatsPerMinute = 160f;
    [SerializeField] float _restingPulse = 0.05f;
    [SerializeField] float _criticalPulse = 0.18f;
    [SerializeField] float _beatDuration = 0.38f;

    Vector3 _baseScale;
    float _beatTime;
    bool _hasBaseScale;

    void OnEnable()
    {
        _baseScale = transform.localScale;
        _hasBaseScale = true;
        _beatTime = 0f;
    }

    void OnDisable()
    {
        RestoreScale();
    }

    void Update()
    {
        if (!_hasBaseScale)
            return;

        int max = GetMaxValue();
        if (!HasValueBinding || max <= 0)
        {
            RestoreScale();
            return;
        }

        float health = Mathf.Clamp01((float)GetCurrentValue() / max);
        float bpm = Mathf.Lerp(_criticalBeatsPerMinute, _restingBeatsPerMinute, health);
        float period = 60f / Mathf.Max(1f, bpm);

        _beatTime += Time.deltaTime;
        if (_beatTime >= period)
            _beatTime %= period;

        float window = Mathf.Min(Mathf.Max(0.05f, _beatDuration), period);
        float shape = Heartbeat(Mathf.Clamp01(_beatTime / window));
        float amount = Mathf.Lerp(_criticalPulse, _restingPulse, health);
        transform.localScale = _baseScale * (1f + amount * shape);
    }

    void RestoreScale()
    {
        if (_hasBaseScale)
            transform.localScale = _baseScale;
    }

    static float Heartbeat(float t)
    {
        float lub = Gaussian(t, 0.22f, 0.09f);
        float dub = Gaussian(t, 0.55f, 0.07f) * 0.55f;
        return lub + dub;
    }

    static float Gaussian(float t, float center, float width)
    {
        float x = (t - center) / width;
        return Mathf.Exp(-x * x);
    }
}
