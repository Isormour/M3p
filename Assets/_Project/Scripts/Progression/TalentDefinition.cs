using UnityEngine;

namespace M3P
{
    /// <summary>
    /// A pick offered when a stat reaches a milestone. Talents are keyed by stat and tier so the profile
    /// can remember which branch the player chose at 5, 10, 15 and so on.
    /// </summary>
    [CreateAssetMenu(fileName = "TalentDefinition", menuName = "M3P/Talent Definition", order = 4)]
    public class TalentDefinition : ScriptableObject
    {
        [SerializeField] string _displayName;
        [SerializeField] EStatType _stat;
        [Tooltip("1 = first milestone (5 points), 2 = second (10 points), and so on.")]
        [Min(1), SerializeField] int _milestoneTier = 1;

        [Header("Bonuses")]
        [SerializeField] int _bonusMaxHp;
        [SerializeField] float _bonusPhysicalDamagePercent;
        [SerializeField] int _bonusMaxHandSize;
        [SerializeField] int _bonusMaxActionPoints;
        [SerializeField] float _bonusMagicEffectPercent;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public EStatType Stat => _stat;
        public int MilestoneTier => Mathf.Max(1, _milestoneTier);

        public TalentBonuses ToBonuses()
        {
            return new TalentBonuses
            {
                MaxHp = _bonusMaxHp,
                MaxActionPoints = _bonusMaxActionPoints,
                MaxHandSize = _bonusMaxHandSize,
                PhysicalDamagePercent = _bonusPhysicalDamagePercent,
                MagicEffectPercent = _bonusMagicEffectPercent,
            };
        }
    }
}
