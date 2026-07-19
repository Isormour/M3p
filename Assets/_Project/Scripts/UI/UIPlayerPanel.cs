using System.Collections;
using Match3;
using UnityEngine;

namespace M3P
{
    public sealed class UIPlayerPanel : MonoBehaviour
    {
        [SerializeField] PlayerBattleCharacter _player;
        [SerializeField] UIPanelPlayerMana _playerManaPanel;
        [SerializeField] UIPlayerPanelSkills _playerSkillsPanel;
        [SerializeField] UIHPIndicator _playerHP;

        Coroutine _watchBattleRoutine;

        public PlayerBattleCharacter Player => _player;

        void Awake()
        {
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

            UnbindHP();
        }

        void OnValidate()
        {
            if (_playerManaPanel == null)
                _playerManaPanel = GetComponentInChildren<UIPanelPlayerMana>(true);

            if (_playerSkillsPanel == null)
                _playerSkillsPanel = GetComponentInChildren<UIPlayerPanelSkills>(true);

            if (_playerHP == null)
                _playerHP = GetComponentInChildren<UIHPIndicator>(true);
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

            BindHP();
        }

        void BindHP()
        {
            if (_playerHP == null)
                return;

            if (_player?.Stats?.Soft == null)
            {
                _playerHP.Unbind();
                return;
            }

            _playerHP.Bind(_player.Stats.Soft, _player.Stats.MaxHealth);
        }

        void UnbindHP()
        {
            _playerHP?.Unbind();
        }
    }
}
