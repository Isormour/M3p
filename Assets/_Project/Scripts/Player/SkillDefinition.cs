using System;
using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "M3P/Skill Definition", order = 0)]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] int _skillId;
        [SerializeField] TileTypeMana[] _manaCosts = Array.Empty<TileTypeMana>();
        [SerializeField] BattleEffect[] _effects = Array.Empty<BattleEffect>();
        [field: SerializeField] public string _animationName { private set; get; } = "BasicAttack";
        public int SkillId => _skillId;

        public TileTypeMana[] ManaCosts => _manaCosts ?? Array.Empty<TileTypeMana>();

        public BattleEffect[] Effects => _effects ?? Array.Empty<BattleEffect>();

        public int GetManaCostForTileType(int tileTypeId)
        {
            TileTypeMana[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].TileTypeId == tileTypeId)
                    return costs[i].Amount;
            }

            return 0;
        }

        public const int DefaultActionPointCost = 1;

        public bool HasEnoughActionPoints(SoftStats softStats)
        {
            return softStats != null && softStats.HasActionPoints(DefaultActionPointCost);
        }

        public bool TrySpendActionPoints(SoftStats softStats)
        {
            return softStats != null && softStats.TrySpendActionPoint(DefaultActionPointCost);
        }

        public bool HasEnoughMana(SoftStats softStats)
        {
            if (softStats == null)
                return false;

            TileTypeMana[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0)
                    continue;

                if (softStats.GetManaForTileType(costs[i].TileTypeId) < costs[i].Amount)
                    return false;
            }

            return true;
        }

        public bool TrySpendMana(SoftStats softStats)
        {
            if (!HasEnoughMana(softStats))
                return false;

            TileTypeMana[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0)
                    continue;

                int remaining = softStats.GetManaForTileType(costs[i].TileTypeId) - costs[i].Amount;
                softStats.SetManaForTileType(costs[i].TileTypeId, remaining);
            }

            return true;
        }

        public void UseSkill(BattleCharacter caster, BattleCharacter target)
        {
            BattleEffect[] effects = Effects;
            if (effects.Length == 0)
                return;

            var context = new BattleEffectContext(caster, target);
            for (int i = 0; i < effects.Length; i++)
                effects[i]?.Apply(context);
        }
    }
}
