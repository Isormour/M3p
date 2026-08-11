using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Turns battle results into character progression: banking experience, paying out the levels it
    /// crossed and spending the points those levels granted. Storage belongs to <see cref="ProfileManager"/>.
    /// </summary>
    public sealed class ProgressionService
    {
        readonly GameConfig _config;
        readonly ProfileManager _profiles;

        LevelProgressionConfig _fallbackLevelProgression;

        public ProgressionService(GameConfig config, ProfileManager profiles)
        {
            _config = config;
            _profiles = profiles;
        }

        public LevelProgressionConfig LevelProgression =>
            _config != null && _config.LevelProgression != null
                ? _config.LevelProgression
                : _fallbackLevelProgression ??= LevelProgressionConfig.CreateDefault();

        public StatProgressionConfig StatProgression =>
            _config != null ? _config.StatProgression : StatProgressionConfig.CreateDefault();

        public TalentConfig Talents => _config != null ? _config.Talents : null;

        /// <summary>
        /// Adds what a won battle produced and pays out any levels it crossed, then commits the result.
        /// Callers only pass a reward for a win, so a fight the player is losing is never worth
        /// dragging out.
        /// </summary>
        public BattleRewardResult ApplyBattleRewards(int experience, IReadOnlyDictionary<int, int> shardsByTileType = null)
        {
            PlayerProfile profile = _profiles.CurrentProfile;
            LevelProgressionConfig levelProgression = LevelProgression;
            experience = Mathf.Max(0, experience);

            int levelBefore = profile.Level;
            profile.Experience += experience;

            // Never walk a level back: a retuned curve must not take away points already spent.
            int levelAfter = Mathf.Max(levelBefore, levelProgression.GetLevelForTotalExperience(profile.Experience));
            int statPoints = (levelAfter - levelBefore) * levelProgression.StatPointsPerLevel;

            profile.Level = levelAfter;
            profile.UnspentStatPoints += statPoints;

            ShardAmount[] shardsGained = BankShards(profile, shardsByTileType);

            _profiles.Save();

            return new BattleRewardResult(
                experience,
                statPoints,
                levelBefore,
                levelAfter,
                profile.Experience,
                levelProgression.GetExperienceIntoLevel(profile.Experience, levelAfter),
                levelProgression.GetExperienceToAdvance(levelAfter),
                shardsGained);
        }

        /// <summary>
        /// Moves a battle's shards into the wallet, translating the board's runtime tile ids into the
        /// names the save keys on.
        /// </summary>
        ShardAmount[] BankShards(PlayerProfile profile, IReadOnlyDictionary<int, int> shardsByTileType)
        {
            if (shardsByTileType == null || shardsByTileType.Count == 0)
                return Array.Empty<ShardAmount>();

            List<ShardAmount> granted = new List<ShardAmount>(shardsByTileType.Count);

            foreach (KeyValuePair<int, int> entry in shardsByTileType)
            {
                if (entry.Value <= 0)
                    continue;

                string tileType = _config != null ? _config.GetTileTypeKey(entry.Key) : null;
                if (string.IsNullOrEmpty(tileType))
                {
                    Debug.LogWarning($"{nameof(ProgressionService)}: dropping {entry.Value} shards, tile type {entry.Key} is not in {nameof(GameConfig)}.");
                    continue;
                }

                profile.AddShards(tileType, entry.Value);
                granted.Add(new ShardAmount(tileType, entry.Value));
            }

            return granted.ToArray();
        }

        public bool TryAllocateStatPoint(EStatType stat) => TryAllocateStatPoints(stat, 1);

        /// <summary>
        /// Spends several points on one stat as a single transaction, so a full allocation writes the
        /// save once instead of once per point. Spends nothing unless every point is affordable.
        /// </summary>
        public bool TryAllocateStatPoints(EStatType stat, int points)
        {
            if (points <= 0)
                return false;

            PlayerProfile profile = _profiles.CurrentProfile;
            if (profile.UnspentStatPoints < points)
                return false;

            int valueBefore = profile.HardStats.Get(stat);

            for (int i = 0; i < points; i++)
                profile.TrySpendStatPoint(stat);

            UpdatePendingTalentAfterAllocation(profile, stat, valueBefore, profile.HardStats.Get(stat));

            _profiles.Save();
            return true;
        }

        /// <summary>Commits a talent pick for the profile's pending milestone.</summary>
        public bool TryChooseTalent(int talentId)
        {
            PlayerProfile profile = _profiles.CurrentProfile;
            TalentConfig talents = Talents;

            if (!profile.TryUnlockTalent(talentId, talents))
                return false;

            _profiles.Save();
            return true;
        }

        void UpdatePendingTalentAfterAllocation(
            PlayerProfile profile,
            EStatType stat,
            int valueBefore,
            int valueAfter)
        {
            StatProgressionConfig progression = StatProgression;
            TalentConfig talents = Talents;
            int interval = progression.MilestoneInterval;

            for (int value = valueBefore + 1; value <= valueAfter; value++)
            {
                if (value % interval != 0)
                    continue;

                int tier = value / interval;
                if (talents != null && profile.HasTalentForMilestone(stat, tier, talents))
                    continue;

                profile.PendingTalent = new PendingTalentChoice(stat, tier);
                return;
            }
        }
    }
}
