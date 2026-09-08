using M3P;
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
    [field: SerializeField] public ECharacterType CharacterType { private set; get; } = ECharacterType.Ground;

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

        if (shielded || HasShield)
        {
            _shieldIndicator?.Pulse();
            return;
        }

        PlayGetHit();
    }
}
