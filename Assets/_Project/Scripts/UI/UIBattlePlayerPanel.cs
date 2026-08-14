using Match3;
using System.Collections;
using UnityEngine;

namespace M3P
{
    public sealed class UIBattlePlayerPanel : MonoBehaviour
    {
        [SerializeField] PlayerBattleCharacter _player;
        [SerializeField] UIPanelPlayerMana _playerManaPanel;
        [SerializeField] UIPanelSkills _playerSkillsPanel;
        [SerializeField] UISimpleIndicator _playerHP;
        [SerializeField] UISimpleIndicator _playerActionPoints;
        [SerializeField] UISimpleIndicator _playerShield;

        Coroutine _watchBattleRoutine;

        public PlayerBattleCharacter Player => _player;

        void Awake()
        {
            ResolveShieldIndicator();
            ApplyPlayer();
        }

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

            UnbindIndicators();
        }

        void OnValidate()
        {
            if (_playerManaPanel == null)
                _playerManaPanel = GetComponentInChildren<UIPanelPlayerMana>(true);

            if (_playerSkillsPanel == null)
                _playerSkillsPanel = GetComponentInChildren<UIPanelSkills>(true);

            ResolveShieldIndicator();
        }

        public void SetPlayer(PlayerBattleCharacter player)
        {
            _player = player;
            ApplyPlayer();
        }

        IEnumerator WatchBattleRoutine()
        {
            Match3Board boundBoard = null;

            while (true)
            {
                BattleManager battleManager = BattleManager.Instance;
                Match3Board activeBoard = battleManager != null ? battleManager.ActiveBoard : null;

                if (_player == null && battleManager != null)
                    _player = battleManager.Player;

                if (activeBoard != boundBoard)
                {
                    ApplyPlayer();
                    boundBoard = activeBoard;
                }

                yield return null;
            }
        }

        void ApplyPlayer()
        {
            if (_playerManaPanel != null)
                _playerManaPanel.SetPlayer(_player);

            if (_playerSkillsPanel != null)
                _playerSkillsPanel.SetPlayer(_player);

            BindIndicators();
        }

        void BindIndicators()
        {
            if (_player?.Stats?.Soft == null)
            {
                UnbindIndicators();
                return;
            }

            SoftStats softStats = _player.Stats.Soft;
            int maxHealth = _player.Stats.MaxHealth;

            if (_playerHP != null)
            {
                _playerHP.Bind(
                    () => softStats.CurrentHealth,
                    () => maxHealth,
                    handler => softStats.Changed += handler,
                    handler => softStats.Changed -= handler);
            }

            if (_playerActionPoints != null)
            {
                _playerActionPoints.Bind(
                    () => softStats.CurrentActionPoints,
                    () => softStats.MaxActionPoints,
                    handler => softStats.Changed += handler,
                    handler => softStats.Changed -= handler);
            }

            ResolveShieldIndicator();
            if (_playerShield != null)
            {
                _playerShield.Bind(
                    () => softStats.CurrentShield,
                    () => softStats.CurrentShield > 0 ? softStats.CurrentShield : 1,
                    handler => softStats.Changed += handler,
                    handler => softStats.Changed -= handler,
                    (current, _) => current.ToString());
            }
        }

        void UnbindIndicators()
        {
            _playerHP?.Unbind();
            _playerActionPoints?.Unbind();
            _playerShield?.Unbind();
        }

        void ResolveShieldIndicator()
        {
            if (_playerShield != null)
                return;

            UISimpleIndicator[] indicators = GetComponentsInChildren<UISimpleIndicator>(true);
            for (int i = 0; i < indicators.Length; i++)
            {
                UISimpleIndicator indicator = indicators[i];
                if (indicator != null && indicator.gameObject.name == "ShieldIndicator")
                {
                    _playerShield = indicator;
                    return;
                }
            }
        }
    }
}
