using Match3;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelSkills : MonoBehaviour
    {
        [SerializeField] RectTransform _barContainer;
        [SerializeField] UIPlayerPanelSkillsBar _barPrefab;

        PlayerBattleCharacter _player;
        Coroutine _watchPlayerRoutine;
        Match3Board _board;

        UIPlayerPanelSkillsBar[] _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();

        public UIPlayerPanelSkillsBar[] SkillBars => _skillBars;

        public void SetPlayer(PlayerBattleCharacter player)
        {
            _player = player;
            BuildBars();
        }

        void OnEnable()
        {
            if (_watchPlayerRoutine == null)
                _watchPlayerRoutine = StartCoroutine(WatchPlayerRoutine());
        }

        void OnDisable()
        {
            if (_watchPlayerRoutine != null)
            {
                StopCoroutine(_watchPlayerRoutine);
                _watchPlayerRoutine = null;
            }

            ClearBars();
        }

        IEnumerator WatchPlayerRoutine()
        {
            while (true)
            {
                PlayerBattleCharacter activePlayer = BattleManager.Instance?.Player;
                Match3Board activeBoard = BattleManager.Instance?.ActiveBoard;

                if (activePlayer != _player)
                {
                    _player = activePlayer;
                    BuildBars();
                }

                if (activeBoard != _board)
                {
                    _board = activeBoard;
                    BuildBars();
                }

                RefreshSkillBars();

                yield return null;
            }
        }

        void RefreshSkillBars()
        {
            for (int i = 0; i < _skillBars.Length; i++)
            {
                if (_skillBars[i] != null)
                    _skillBars[i].RefreshInteractable();
            }
        }

        void BuildBars()
        {
            ClearBars();

            if (_player == null || _barPrefab == null)
            {
                if (_barPrefab == null)
                    Debug.LogError($"{nameof(UIPanelSkills)}: assign {nameof(_barPrefab)}.", this);

                _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();
                return;
            }

            SkillDefinition[] skills = _player.Skills;
            if (skills == null || skills.Length == 0)
            {
                _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();
                return;
            }

            var bars = new UIPlayerPanelSkillsBar[skills.Length];
            int barIndex = 0;

            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                    continue;

                UIPlayerPanelSkillsBar bar = Instantiate(_barPrefab, _barContainer);
                bar.name = $"SkillBar_{skill.name}";
                bar.Configure(skill, _player);
                bars[barIndex++] = bar;
            }

            if (barIndex == bars.Length)
            {
                _skillBars = bars;
                return;
            }

            var trimmedBars = new UIPlayerPanelSkillsBar[barIndex];
            for (int i = 0; i < barIndex; i++)
                trimmedBars[i] = bars[i];

            _skillBars = trimmedBars;
        }

        void EnsureBarContainer()
        {
            if (_barContainer == null)
                _barContainer = transform as RectTransform;

            if (_barContainer == null)
                return;

            HorizontalLayoutGroup layout = _barContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = _barContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }

        void ClearBars()
        {
            for (int i = 0; i < _skillBars.Length; i++)
            {
                if (_skillBars[i] != null)
                    Destroy(_skillBars[i].gameObject);
            }

            _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();
        }
    }
}
