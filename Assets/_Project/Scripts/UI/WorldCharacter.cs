using UnityEngine;

public class WorldCharacter : MonoBehaviour
{
    static readonly int AttackVariantsId = Animator.StringToHash("AttackVariants");

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
}
