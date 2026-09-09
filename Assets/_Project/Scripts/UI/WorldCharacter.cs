using M3P;
using System.Collections.Generic;
using UnityEngine;

public enum ECharacterType
{
    Ground,
    Flying
}

[RequireComponent(typeof(CharacterVFX))]
public class WorldCharacter : MonoBehaviour
{
    static readonly int AttackVariantsId = Animator.StringToHash("AttackVariants");
    static readonly int BasicAttackId = Animator.StringToHash("BasicAttack");
    static readonly int GuardUpId = Animator.StringToHash("GuardUp");
    static readonly int GetHitId = Animator.StringToHash("GetHit");
    static readonly int DieId = Animator.StringToHash("Die");
    static readonly int[] ActionTriggerIds = { BasicAttackId, GuardUpId };

    [SerializeField] Animator _animator;
    [SerializeField] int _attackVariantCount = 5;
    [SerializeField] CharacterVFX _vfx;
    [SerializeField] BattleCharacterShield _shieldIndicator;

    [Tooltip("Hands (or weapon tips) that gather energy and throw attacks. Leave empty to use the hand bones of a humanoid rig.")]
    [SerializeField] Transform[] _attackOrigins;

    [field: SerializeField] public ECharacterType CharacterType { private set; get; } = ECharacterType.Ground;

    Transform[] _resolvedAttackOrigins;

    public Animator Anim => _animator;

    public CharacterVFX VFX => _vfx;

    public bool HasShield => _shieldIndicator != null && _shieldIndicator.IsVisible;

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        EnsureVfx();
        EnsureShield();
        _vfx?.CollectRenderers();
    }

    void Reset()
    {
        EnsureVfx();
        EnsureShield();
        _vfx?.CollectRenderers();
    }

    void OnValidate()
    {
        EnsureVfx();
        EnsureShield();
    }

    void EnsureVfx()
    {
        if (_vfx == null)
            _vfx = GetComponent<CharacterVFX>();
        if (_vfx == null)
            _vfx = GetComponentInChildren<CharacterVFX>(true);
    }

    void EnsureShield()
    {
        if (_shieldIndicator == null)
            _shieldIndicator = GetComponentInChildren<BattleCharacterShield>(true);
    }

    public void BindShield(SoftStats stats)
    {
        EnsureShield();
        _shieldIndicator?.Bind(stats);
    }

    /// <summary>
    /// Where the <paramref name="index"/>-th attack of a flurry comes from. Successive indices cycle
    /// through the available hands, so a series of swings alternates instead of repeating one arm.
    /// </summary>
    public Transform GetAttackOrigin(int index)
    {
        Transform[] origins = ResolveAttackOrigins();
        if (origins.Length == 0)
            return transform;

        int wrapped = index % origins.Length;
        if (wrapped < 0)
            wrapped += origins.Length;

        return origins[wrapped];
    }

    Transform[] ResolveAttackOrigins()
    {
        if (_resolvedAttackOrigins != null)
            return _resolvedAttackOrigins;

        List<Transform> origins = new List<Transform>(2);

        if (_attackOrigins != null)
        {
            for (int i = 0; i < _attackOrigins.Length; i++)
            {
                if (_attackOrigins[i] != null)
                    origins.Add(_attackOrigins[i]);
            }
        }

        if (origins.Count == 0 && _animator != null && _animator.isHuman)
        {
            AddBoneOrigin(origins, HumanBodyBones.RightHand);
            AddBoneOrigin(origins, HumanBodyBones.LeftHand);
        }

        if (origins.Count == 0)
            origins.Add(transform);

        _resolvedAttackOrigins = origins.ToArray();
        return _resolvedAttackOrigins;
    }

    void AddBoneOrigin(List<Transform> origins, HumanBodyBones bone)
    {
        Transform resolved = _animator.GetBoneTransform(bone);
        if (resolved != null)
            origins.Add(resolved);
    }

    public void PlayAttack(string triggerName = "BasicAttack", int variantCount = -1)
    {
        if (_animator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            triggerName = "BasicAttack";

        bool isBasicAttack = triggerName == "BasicAttack";
        if (isBasicAttack)
        {
            int count = variantCount >= 0 ? variantCount : _attackVariantCount;
            if (count > 0)
                _animator.SetFloat(AttackVariantsId, Random.Range(0, count) / (float)count);
        }

        for (int i = 0; i < ActionTriggerIds.Length; i++)
            _animator.ResetTrigger(ActionTriggerIds[i]);

        _animator.SetTrigger(triggerName);
    }

    public void PlayGetHit()
    {
        if (HasShield)
        {
            _shieldIndicator.Pulse();
            return;
        }

        if (_animator == null)
            return;

        _animator.SetTrigger(GetHitId);
    }

    public void PlayDie()
    {
        _shieldIndicator?.Unbind();

        if (_animator != null)
        {
            _animator.ResetTrigger(GetHitId);
            _animator.SetTrigger(DieId);
        }

        var flying = GetComponent<FlyingCharacterAnimation>();
        if (flying == null)
            flying = GetComponentInChildren<FlyingCharacterAnimation>(true);
        flying?.Die();
    }

    public void PlayHitReaction(bool died, bool shielded = false)
    {
        if (died)
        {
            PlayDie();
            return;
        }

        if (HasShield)
        {
            _shieldIndicator.Pulse();
            return;
        }

        PlayGetHit();
    }
}
