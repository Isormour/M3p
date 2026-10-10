using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the hips and everything below them in the bind pose.
/// Spine, chest, arms and head still follow the Animator.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class CharacterDisableLegs : MonoBehaviour
{
    Animator _animator;
    CachedBone[] _lowerBody;

    struct CachedBone
    {
        public Transform Bone;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
    }

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);
    }

    void Start()
    {
        if (!TryCacheBindPose())
            enabled = false;
    }

    bool TryCacheBindPose()
    {
        if (_animator == null || !_animator.isHuman)
        {
            Debug.LogWarning($"{nameof(CharacterDisableLegs)} requires a humanoid Animator.", this);
            return false;
        }

        Transform hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            Debug.LogWarning($"{nameof(CharacterDisableLegs)} could not find the hips bone.", this);
            return false;
        }

        _animator.WriteDefaultValues();

        Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
        var bones = new List<CachedBone>(16);
        AddLowerBody(hips, spine, bones);
        _lowerBody = bones.ToArray();
        return _lowerBody.Length > 0;
    }

    static void AddLowerBody(Transform hips, Transform spine, List<CachedBone> bones)
    {
        AddBone(hips, bones);

        for (int i = 0; i < hips.childCount; i++)
        {
            Transform child = hips.GetChild(i);
            if (IsSpineBranch(child, spine))
                continue;

            AddBranch(child, bones);
        }
    }

    static bool IsSpineBranch(Transform child, Transform spine)
    {
        if (spine == null)
            return false;

        for (Transform bone = spine; bone != null; bone = bone.parent)
        {
            if (bone == child)
                return true;
        }

        return false;
    }

    static void AddBranch(Transform bone, List<CachedBone> bones)
    {
        AddBone(bone, bones);

        for (int i = 0; i < bone.childCount; i++)
            AddBranch(bone.GetChild(i), bones);
    }

    static void AddBone(Transform bone, List<CachedBone> bones)
    {
        bones.Add(new CachedBone
        {
            Bone = bone,
            LocalPosition = bone.localPosition,
            LocalRotation = bone.localRotation,
        });
    }

    void LateUpdate()
    {
        if (_lowerBody == null)
            return;

        for (int i = 0; i < _lowerBody.Length; i++)
        {
            CachedBone cached = _lowerBody[i];
            if (cached.Bone == null)
                continue;

            cached.Bone.localPosition = cached.LocalPosition;
            cached.Bone.localRotation = cached.LocalRotation;
        }
    }
}
