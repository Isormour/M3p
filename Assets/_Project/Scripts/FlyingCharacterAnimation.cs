using UnityEngine;

[DefaultExecutionOrder(-50)]
public class FlyingCharacterAnimation : MonoBehaviour
{
    [SerializeField] Transform Hips;
    [SerializeField] Transform LeftWing;
    [SerializeField] Transform RightWing;
    [SerializeField] Transform LeftLeg;
    [SerializeField] Transform RightLeg;
    [SerializeField] Transform LeftEar;
    [SerializeField] Transform RightEar;
    [SerializeField] CharacterAnimation CharacterAnimation;

    [SerializeField] float HipHoverHeight = 0.45f;
    [SerializeField] float HipSineAmplitude = 0.08f;
    [SerializeField] float WingFlapSpeed = 2.4f;
    [SerializeField] float FallGravity = 18f;

    [SerializeField] Vector3 LeftWingFlapFrom = new Vector3(346.72f, 292.7f, 26.72f);
    [SerializeField] Vector3 LeftWingFlapTo = new Vector3(35.15f, 296.9f, 46.7f);
    [SerializeField] Vector3 RightWingFlapFrom = new Vector3(13.28f, 292.7f, 338.6f);
    [SerializeField] Vector3 RightWingFlapTo = new Vector3(324.85f, 296.9f, 318.62f);

    [SerializeField] float EarDamping = 0.25f;
    [SerializeField] float EarElasticity = 0.05f;
    [SerializeField] float EarStiffness = 0.05f;
    [SerializeField] Vector3 EarGravity = new Vector3(0f, -0.2f, 0f);

    [SerializeField] float LegDamping = 0.2f;
    [SerializeField] float LegElasticity = 0.03f;
    [SerializeField] float LegStiffness = 0.02f;
    [SerializeField] Vector3 LegGravity = new Vector3(0f, -0.45f, 0f);

    bool _dead;
    float _hover;
    float _fallVelocity;

    void Awake()
    {
        if (CharacterAnimation != null)
            CharacterAnimation.enabled = false;

        EnsureDynamicBone(LeftEar, EarDamping, EarElasticity, EarStiffness, EarGravity);
        EnsureDynamicBone(RightEar, EarDamping, EarElasticity, EarStiffness, EarGravity);
        EnsureDynamicBone(LeftLeg, LegDamping, LegElasticity, LegStiffness, LegGravity);
        EnsureDynamicBone(RightLeg, LegDamping, LegElasticity, LegStiffness, LegGravity);
    }

    public void Die()
    {
        if (_dead)
            return;

        _dead = true;
        float flapSin = Mathf.Sin(Time.time * WingFlapSpeed * Mathf.PI * 2f);
        _hover = HipHoverHeight + flapSin * HipSineAmplitude;
        _fallVelocity = 0f;
    }

    void LateUpdate()
    {
        if (_dead)
        {
            _fallVelocity += FallGravity * Time.deltaTime;
            _hover -= _fallVelocity * Time.deltaTime;
            if (_hover < 0f)
                _hover = 0f;

            if (Hips != null && _hover > 0f)
                Hips.position += Vector3.up * _hover;
            return;
        }

        float phase = Time.time * WingFlapSpeed * Mathf.PI * 2f;
        float flapSin = Mathf.Sin(phase);
        float flapT = flapSin * 0.5f + 0.5f;

        LerpLocalRotation(LeftWing, LeftWingFlapFrom, LeftWingFlapTo, flapT);
        LerpLocalRotation(RightWing, RightWingFlapFrom, RightWingFlapTo, flapT);

        if (Hips != null)
            Hips.position += Vector3.up * (HipHoverHeight + flapSin * HipSineAmplitude);
    }

    static void LerpLocalRotation(Transform bone, Vector3 fromEuler, Vector3 toEuler, float t)
    {
        if (bone == null)
            return;

        bone.localRotation = Quaternion.Slerp(Quaternion.Euler(fromEuler), Quaternion.Euler(toEuler), t);
    }

    static void EnsureDynamicBone(Transform bone, float damping, float elasticity, float stiffness, Vector3 gravity)
    {
        if (bone == null)
            return;

        var db = bone.GetComponent<DynamicBone>();
        bool created = db == null;
        if (created)
            db = bone.gameObject.AddComponent<DynamicBone>();

        db.m_Root = bone;
        db.m_UpdateMode = DynamicBone.UpdateMode.Default;
        db.m_Damping = damping;
        db.m_Elasticity = elasticity;
        db.m_Stiffness = stiffness;
        db.m_Gravity = gravity;
        db.m_Inert = 0.15f;
        db.m_BlendWeight = 1f;

        if (!created)
            db.SetupParticles();
    }
}
