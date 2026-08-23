using Match3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelSkills : MonoBehaviour
    {
        [SerializeField] RectTransform _barContainer;
        [SerializeField] UIPlayerPanelSkillsBar _barPrefab;

        BattleCharacter _owner;
        Coroutine _watchOwnerRoutine;
        Match3Board _board;

        UIPlayerPanelSkillsBar[] _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();

        public UIPlayerPanelSkillsBar[] SkillBars => _skillBars;

        public void Set(BattleCharacter owner)
        {
            _owner = owner;
            BuildBars();
        }

        public void SetPlayer(PlayerBattleCharacter player)
        {
            Set(player);
        }

        void OnEnable()
        {
            if (_watchOwnerRoutine == null)
                _watchOwnerRoutine = StartCoroutine(WatchOwnerRoutine());
        }

        void OnDisable()
        {
            if (_watchOwnerRoutine != null)
            {
                StopCoroutine(_watchOwnerRoutine);
                _watchOwnerRoutine = null;
            }

            ClearBars();
        }

        IEnumerator WatchOwnerRoutine()
        {
            while (true)
            {
                WatchOwner();
                RefreshSkillBars();
                yield return null;
            }
        }

        void WatchOwner()
        {
            if (_owner == null)
                return;

            BattleManager battleManager = BattleManager.Instance;
            if (battleManager == null)
                return;

            BattleCharacter live = _owner.IsPlayerControlled
                ? battleManager.Player
                : battleManager.ActiveEnemy;

            Match3Board activeBoard = battleManager.ActiveBoard;
            bool boardChanged = _owner.IsPlayerControlled && activeBoard != _board;
            if (boardChanged)
                _board = activeBoard;

            if (live != _owner || boardChanged)
            {
                _owner = live;
                BuildBars();
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

            if (_barPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelSkills)}: assign {nameof(_barPrefab)}.", this);
                _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();
                return;
            }

            IReadOnlyList<SkillDefinition> skills = _owner != null ? _owner.Skills : null;
            if (skills == null || skills.Count == 0)
            {
                _skillBars = System.Array.Empty<UIPlayerPanelSkillsBar>();
                return;
            }

            var bars = new UIPlayerPanelSkillsBar[skills.Count];
            int barIndex = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                    continue;

                UIPlayerPanelSkillsBar bar = Instantiate(_barPrefab, _barContainer);
                bar.name = $"SkillBar_{skill.name}";
                bar.Configure(skill, _owner);
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
