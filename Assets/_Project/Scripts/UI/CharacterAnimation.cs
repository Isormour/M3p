using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    static readonly int WalkId = Animator.StringToHash("Walk");

    [SerializeField] bool _useFootIK = true;
    [SerializeField, Range(0f, 1f)] float _footIKWeight = 1f;
    [SerializeField] LayerMask _groundMask = ~0;
    [SerializeField] float _rayUp = 0.5f;
    [SerializeField] float _rayDown = 1.5f;
    [SerializeField] float _footSoleOffset = 0.02f;

    Animator _animator;
    Vector3 _standLeftLocal;
    Vector3 _standRightLocal;
    bool _hasStandPose;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!_useFootIK || _animator == null || layerIndex != 0)
            return;

        bool walking = _animator.GetBool(WalkId);
        if (walking)
            _hasStandPose = false;
        else if (!_hasStandPose)
            CaptureStandPose();

        ApplyFootIK(AvatarIKGoal.LeftFoot, _standLeftLocal, walking);
        ApplyFootIK(AvatarIKGoal.RightFoot, _standRightLocal, walking);
    }

    void CaptureStandPose()
    {
        _standLeftLocal = transform.InverseTransformPoint(_animator.GetIKPosition(AvatarIKGoal.LeftFoot));
        _standRightLocal = transform.InverseTransformPoint(_animator.GetIKPosition(AvatarIKGoal.RightFoot));
        _hasStandPose = true;
    }

    void ApplyFootIK(AvatarIKGoal foot, Vector3 standLocal, bool walking)
    {
        Vector3 animPos = _animator.GetIKPosition(foot);
        Quaternion animRot = _animator.GetIKRotation(foot);
        Vector3 target = walking || !_hasStandPose
            ? animPos
            : transform.TransformPoint(standLocal);

        Vector3 origin = new Vector3(target.x, transform.position.y + _rayUp, target.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _rayUp + _rayDown, _groundMask, QueryTriggerInteraction.Ignore))
        {
            target = hit.point + hit.normal * _footSoleOffset;
            animRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * animRot;
        }
        else
        {
            target.y = transform.position.y + _footSoleOffset;
        }

        _animator.SetIKPositionWeight(foot, _footIKWeight);
        _animator.SetIKRotationWeight(foot, _footIKWeight);
        _animator.SetIKPosition(foot, target);
        _animator.SetIKRotation(foot, animRot);
    }

    void Hit()
    {
    }

    void FootL()
    {
    }

    void FootR()
    {
    }
}
