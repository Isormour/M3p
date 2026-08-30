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
    [SerializeField] float HipSineSpeed = 2.2f;

    [SerializeField] float EarDamping = 0.25f;
    [SerializeField] float EarElasticity = 0.05f;
    [SerializeField] float EarStiffness = 0.05f;
    [SerializeField] Vector3 EarGravity = new Vector3(0f, -0.2f, 0f);

    [SerializeField] float LegDamping = 0.2f;
    [SerializeField] float LegElasticity = 0.03f;
    [SerializeField] float LegStiffness = 0.02f;
    [SerializeField] Vector3 LegGravity = new Vector3(0f, -0.45f, 0f);

    void Awake()
    {
        if (CharacterAnimation != null)
            CharacterAnimation.enabled = false;

        EnsureDynamicBone(LeftEar, EarDamping, EarElasticity, EarStiffness, EarGravity);
        EnsureDynamicBone(RightEar, EarDamping, EarElasticity, EarStiffness, EarGravity);
        EnsureDynamicBone(LeftLeg, LegDamping, LegElasticity, LegStiffness, LegGravity);
        EnsureDynamicBone(RightLeg, LegDamping, LegElasticity, LegStiffness, LegGravity);
    }

    void LateUpdate()
    {
        if (Hips == null)
            return;

        float sine = Mathf.Sin(Time.time * HipSineSpeed) * HipSineAmplitude;
        Hips.position += Vector3.up * (HipHoverHeight + sine);
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
