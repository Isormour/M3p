using M3P;
using UnityEngine;

public class MapCamera : MonoBehaviour
{
    Transform currentTarget;
    Transform oldTarget;
    float lerpTime = 1f;
    Vector3 startPos;
    Quaternion startRot;
    bool startCaptured;
    bool returnToStart;
    [SerializeField] AnimationCurve lerpCurve;

    void Update()
    {
        if (lerpTime >= 1f)
            return;

        if (currentTarget == null && !returnToStart)
            return;

        lerpTime += Time.deltaTime;
        float t = lerpCurve.Evaluate(Mathf.Clamp01(lerpTime));

        Vector3 fromPos = oldTarget != null ? oldTarget.position : startPos;
        Quaternion fromRot = oldTarget != null ? oldTarget.rotation : startRot;
        Vector3 toPos = returnToStart ? startPos : currentTarget.position;
        Quaternion toRot = returnToStart ? startRot : currentTarget.rotation;

        transform.position = Vector3.Lerp(fromPos, toPos, t);
        transform.rotation = Quaternion.Lerp(fromRot, toRot, t);
    }

    public void SetTarget(MapNode newTarget)
    {
        if (newTarget == null || newTarget.camTarget == null)
            return;

        CaptureStartIfNeeded();

        oldTarget = returnToStart ? null : currentTarget;
        currentTarget = newTarget.camTarget;
        returnToStart = false;
        lerpTime = 0f;
    }

    /// <summary>Lerps back to the pose before the last <see cref="SetTarget"/>.</summary>
    public void RestorePrevious()
    {
        if (currentTarget == null && !returnToStart)
            return;

        Transform previous = oldTarget;
        oldTarget = returnToStart ? null : currentTarget;
        currentTarget = previous;
        returnToStart = previous == null;
        lerpTime = 0f;
    }

    void CaptureStartIfNeeded()
    {
        if (startCaptured)
            return;

        startPos = transform.position;
        startRot = transform.rotation;
        startCaptured = true;
    }
}
