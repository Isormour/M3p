using UnityEngine;

[ExecuteAlways]
public class Rotator : MonoBehaviour
{
    public enum RotationSpace
    {
        Local = 0,
        World = 1
    }

    float currentTime = 990;
    [SerializeField] float duration = 1;
    [SerializeField] float amplitude = 1;
    [SerializeField] float rotationMult = 1;
    [SerializeField] RotationSpace space = RotationSpace.Local;

    Vector2 startTime;

    [SerializeField] AnimationCurve curve;

    public void StartRotate()
    {
        currentTime = 0;
        startTime = new Vector2(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
    }

    void Update()
    {
        if (currentTime >= duration)
            return;

        currentTime += Time.deltaTime;
        Vector3 fwd = Vector3.forward;

        Vector2 dir = new Vector2(
            Mathf.Sin(startTime.x + (currentTime * amplitude)) * rotationMult,
            Mathf.Cos(startTime.y + (currentTime * amplitude)) * rotationMult
        );
        float timeNormalized = currentTime / duration;
        float curveValue = curve.Evaluate(timeNormalized);

        fwd.x += curveValue * dir.x;
        fwd.y += curveValue * dir.y;

        if (currentTime > duration)
            fwd = Vector3.forward;

        ApplyForward(fwd);
    }

    void ApplyForward(Vector3 fwd)
    {
        if (space == RotationSpace.Local)
            transform.localRotation = Quaternion.LookRotation(fwd);
        else
            transform.forward = fwd;
    }
}
