using UnityEngine;

namespace M3P
{
    public abstract class BattleCharacter : MonoBehaviour
    {
        CharacterStats _characterStats;

        public abstract bool IsPlayerControlled { get; }

        public abstract EEffectSource EffectSource { get; }

        public CharacterStats Stats => _characterStats;

        public bool IsAlive => _characterStats != null && _characterStats.IsAlive;

        protected void SetCharacterStats(CharacterStats stats)
        {
            _characterStats = stats;
        }

        public virtual void OnTurnStarted() { }
    }
}
