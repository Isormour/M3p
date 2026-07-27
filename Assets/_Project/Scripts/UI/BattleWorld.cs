using UnityEngine;

namespace M3P
{
    public sealed class BattleWorld : MonoBehaviour
    {
        static readonly int AttackTriggerId = Animator.StringToHash("Attack");

        [SerializeField] GameObject _player;
        [Tooltip("Optional override. When unset, skill id 0 is treated as basic attack.")]

        Animator _playerAnimator;

        void Awake()
        {
            ResolveAnimator();
        }

        public void SetPlayer(GameObject player)
        {
            _player = player;
            ResolveAnimator();
        }

        public void PlayPlayerAttack(string triggerID, int varians = -1)
        {
            if (_playerAnimator == null)
                return;

            if (varians > 0)
                _playerAnimator.SetFloat("AttackVariants", Random.Range(0, varians) / (float)varians);
            _playerAnimator.SetTrigger(triggerID);
        }

        public void NotifySkillUsed(SkillDefinition skill)
        {
            if (skill._animationName == "BasicAttack")
            {
                PlayPlayerAttack("BasicAttack", 5);
                return;
            }

            PlayPlayerAttack("BasicAttack");

        }

        public void NotifyMatchWave(int tilesDestroyed)
        {
            if (tilesDestroyed > 0)
                PlayPlayerAttack("BasicAttack", 5);
        }

        void ResolveAnimator()
        {
            _playerAnimator = _player != null ? _player.GetComponentInChildren<Animator>() : null;
        }
    }
}
