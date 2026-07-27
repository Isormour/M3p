using System.Collections;
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
        Coroutine _watchBattleRoutine;

        public RectTransform PortraitTarget => _enemyIcon != null ? _enemyIcon.rectTransform : null;

        void OnEnable()
        {
            if (_watchBattleRoutine == null)
                _watchBattleRoutine = StartCoroutine(WatchBattleRoutine());
        }

        void OnDisable()
        {
            if (_watchBattleRoutine != null)
            {
                StopCoroutine(_watchBattleRoutine);
                _watchBattleRoutine = null;
            }

            ClearBinding();
        }

        IEnumerator WatchBattleRoutine()
        {
            EnemyBattleCharacter boundEnemy = null;

            while (true)
            {
                BattleManager battleManager = BattleManager.Instance;
                EnemyBattleCharacter activeEnemy = battleManager != null ? battleManager.ActiveEnemy : null;

                if (activeEnemy != boundEnemy)
                {
                    EnemyDefinition definition = battleManager != null ? battleManager.ActiveEnemyDefinition : null;
                    BindEnemy(definition, activeEnemy);
                    boundEnemy = activeEnemy;
                }

                yield return null;
            }
        }

        public void SetEnemy(EnemyDefinition enemyDefinition)
        {
            BindEnemy(enemyDefinition, BattleManager.Instance != null ? BattleManager.Instance.ActiveEnemy : null);
        }

        void BindEnemy(EnemyDefinition enemyDefinition, EnemyBattleCharacter enemy)
        {
            ApplyDefinition(enemyDefinition);
            BindStats(enemy);
        }

        void ApplyDefinition(EnemyDefinition enemyDefinition)
        {
            if (_enemyNameText != null)
            {
                string displayName = enemyDefinition != null ? enemyDefinition.Name : string.Empty;
                _enemyNameText.text = string.IsNullOrEmpty(displayName) ? "Enemy" : displayName;
            }

            if (_enemyIcon == null)
                return;

            Sprite icon = enemyDefinition != null ? enemyDefinition.Icon : null;
            _enemyIcon.sprite = icon;
            _enemyIcon.enabled = icon != null;
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

