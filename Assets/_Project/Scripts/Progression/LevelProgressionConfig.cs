using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The experience curve for character levels. Each level costs one increment more than the level
    /// before it, so early levels arrive fast enough to teach the loop while later ones stay meaningful.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelProgressionConfig", menuName = "M3P/Level Progression Config", order = 1)]
    public class LevelProgressionConfig : ScriptableObject
    {
        /// <summary>Level a brand new profile starts at.</summary>
        public const int FirstLevel = 1;

        [Tooltip("Highest reachable level. Experience past the cap is still recorded but grants nothing.")]
        [Min(FirstLevel), SerializeField] int _levelCap = 20;

        [Tooltip("Experience needed to go from the first level to the second.")]
        [Min(1), SerializeField] int _baseExperiencePerLevel = 100;

        [Tooltip("Added to the cost of every level after the first.")]
        [Min(0), SerializeField] int _experienceIncreasePerLevel = 100;

        [Tooltip("Stat points granted per level gained.")]
        [Min(0), SerializeField] int _statPointsPerLevel = 1;

        public int LevelCap => Mathf.Max(FirstLevel, _levelCap);

        public int StatPointsPerLevel => Mathf.Max(0, _statPointsPerLevel);

        /// <summary>Stand-in used when no asset is assigned, so a scene still runs with sane values.</summary>
        public static LevelProgressionConfig CreateDefault()
        {
            LevelProgressionConfig config = CreateInstance<LevelProgressionConfig>();
            config.name = "LevelProgressionConfig (Default)";
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }

        /// <summary>Experience that buys the step from <paramref name="level"/> to the next one, or 0 at the cap.</summary>
        public int GetExperienceToAdvance(int level)
        {
            if (level < FirstLevel)
                level = FirstLevel;

            if (level >= LevelCap)
                return 0;

            return _baseExperiencePerLevel + _experienceIncreasePerLevel * (level - FirstLevel);
        }

        /// <summary>Lifetime experience a character needs to sit exactly at <paramref name="level"/>.</summary>
        public int GetTotalExperienceForLevel(int level)
        {
            int total = 0;
            for (int step = FirstLevel; step < level; step++)
                total += GetExperienceToAdvance(step);

            return total;
        }

        public int GetLevelForTotalExperience(int totalExperience)
        {
            int level = FirstLevel;
            int remaining = Mathf.Max(0, totalExperience);

            while (level < LevelCap)
            {
                int cost = GetExperienceToAdvance(level);
                if (cost <= 0 || remaining < cost)
                    break;

                remaining -= cost;
                level++;
            }

            return level;
        }

        /// <summary>Progress inside the current level, for a bar that fills between two levels.</summary>
        public int GetExperienceIntoLevel(int totalExperience, int level)
        {
            return Mathf.Max(0, totalExperience - GetTotalExperienceForLevel(level));
        }
    }
}
