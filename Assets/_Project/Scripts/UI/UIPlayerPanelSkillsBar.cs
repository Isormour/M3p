using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPlayerPanelSkillsBar : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _artworkImage;
        [SerializeField] TextMeshProUGUI _skillNameLabel;
        [SerializeField] RectTransform _costContainer;
        [SerializeField] UIPlayerPanelSkillsCostLabel _costLabelPrefab;

        SkillDefinition _skill;
        PlayerBattleCharacter _player;
        SoftStats _boundSoftStats;
        UICardChoiceOverlay _promptOverlay;

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

            RefreshSkillName();
            RefreshArtwork();

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

            if (skill.DistinctColorManaCost > 0)
            {
                UIPlayerPanelSkillsCostLabel label = Instantiate(_costLabelPrefab, _costContainer);
                label.name = "CostLabel_DistinctColors";
                label.Configure(null, skill.DistinctColorManaCost);
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
            if (manager == null || !manager.IsPlayerTurn || manager.IsAwaitingSkillChoice)
                return;

            if (!_player.IsSkillReady(_skill))
            {
                manager.TryReduceSkillCooldown(_skill, _player);
                RefreshInteractable();
                return;
            }

            BattleCharacter target = manager.ActiveEnemy;
            if (target == null)
                return;

            if (_skill.CastPrompt == SkillCastPrompt.DiscardCard)
            {
                BeginDiscardPrompt(manager, target);
                return;
            }

            if (_skill.CastPrompt == SkillCastPrompt.TransmuteMana)
            {
                BeginTransmutePrompt(manager, target);
                return;
            }

            manager.TryExecuteSkill(_skill, _player, target);
            RefreshInteractable();
        }

        void BeginDiscardPrompt(BattleManager manager, BattleCharacter target)
        {
            HidePrompt();
            manager.BeginSkillChoice();
            _promptOverlay = SkillCastPromptUI.ShowDiscardReturn(
                manager.CardPlay != null ? manager.CardPlay.Deck : null,
                index => CompleteCast(manager, target, new SkillCastChoice(index)),
                () => CancelPrompt(manager));
        }

        void BeginTransmutePrompt(BattleManager manager, BattleCharacter target)
        {
            HidePrompt();
            manager.BeginSkillChoice();
            _promptOverlay = SkillCastPromptUI.ShowManaColor(
                "Z",
                _player.Stats?.Soft,
                _skill,
                excludeTypeId: -1,
                remainingAfterCost: true,
                requirePositiveAmount: true,
                picked => HandleTransmuteSourcePicked(manager, target, picked),
                () => CancelPrompt(manager));
        }

        void HandleTransmuteSourcePicked(BattleManager manager, BattleCharacter target, int sourceTypeId)
        {
            HidePrompt();
            _promptOverlay = SkillCastPromptUI.ShowManaColor(
                "Na",
                _player.Stats?.Soft,
                _skill,
                excludeTypeId: sourceTypeId,
                remainingAfterCost: false,
                requirePositiveAmount: false,
                picked => CompleteCast(manager, target, new SkillCastChoice(sourceTypeId, picked)),
                () => CancelPrompt(manager));
        }

        void CompleteCast(BattleManager manager, BattleCharacter target, SkillCastChoice choice)
        {
            HidePrompt();
            manager.CancelSkillChoice();
            manager.TryExecuteSkill(_skill, _player, target, choice);
            RefreshInteractable();
        }

        void CancelPrompt(BattleManager manager)
        {
            HidePrompt();
            manager.CancelSkillChoice();
            RefreshInteractable();
        }

        void HidePrompt()
        {
            if (_promptOverlay == null)
                return;

            _promptOverlay.Dismiss();
            _promptOverlay = null;
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
            BattleManager manager = BattleManager.Instance;
            if (manager == null || !manager.IsPlayerTurn)
                HidePrompt();

            if (_button == null)
                _button = GetComponent<Button>();

            RefreshSkillName();

            if (_button == null)
                return;

            _button.interactable = CanInteractWithSkill();
        }

        void RefreshSkillName()
        {
            if (_skillNameLabel == null)
                return;

            if (_skill == null)
            {
                _skillNameLabel.text = string.Empty;
                return;
            }

            int remaining = _player != null ? _player.GetRemainingCooldown(_skill) : 0;
            _skillNameLabel.text = remaining > 0
                ? $"{_skill.DisplayName} ({remaining})"
                : _skill.DisplayName;
        }

        void RefreshArtwork()
        {
            EnsureArtworkImage();
            if (_artworkImage == null)
                return;

            Sprite artwork = _skill != null ? _skill.Artwork : null;
            _artworkImage.sprite = artwork;
            _artworkImage.enabled = artwork != null;
        }

        void EnsureArtworkImage()
        {
            if (_artworkImage != null)
                return;

            Transform existing = transform.Find("Artwork");
            if (existing != null)
                _artworkImage = existing.GetComponent<Image>();

            if (_artworkImage != null)
                return;

            var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artworkObject.transform.SetParent(transform, false);
            artworkObject.transform.SetAsFirstSibling();

            RectTransform rect = (RectTransform)artworkObject.transform;
            rect.anchorMin = new Vector2(0.08f, 0.28f);
            rect.anchorMax = new Vector2(0.92f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _artworkImage = artworkObject.GetComponent<Image>();
            _artworkImage.preserveAspect = true;
            _artworkImage.raycastTarget = false;
            _artworkImage.color = Color.white;
        }

        bool CanInteractWithSkill()
        {
            if (_skill == null || _player?.Stats?.Soft == null)
                return false;

            BattleManager manager = BattleManager.Instance;
            if (manager == null || !manager.IsPlayerTurn || manager.IsAwaitingSkillChoice)
                return false;

            if (!_player.IsSkillReady(_skill))
                return _player.CanReduceSkillCooldown(_skill);

            BattleCharacter target = manager.ActiveEnemy;
            if (target == null || !target.IsAlive)
                return false;

            SoftStats softStats = _player.Stats.Soft;
            return _skill.HasEnoughActionPoints(softStats)
                && _skill.HasEnoughMana(softStats)
                && _skill.MeetsCastRequirements(_player, target);
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
            HidePrompt();
            ClearCostLabels();
        }
    }
}
