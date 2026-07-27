using UnityEngine;

[ExecuteAlways]
public class Rotator : MonoBehaviour
{
    float currentTime = 990;
    [SerializeField] float duration = 1;
    [SerializeField] float amplitude = 1;
    [SerializeField] float rotationMult = 1;

    Vector2 startTime;

    [SerializeField] AnimationCurve curve;

    public void StartRotate()
    {
        currentTime = 0;
        startTime = new Vector2(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
    }
    // Update is called once per frame
    void Update()
    {
        if (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            Vector3 fwd = new Vector3(0, 0, 1);

            Vector2 dir = new Vector2(
                 Mathf.Sin(startTime.x + (currentTime * amplitude)) * rotationMult,
                 Mathf.Cos(startTime.y + (currentTime * amplitude)) * rotationMult
                );
            float timeNormalized = currentTime / duration;

            fwd.x += curve.Evaluate(timeNormalized) * dir.x;
            fwd.y += curve.Evaluate(timeNormalized) * dir.y;

            this.transform.forward = fwd;
            if (currentTime > duration)
            {
                fwd = new Vector3(0, 0, 1);
            }
            this.transform.forward = fwd;
        }
    }
}
