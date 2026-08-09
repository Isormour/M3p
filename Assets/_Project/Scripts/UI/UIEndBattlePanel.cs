using System.Collections.Generic;
using System.Text;
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
        [SerializeField] TextMeshProUGUI _experienceText;
        [SerializeField] GameObject _levelUpSection;
        [SerializeField] TextMeshProUGUI _levelUpText;
        [SerializeField] TextMeshProUGUI _statPointsText;

        [Tooltip("Hidden when no match was long enough to drop shards.")]
        [SerializeField] GameObject _shardsSection;
        [SerializeField] TextMeshProUGUI _shardsText;

        void Awake()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            WireCloseButtons();
            Hide();
        }

        void OnValidate()
        {
            if (_winButton == null && _winSection != null)
                _winButton = _winSection.GetComponentInChildren<Button>(true);

            if (_loseButton == null && _loseSection != null)
                _loseButton = _loseSection.GetComponentInChildren<Button>(true);
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
            if (_rewardsSection != null)
                _rewardsSection.SetActive(rewards.HasRewards);

            if (_experienceText != null)
                _experienceText.text = $"+{rewards.ExperienceGained} EXP";

            if (_levelUpSection != null)
                _levelUpSection.SetActive(rewards.LeveledUp);

            if (_levelUpText != null)
                _levelUpText.text = $"Level {rewards.LevelAfter}";

            if (_statPointsText != null)
                _statPointsText.text = $"+{rewards.StatPointsGained} stat points";

            int shardsGained = rewards.TotalShardsGained;

            if (_shardsSection != null)
                _shardsSection.SetActive(shardsGained > 0);

            if (_shardsText != null && shardsGained > 0)
                _shardsText.text = FormatShards(rewards.ShardsGained);
        }

        static string FormatShards(IReadOnlyList<ShardAmount> shards)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i].Amount <= 0)
                    continue;

                if (builder.Length > 0)
                    builder.Append("   ");

                builder.Append(shards[i].TileType).Append(" x").Append(shards[i].Amount);
            }

            return builder.ToString();
        }

        public void Hide()
        {
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
    }
}
