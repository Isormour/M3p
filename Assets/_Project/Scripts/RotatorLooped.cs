using UnityEngine;

public class RotatorLooped : MonoBehaviour
{
    [SerializeField] AnimationCurve curve;
    [SerializeField] float AngleDelta;
    [SerializeField] float startTime = 0;
    float currentTime;
    [SerializeField] float speed = 1;

    [SerializeField] SpriteRenderer spr;
    [SerializeField] AnimationCurve spriteAlpha;
    void Start()
    {
        currentTime = startTime;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime * speed;
        this.transform.rotation = Quaternion.Euler(0, 0, AngleDelta * curve.Evaluate(currentTime));
        if (currentTime > 1)
        {
            currentTime = 0;
        }
        if (spr == null) return;

        Color col = spr.color;
        col.a = spriteAlpha.Evaluate(currentTime);
        spr.color = col;
    }
}
