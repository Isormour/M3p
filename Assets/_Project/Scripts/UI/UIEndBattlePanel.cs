using System.Collections;
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

        [Header("Experience")]
        [SerializeField] UIPanelPlayerStats StatsPanel;
        [SerializeField] UIPanelStatsSkillFillBar experienceBar;
        [Tooltip("Seconds to fill one full experience bar. Partial gains take a matching slice of this.")]
        [Min(0.05f), SerializeField] float _experienceFillDuration = 0.9f;
        [Tooltip("Hold at a full bar before wrapping into the next level.")]
        [Min(0f), SerializeField] float _experienceLevelPause = 0.15f;

        Coroutine _experienceFillRoutine;

        void Awake()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            if (_rewardsContainer == null && _rewardsSection != null)
                _rewardsContainer = _rewardsSection.transform as RectTransform;

            if (experienceBar == null)
                experienceBar = GetComponentInChildren<UIPanelStatsSkillFillBar>(true);

            if (StatsPanel == null)
                StatsPanel = GetComponentInChildren<UIPanelPlayerStats>(true);

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

            if (experienceBar == null)
                experienceBar = GetComponentInChildren<UIPanelStatsSkillFillBar>(true);

            if (StatsPanel == null)
                StatsPanel = GetComponentInChildren<UIPanelPlayerStats>(true);
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
            HideStatsPanel();

            // The panel GameObject is often the root, so it must be active before the fill coroutine starts.
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);

            PlayExperienceBar(rewards);
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
            if (this == null)
                return;

            StopExperienceFill();
            HideStatsPanel();
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
            StopExperienceFill();

            if (_winButton != null)
                _winButton.onClick.RemoveListener(HandleCloseClicked);

            if (_loseButton != null)
                _loseButton.onClick.RemoveListener(HandleCloseClicked);
        }

        void PlayExperienceBar(BattleRewardResult rewards)
        {
            StopExperienceFill();

            LevelProgressionConfig curve = GameManager.Instance != null
                ? GameManager.Instance.Progression.LevelProgression
                : null;

            if (experienceBar == null ||
                !TryResolveExperienceSpan(rewards, curve, out int startTotal, out int endTotal))
            {
                if (experienceBar != null)
                    experienceBar.SetFill(0f);

                if (rewards.LeveledUp)
                    ShowStatsPanel();
                return;
            }

            experienceBar.SetFill(NormalizedFill(curve, startTotal));
            if (endTotal <= startTotal)
            {
                if (rewards.LeveledUp)
                    ShowStatsPanel();
                return;
            }

            _experienceFillRoutine = StartCoroutine(FillExperienceBarRoutine(curve, startTotal, endTotal));
        }

        static bool TryResolveExperienceSpan(
            BattleRewardResult rewards,
            LevelProgressionConfig curve,
            out int startTotal,
            out int endTotal)
        {
            startTotal = 0;
            endTotal = 0;
            if (curve == null)
                return false;

            if (rewards.ExperienceGained > 0)
            {
                endTotal = rewards.TotalExperience;
                startTotal = Mathf.Max(0, rewards.TotalExperience - rewards.ExperienceGained);
                return true;
            }

            PlayerProfile profile = GameManager.Instance != null
                ? GameManager.Instance.ProfileManager.CurrentProfile
                : null;
            if (profile == null)
                return false;

            startTotal = endTotal = profile.Experience;
            return true;
        }

        IEnumerator FillExperienceBarRoutine(LevelProgressionConfig curve, int startTotal, int endTotal)
        {
            int xp = startTotal;
            while (xp < endTotal)
            {
                int level = curve.GetLevelForTotalExperience(xp);
                int into = curve.GetExperienceIntoLevel(xp, level);
                int cost = curve.GetExperienceToAdvance(level);
                if (cost <= 0)
                {
                    experienceBar.SetFill(1f);
                    yield break;
                }

                int remainingInLevel = cost - into;
                int gainedThisSegment = Mathf.Min(remainingInLevel, endTotal - xp);
                float from = into / (float)cost;
                float to = (into + gainedThisSegment) / (float)cost;
                float duration = Mathf.Max(0.15f, _experienceFillDuration * Mathf.Max(0.05f, to - from));

                yield return AnimateExperienceFill(from, to, duration);

                xp += gainedThisSegment;
                if (gainedThisSegment < remainingInLevel)
                    break;

                ShowStatsPanel();

                if (_experienceLevelPause > 0f)
                    yield return new WaitForSeconds(_experienceLevelPause);

                experienceBar.SetFill(NormalizedFill(curve, xp));
            }

            experienceBar.SetFill(NormalizedFill(curve, endTotal));
            _experienceFillRoutine = null;
        }

        IEnumerator AnimateExperienceFill(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                experienceBar.SetFill(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                experienceBar.SetFill(Mathf.Lerp(from, to, t));
                yield return null;
            }

            experienceBar.SetFill(to);
        }

        static float NormalizedFill(LevelProgressionConfig curve, int totalExperience)
        {
            int level = curve.GetLevelForTotalExperience(totalExperience);
            int cost = curve.GetExperienceToAdvance(level);
            if (cost <= 0)
                return 1f;

            return curve.GetExperienceIntoLevel(totalExperience, level) / (float)cost;
        }

        void ShowStatsPanel()
        {
            if (StatsPanel == null)
                return;

            StatsPanel.Show();
        }

        void HideStatsPanel()
        {
            if (StatsPanel == null)
                return;

            StatsPanel.Hide();
        }

        void StopExperienceFill()
        {
            if (_experienceFillRoutine == null)
                return;

            StopCoroutine(_experienceFillRoutine);
            _experienceFillRoutine = null;
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
