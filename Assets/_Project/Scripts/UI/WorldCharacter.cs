using UnityEngine;

public class WorldCharacter : MonoBehaviour
{
    static readonly int AttackVariantsId = Animator.StringToHash("AttackVariants");
    static readonly int GetHitId = Animator.StringToHash("GetHit");
    static readonly int DieId = Animator.StringToHash("Die");

    [SerializeField] Animator _animator;
    [SerializeField] int _attackVariantCount = 5;

    public Animator Anim => _animator;

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    public void PlayAttack(string triggerName = "BasicAttack", int variantCount = -1)
    {
        if (_animator == null)
            return;

        int count = variantCount >= 0 ? variantCount : _attackVariantCount;
        if (count > 0)
            _animator.SetFloat(AttackVariantsId, Random.Range(0, count) / (float)count);

        _animator.SetTrigger(triggerName);
    }

    public void PlayGetHit()
    {
        if (_animator == null)
            return;

        _animator.SetTrigger(GetHitId);
    }

    public void PlayDie()
    {
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

    public void PlayHitReaction(bool died)
    {
        if (died)
            PlayDie();
        else
            PlayGetHit();
    }
}
