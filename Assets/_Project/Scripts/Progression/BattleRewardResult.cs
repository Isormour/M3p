using System;
using System.Collections.Generic;

namespace M3P
{
    /// <summary>
    /// What a finished battle paid out and where it left the profile. Produced by
    /// <see cref="ProgressionService.ApplyBattleRewards"/> and read by the end-of-battle UI.
    /// </summary>
    public readonly struct BattleRewardResult
    {
        /// <summary>An unrewarded battle, such as a loss.</summary>
        public static readonly BattleRewardResult None = default;

        readonly ShardAmount[] _shardsGained;

        public readonly int ExperienceGained;
        public readonly int StatPointsGained;
        public readonly int LevelBefore;
        public readonly int LevelAfter;
        public readonly int TotalExperience;
        public readonly int ExperienceIntoLevel;
        public readonly int ExperienceToAdvance;

        public BattleRewardResult(
            int experienceGained,
            int statPointsGained,
            int levelBefore,
            int levelAfter,
            int totalExperience,
            int experienceIntoLevel,
            int experienceToAdvance,
            ShardAmount[] shardsGained)
        {
            ExperienceGained = experienceGained;
            StatPointsGained = statPointsGained;
            LevelBefore = levelBefore;
            LevelAfter = levelAfter;
            TotalExperience = totalExperience;
            ExperienceIntoLevel = experienceIntoLevel;
            ExperienceToAdvance = experienceToAdvance;
            _shardsGained = shardsGained;
        }

        /// <summary>Shards banked by this battle, one entry per colour.</summary>
        public IReadOnlyList<ShardAmount> ShardsGained => _shardsGained ?? Array.Empty<ShardAmount>();

        public int TotalShardsGained
        {
            get
            {
                if (_shardsGained == null)
                    return 0;

                int total = 0;
                for (int i = 0; i < _shardsGained.Length; i++)
                    total += _shardsGained[i].Amount;

                return total;
            }
        }

        public bool LeveledUp => LevelAfter > LevelBefore;

        public bool HasRewards => ExperienceGained > 0 || TotalShardsGained > 0;

        /// <summary>False once the character has nothing left to level into.</summary>
        public bool HasNextLevel => ExperienceToAdvance > 0;
    }
}
