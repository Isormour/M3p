using System;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring-side mana cost. References the tile type asset directly so reordering
    /// <see cref="GameConfig.TileTypes"/> cannot silently remap a cost onto another colour.
    /// </summary>
    [Serializable]
    public struct TileTypeManaCost
    {
        public Match3TileTypeDefinition TileType;
        public int Amount;

        public TileTypeManaCost(Match3TileTypeDefinition tileType, int amount = 0)
        {
            TileType = tileType;
            Amount = amount;
        }
    }

    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "M3P/Skill Definition", order = 0)]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] TileTypeManaCost[] _manaCosts = Array.Empty<TileTypeManaCost>();
        [SerializeField] BattleEffect[] _effects = Array.Empty<BattleEffect>();
        [field: SerializeField] public string _animationName { private set; get; } = "BasicAttack";

        [NonSerialized] bool _loggedUnresolvedTileType;

        public TileTypeManaCost[] ManaCosts => _manaCosts ?? Array.Empty<TileTypeManaCost>();

        public BattleEffect[] Effects => _effects ?? Array.Empty<BattleEffect>();

        public int GetManaCostForTileType(Match3TileTypeDefinition tileType)
        {
            if (tileType == null)
                return 0;

            TileTypeManaCost[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].TileType == tileType)
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

            TileTypeManaCost[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0)
                    continue;

                int tileTypeId = ResolveTileTypeId(costs[i].TileType);
                if (tileTypeId < 0)
                    return false;

                if (softStats.GetManaForTileType(tileTypeId) < costs[i].Amount)
                    return false;
            }

            return true;
        }

        public bool TrySpendMana(SoftStats softStats)
        {
            if (!HasEnoughMana(softStats))
                return false;

            TileTypeManaCost[] costs = ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Amount <= 0)
                    continue;

                int tileTypeId = ResolveTileTypeId(costs[i].TileType);
                int remaining = softStats.GetManaForTileType(tileTypeId) - costs[i].Amount;
                softStats.SetManaForTileType(tileTypeId, remaining);
            }

            return true;
        }

        /// <summary>
        /// Maps an authored tile type onto the runtime id used by the board, or -1 when it cannot be resolved.
        /// Logs at most once per asset because this runs from per-frame UI refreshes.
        /// </summary>
        int ResolveTileTypeId(Match3TileTypeDefinition tileType)
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return -1;

            int tileTypeId = config.GetTileTypeId(tileType);
            if (tileTypeId < 0 && !_loggedUnresolvedTileType)
            {
                _loggedUnresolvedTileType = true;
                string tileTypeName = tileType != null ? tileType.name : "<none>";
                Debug.LogError(
                    $"{nameof(SkillDefinition)} '{name}': mana cost tile type '{tileTypeName}' is missing from {nameof(GameConfig)}. The skill can never be cast.",
                    this);
            }

            return tileTypeId;
        }

        void OnEnable()
        {
            _loggedUnresolvedTileType = false;
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
