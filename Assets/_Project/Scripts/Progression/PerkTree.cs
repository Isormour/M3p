using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One spendable perk ladder. Profiles store points against <see cref="Id"/> so more trees
    /// can be added without changing the save shape.
    /// </summary>
    [CreateAssetMenu(fileName = "PerkTree", menuName = "M3P/Perk Tree", order = 3)]
    public class PerkTree : ScriptableObject
    {
        public const int InvalidId = 0;

        [SerializeField] string Nazwa;
        [SerializeField] int id;
        [SerializeField] StatPerk[] perks = Array.Empty<StatPerk>();
        [SerializeReference] TreeStatBonusLogic TreeStatBonusLogic;
        [SerializeField] Sprite _icon;

        public string DisplayName => string.IsNullOrEmpty(Nazwa) ? name : Nazwa;
        public int Id => id;
        public StatPerk[] Perks => perks ?? Array.Empty<StatPerk>();
        public TreeStatBonusLogic StatBonus => TreeStatBonusLogic;
        public Sprite Icon => _icon;

        public bool TryGetGrantedStat(out EStatType stat)
        {
            if (TreeStatBonusLogic != null)
                return TreeStatBonusLogic.TryGetGrantedStat(out stat);

            stat = default;
            return false;
        }

        public int GetProgressionLevel(HardStats hard)
        {
            return TreeStatBonusLogic != null ? TreeStatBonusLogic.GetProgressionLevel(hard) : 0;
        }

        public void ApplyStatBonus(int treeValue, ref HardStats hard)
        {
            TreeStatBonusLogic?.Apply(treeValue, ref hard);
        }
    }
}
