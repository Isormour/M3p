using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Converts cleared matches into shards. Kept separate from damage tuning because the shard economy
    /// is balanced against crafting costs rather than against combat pacing.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchRewardRules", menuName = "M3P/Match Reward Rules", order = 3)]
    public class MatchRewardRules : ScriptableObject
    {
        [Tooltip("Tiles a line may contain before it starts paying shards. At 3, a line of 4 yields 1 shard.")]
        [Min(0)]
        [SerializeField] int _freeTilesPerMatch = 3;

        /// <summary>
        /// Shards a single line of this length is worth. Lines at or below the free size pay nothing, and
        /// each line scores on its own, so an L or T is worth no more than its two arms are separately.
        /// </summary>
        public int GetShardsForMatch(int matchSize)
        {
            return Mathf.Max(0, matchSize - _freeTilesPerMatch);
        }

        public static MatchRewardRules CreateDefault()
        {
            return CreateInstance<MatchRewardRules>();
        }
    }
}
