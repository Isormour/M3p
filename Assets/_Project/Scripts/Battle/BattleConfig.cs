using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Tuning for basic attacks: match length, Strength, supermatch size and extra cascade hits.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleConfig", menuName = "M3P/Battle Config", order = 1)]
    public class BattleConfig : ScriptableObject
    {
        public const int MinSupermatchSize = 3;
        public const int MaxSupermatchSize = 10;

        [Header("Basic Attack")]
        [Tooltip("Flat damage before Strength scaling and match-length bonus.")]
        [SerializeField] int _basePhysicalDamage = 1;

        [Tooltip("Damage added per matched tile above the minimum-2 threshold, so a match of 3 scores one step.")]
        [SerializeField] int _damagePerMatchedTile = 1;

        [Tooltip("Physical damage multiplier added per Strength point. 0.05 means +5% per point.")]
        [Min(0f), SerializeField] float _damagePerStrength = 0.05f;

        [Tooltip("Bonus damage for match sizes 3, 4, 5, 6, 7, 8, 9, 10.")]
        [SerializeField] int[] _damagePerSupermatchSize = { 0, 0, 0, 0, 0, 0, 0, 0 };

        [Tooltip("Extra basic attacks launched on each cascade wave (wave 2+).")]
        [Min(0), SerializeField] int _additionalAttackPerCascade = 1;

        public int BasePhysicalDamage => _basePhysicalDamage;
        public int DamagePerMatchedTile => _damagePerMatchedTile;
        public float DamagePerStrength => _damagePerStrength;
        public int AdditionalAttackPerCascade => _additionalAttackPerCascade;

        public static BattleConfig CreateDefault()
        {
            BattleConfig config = CreateInstance<BattleConfig>();
            config.name = "BattleConfig (Default)";
            config.hideFlags = HideFlags.HideAndDontSave;
            config.EnsureSupermatchArray();
            return config;
        }

        public int GetDamagePerSupermatchSize(int matchSize)
        {
            EnsureSupermatchArray();
            if (matchSize < MinSupermatchSize)
                return 0;

            int index = Mathf.Min(matchSize, MaxSupermatchSize) - MinSupermatchSize;
            return _damagePerSupermatchSize[index];
        }

        public int GetExtraAttacksForWave(int waveIndex)
        {
            if (waveIndex < 2 || _additionalAttackPerCascade <= 0)
                return 0;

            return _additionalAttackPerCascade;
        }

        /// <summary>
        /// Damage of a single basic attack. One attack fires per match group, and cascade waves
        /// can add more via <see cref="AdditionalAttackPerCascade"/>.
        /// </summary>
        public int CalculateBasicAttackDamage(HardStats attacker, int matchSize, TalentBonuses talents = default)
        {
            int extraTiles = Mathf.Max(0, matchSize - (Match3Board.MinimumMatchSize - 1));
            int raw = _basePhysicalDamage
                + _damagePerMatchedTile * extraTiles
                + GetDamagePerSupermatchSize(matchSize);

            float multiplier = 1f + attacker.Strength * _damagePerStrength + talents.PhysicalDamagePercent;
            return Mathf.Max(1, Mathf.RoundToInt(raw * multiplier));
        }

        void OnValidate()
        {
            EnsureSupermatchArray();
        }

        void EnsureSupermatchArray()
        {
            int length = MaxSupermatchSize - MinSupermatchSize + 1;
            if (_damagePerSupermatchSize != null && _damagePerSupermatchSize.Length == length)
                return;

            int[] resized = new int[length];
            if (_damagePerSupermatchSize != null)
            {
                int copy = Mathf.Min(_damagePerSupermatchSize.Length, length);
                for (int i = 0; i < copy; i++)
                    resized[i] = _damagePerSupermatchSize[i];
            }

            _damagePerSupermatchSize = resized;
        }
    }
}
