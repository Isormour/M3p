using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public enum BattleOutcome
    {
        Win,
        Lose
    }
    public enum ERewardType
    {
        None,
        Exp,
        Shard,
    }
    public sealed class UIEndBattlePanel : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] GameObject _winSection;
        [SerializeField] GameObject _loseSection;
        [SerializeField] Button _winButton;
        [SerializeField] Button _loseButton;

        [Header("Rewards")]
        [Tooltip("Hidden when the battle paid out nothing, such as after a loss.")]
        [SerializeField] GameObject _rewardsSection;
        [SerializeField] RectTransform _rewardsContainer;
        [SerializeField] UIEndPanelRewardIndicator _rewardIndicatorPrefab;

        [SerializeField] GameObject _levelUpSection;
        [SerializeField] TextMeshProUGUI _levelUpText;
        [SerializeField] TextMeshProUGUI _statPointsText;

        readonly List<UIEndPanelRewardIndicator> _spawnedRewards = new List<UIEndPanelRewardIndicator>();
        [SerializeField] public RewardToIcon[] spriteTable;

        void Awake()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            if (_rewardsContainer == null && _rewardsSection != null)
                _rewardsContainer = _rewardsSection.transform as RectTransform;

            WireCloseButtons();
            Hide();
        }

        void OnValidate()
        {
            if (_winButton == null && _winSection != null)
                _winButton = _winSection.GetComponentInChildren<Button>(true);

            if (_loseButton == null && _loseSection != null)
                _loseButton = _loseSection.GetComponentInChildren<Button>(true);

            if (_rewardsContainer == null && _rewardsSection != null)
                _rewardsContainer = _rewardsSection.transform as RectTransform;
        }

        public void Show(BattleOutcome outcome)
        {
            Show(outcome, BattleRewardResult.None);
        }

        public void Show(BattleOutcome outcome, BattleRewardResult rewards)
        {
            if (_winSection != null)
                _winSection.SetActive(outcome == BattleOutcome.Win);

            if (_loseSection != null)
                _loseSection.SetActive(outcome == BattleOutcome.Lose);

            ShowRewards(rewards);

            if (_panelRoot != null)
                _panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        void ShowRewards(BattleRewardResult rewards)
        {
            ClearSpawnedRewards();

            if (_rewardsSection != null)
                _rewardsSection.SetActive(rewards.HasRewards);

            if (rewards.HasRewards)
                PopulateRewardIndicators(rewards);

            if (_levelUpSection != null)
                _levelUpSection.SetActive(rewards.LeveledUp);

            if (_levelUpText != null)
                _levelUpText.text = $"Level {rewards.LevelAfter}";

            if (_statPointsText != null)
                _statPointsText.text = $"+{rewards.StatPointsGained} stat points";
        }

        void PopulateRewardIndicators(BattleRewardResult rewards)
        {
            if (_rewardIndicatorPrefab == null || _rewardsContainer == null)
            {
                Debug.LogError($"{nameof(UIEndBattlePanel)}: assign {nameof(_rewardIndicatorPrefab)} and {nameof(_rewardsContainer)}.", this);
                return;
            }

            // Experience and shards come from BattleSessionRewards, banked into BattleRewardResult on a win.
            if (rewards.ExperienceGained > 0)
                SpawnReward(GetSprite(ERewardType.Exp), rewards.ExperienceGained, "EXP");

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            IReadOnlyList<ShardAmount> shards = rewards.ShardsGained;

            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i].Amount <= 0)
                    continue;

                SpawnReward(ResolveShardIcon(config, shards[i].TileType), shards[i].Amount, shards[i].TileType);
            }
        }

        void SpawnReward(Sprite icon, int amount, string nameSuffix)
        {
            UIEndPanelRewardIndicator indicator = Instantiate(_rewardIndicatorPrefab, _rewardsContainer);
            indicator.name = $"Reward_{nameSuffix}";
            indicator.Configure(icon, amount);
            _spawnedRewards.Add(indicator);
        }

        static Sprite ResolveShardIcon(GameConfig config, string tileTypeKey)
        {
            if (config == null || string.IsNullOrEmpty(tileTypeKey))
                return null;

            int typeId = config.GetTileTypeIdByKey(tileTypeKey);
            return typeId >= 0 ? config.GetTileTypeShardIcon(typeId) : null;
        }

        void ClearSpawnedRewards()
        {
            for (int i = 0; i < _spawnedRewards.Count; i++)
            {
                if (_spawnedRewards[i] != null)
                    Destroy(_spawnedRewards[i].gameObject);
            }

            _spawnedRewards.Clear();
        }

        public void Hide()
        {
            ClearSpawnedRewards();

            if (_panelRoot != null)
                _panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        void WireCloseButtons()
        {
            WireCloseButton(_winButton);
            WireCloseButton(_loseButton);
        }

        void WireCloseButton(Button button)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(HandleCloseClicked);
            button.onClick.AddListener(HandleCloseClicked);
        }

        void HandleCloseClicked()
        {
            Hide();
            BattleManager.Instance?.DismissEndBattlePanel();
        }

        void OnDestroy()
        {
            if (_winButton != null)
                _winButton.onClick.RemoveListener(HandleCloseClicked);

            if (_loseButton != null)
                _loseButton.onClick.RemoveListener(HandleCloseClicked);
        }


        public Sprite GetSprite(ERewardType rewardType)
        {
            Sprite sprite = null;
            for (int i = 0; i < spriteTable.Length; i++)
            {
                if (rewardType == spriteTable[i].rewardType)
                {
                    return spriteTable[i].spr;
                }
            }
            return sprite;
        }
        [System.Serializable]
        public struct RewardToIcon
        {
            public ERewardType rewardType;
            public Sprite spr;
        }
    }
}
