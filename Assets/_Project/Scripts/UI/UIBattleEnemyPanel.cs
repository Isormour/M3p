using System.Collections;
using UnityEngine;

namespace M3P
{
    public sealed class UIBattleEnemyPanel : MonoBehaviour
    {
        [SerializeField] UIEnemyIcon _enemyIcon;
        [SerializeField] UIPanelSkills _enemySkillsPanel;

        EnemyBattleCharacter _enemy;
        Coroutine _watchBattleRoutine;

        public EnemyBattleCharacter Enemy => _enemy;

        void Awake()
        {
            ResolveChildren();
            ApplyEnemy();
        }

        void OnEnable()
        {
            ApplyEnemy();

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
        }

        void OnValidate()
        {
            ResolveChildren();
        }

        public void SetEnemy(EnemyDefinition definition, EnemyBattleCharacter enemy)
        {
            _enemy = enemy;
            ApplyEnemy(definition, enemy);
        }

        IEnumerator WatchBattleRoutine()
        {
            EnemyBattleCharacter boundEnemy = _enemy;
            EnemyRuntimeSpec boundSpec = boundEnemy != null ? boundEnemy.RuntimeSpec : null;

            while (true)
            {
                BattleManager battleManager = BattleManager.Instance;
                EnemyBattleCharacter activeEnemy = battleManager != null ? battleManager.ActiveEnemy : null;
                EnemyDefinition activeDefinition = battleManager != null
                    ? battleManager.ActiveEnemyDefinition
                    : activeEnemy != null ? activeEnemy.Definition : null;
                EnemyRuntimeSpec activeSpec = battleManager != null
                    ? battleManager.ActiveEnemySpec
                    : activeEnemy != null ? activeEnemy.RuntimeSpec : null;

                if (activeEnemy != boundEnemy || activeSpec != boundSpec)
                {
                    _enemy = activeEnemy;
                    ApplyEnemy(activeDefinition, activeEnemy);
                    boundEnemy = activeEnemy;
                    boundSpec = activeSpec;
                }

                yield return null;
            }
        }

        void ApplyEnemy()
        {
            BattleManager battleManager = BattleManager.Instance;
            EnemyBattleCharacter enemy = _enemy != null ? _enemy : battleManager != null ? battleManager.ActiveEnemy : null;
            EnemyDefinition definition = battleManager != null
                ? battleManager.ActiveEnemyDefinition
                : enemy != null ? enemy.Definition : null;

            _enemy = enemy;
            ApplyEnemy(definition, enemy);
        }

        void ApplyEnemy(EnemyDefinition definition, EnemyBattleCharacter enemy)
        {
            if (_enemyIcon != null)
                _enemyIcon.SetEnemy(definition, enemy);

            if (_enemySkillsPanel != null)
                _enemySkillsPanel.Set(enemy);
        }

        void ResolveChildren()
        {
            if (_enemyIcon == null)
                _enemyIcon = GetComponentInChildren<UIEnemyIcon>(true);

            if (_enemySkillsPanel == null)
                _enemySkillsPanel = GetComponentInChildren<UIPanelSkills>(true);
        }
    }
}
