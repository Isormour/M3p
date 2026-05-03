using UnityEngine;

namespace M3P
{
    public abstract class BattleCharacter : MonoBehaviour
    {
        [SerializeField] int _maxHealth = 100;

        int _currentHealth;

        public abstract bool IsPlayerControlled { get; }

        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;

        public bool IsAlive => _currentHealth > 0;

        void Awake()
        {
            _currentHealth = _maxHealth;
        }

        /// <summary>Called after instantiation to apply runtime data (definition, save, etc.).</summary>
        protected void ConfigureHealth(int maxHealth)
        {
            _maxHealth = Mathf.Max(1, maxHealth);
            _currentHealth = _maxHealth;
        }

        public virtual void OnTurnStarted() { }

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
        }

        public void SetCurrentHealth(int value)
        {
            _currentHealth = Mathf.Clamp(value, 0, _maxHealth);
        }

        public void RestoreFullHealth()
        {
            _currentHealth = _maxHealth;
        }
    }
}

