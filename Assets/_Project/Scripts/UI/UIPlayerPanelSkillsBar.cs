using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPlayerPanelSkillsBar : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] TextMeshProUGUI _skillNameLabel;
        [SerializeField] RectTransform _costContainer;
        [SerializeField] UIPlayerPanelSkillsCostLabel _costLabelPrefab;

        SkillDefinition _skill;
        PlayerBattleCharacter _player;
        SoftStats _boundSoftStats;

        readonly List<UIPlayerPanelSkillsCostLabel> _costLabels = new List<UIPlayerPanelSkillsCostLabel>();

        public SkillDefinition Skill => _skill;
        public IReadOnlyList<UIPlayerPanelSkillsCostLabel> CostLabels => _costLabels;

        public void Configure(SkillDefinition skill, PlayerBattleCharacter player)
        {
            _skill = skill;
            _player = player;

            UnbindSoftStats();
            BindSoftStats();
            ClearCostLabels();

            if (_skillNameLabel == null)
            {
                Debug.LogError($"{nameof(UIPlayerPanelSkillsBar)}: assign {nameof(_skillNameLabel)} on the prefab.", this);
                return;
            }

            _skillNameLabel.text = skill != null ? skill.name : string.Empty;

            if (skill == null || _costLabelPrefab == null)
            {
                if (skill != null && _costLabelPrefab == null)
                    Debug.LogError($"{nameof(UIPlayerPanelSkillsBar)}: assign {nameof(_costLabelPrefab)}.", this);

                RefreshInteractable();
                return;
            }

            TileTypeManaCost[] costs = skill.ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0 || costs[i].TileType == null)
                    continue;

                UIPlayerPanelSkillsCostLabel label = Instantiate(_costLabelPrefab, _costContainer);
                label.name = $"CostLabel_{costs[i].TileType.name}";
                label.Configure(costs[i].TileType.Sprite, costs[i].Amount);
                _costLabels.Add(label);
            }

            WireButton();
            RefreshInteractable();
        }

        void WireButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
            {
                Debug.LogError($"{nameof(UIPlayerPanelSkillsBar)}: assign {nameof(_button)} or add a {nameof(Button)} component.", this);
                return;
            }

            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
        }

        void HandleClick()
        {
            if (_skill == null || _player == null)
                return;

            BattleManager manager = BattleManager.Instance;
            if (manager == null || !manager.IsPlayerTurn)
                return;

            BattleCharacter target = manager.ActiveEnemy;
            if (target == null)
                return;

            manager.TryExecuteSkill(_skill, _player, target);
            RefreshInteractable();
        }

        void BindSoftStats()
        {
            _boundSoftStats = _player?.Stats?.Soft;
            if (_boundSoftStats != null)
                _boundSoftStats.Changed += HandleSoftStatsChanged;
        }

        void UnbindSoftStats()
        {
            if (_boundSoftStats != null)
                _boundSoftStats.Changed -= HandleSoftStatsChanged;

            _boundSoftStats = null;
        }

        void HandleSoftStatsChanged()
        {
            RefreshInteractable();
        }

        public void RefreshInteractable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
                return;

            _button.interactable = CanUseSkill();
        }

        bool CanUseSkill()
        {
            if (_skill == null || _player?.Stats?.Soft == null)
                return false;

            BattleManager manager = BattleManager.Instance;
            if (manager == null || !manager.IsPlayerTurn)
                return false;

            BattleCharacter target = manager.ActiveEnemy;
            if (target == null || !target.IsAlive)
                return false;

            SoftStats softStats = _player.Stats.Soft;
            return _skill.HasEnoughActionPoints(softStats) && _skill.HasEnoughMana(softStats);
        }

        void ClearCostLabels()
        {
            for (int i = 0; i < _costLabels.Count; i++)
            {
                if (_costLabels[i] != null)
                    Destroy(_costLabels[i].gameObject);
            }

            _costLabels.Clear();
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            UnbindSoftStats();
            ClearCostLabels();
        }
    }
}
