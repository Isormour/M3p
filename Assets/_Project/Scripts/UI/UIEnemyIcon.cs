using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIEnemyIcon : MonoBehaviour
    {
        [SerializeField] Image _enemyIcon;
        [SerializeField] TextMeshProUGUI _enemyNameText;
        [SerializeField] UISimpleIndicator _enemyHP;

        EnemyBattleCharacter _boundEnemy;

        public RectTransform PortraitTarget => _enemyIcon != null ? _enemyIcon.rectTransform : null;

        void OnDisable()
        {
            ClearBinding();
        }

        public void SetEnemy(EnemyDefinition enemyDefinition)
        {
            SetEnemy(enemyDefinition, BattleManager.Instance != null ? BattleManager.Instance.ActiveEnemy : null);
        }

        public void SetEnemy(EnemyDefinition enemyDefinition, EnemyBattleCharacter enemy)
        {
            BindEnemy(enemyDefinition, enemy);
        }

        void BindEnemy(EnemyDefinition enemyDefinition, EnemyBattleCharacter enemy)
        {
            ApplyDefinition(enemyDefinition, enemy);
            BindStats(enemy);
        }

        void ApplyDefinition(EnemyDefinition enemyDefinition, EnemyBattleCharacter enemy = null)
        {
            if (_enemyNameText != null)
            {
                string displayName = enemy != null && enemy.RuntimeSpec != null
                    ? enemy.RuntimeSpec.DisplayName
                    : enemyDefinition != null ? enemyDefinition.Name : string.Empty;
                _enemyNameText.text = string.IsNullOrEmpty(displayName) ? "Enemy" : displayName;
            }
        }

        void BindStats(EnemyBattleCharacter enemy)
        {
            if (_boundEnemy == enemy)
                return;

            _boundEnemy = enemy;

            if (_enemyHP == null)
                return;

            if (enemy?.Stats?.Soft == null)
            {
                _enemyHP.Unbind();
                return;
            }

            SoftStats softStats = enemy.Stats.Soft;
            int maxHealth = enemy.Stats.MaxHealth;

            _enemyHP.Bind(
                () => softStats.CurrentHealth,
                () => maxHealth,
                handler => softStats.Changed += handler,
                handler => softStats.Changed -= handler);
        }

        void ClearBinding()
        {
            _boundEnemy = null;
            _enemyHP?.Unbind();
            ApplyDefinition(null);
        }
    }
}

