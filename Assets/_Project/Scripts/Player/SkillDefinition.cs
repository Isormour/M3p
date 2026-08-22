using Match3;
using System;
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
        [field: SerializeField] public string _skillName { private set; get; } = "Skill Name";
        [TextArea, SerializeField] string _description;
        [SerializeField] Sprite _artwork;
        [SerializeField] TileTypeManaCost[] _manaCosts = Array.Empty<TileTypeManaCost>();
        [SerializeField] BattleEffect[] _effects = Array.Empty<BattleEffect>();
        [Tooltip("Turns before this skill can be cast again. Zero means it can be reused immediately.")]
        [Min(0), SerializeField] int _cooldown;
        [Tooltip("When true, the skill can be used only once during the caster's turn.")]
        [SerializeField] bool _oncePerTurn;
        [Tooltip("Spend 1 mana from this many different colours, on top of any authored costs.")]
        [Min(0), SerializeField] int _distinctColorManaCost;
        [SerializeField] SkillCastPrompt _castPrompt;
        [Tooltip("When true, Unyielding-style buffs multiply this skill's damage.")]
        [SerializeField] bool _physicalSkill;
        [SerializeField] SkillArchetype _archetype;
        [field: SerializeField] public string _animationName { private set; get; } = "BasicAttack";

        [NonSerialized] bool _loggedUnresolvedTileType;

        public string DisplayName =>
            string.IsNullOrEmpty(_skillName) || _skillName == "Skill Name" ? name : _skillName;

        public string Description => _description ?? string.Empty;

        public Sprite Artwork => _artwork;

        public TileTypeManaCost[] ManaCosts => _manaCosts ?? Array.Empty<TileTypeManaCost>();

        public BattleEffect[] Effects => _effects ?? Array.Empty<BattleEffect>();

        /// <summary>True when any effect is aimed at the opponent rather than the caster.</summary>
        public bool AffectsOpponent()
        {
            BattleEffect[] effects = Effects;
            for (int i = 0; i < effects.Length; i++)
            {
                BattleEffect effect = effects[i];
                if (effect != null && effect.Target == EEffectTarget.Opponent)
                    return true;
            }

            return false;
        }

        /// <summary>Turns the caster must wait after using this skill before casting it again. Zero skips the lockout.</summary>
        public int Cooldown => Mathf.Max(0, _cooldown);

        public bool OncePerTurn => _oncePerTurn;

        public int DistinctColorManaCost => Mathf.Max(0, _distinctColorManaCost);

        public SkillCastPrompt CastPrompt => _castPrompt;

        public bool PhysicalSkill => _physicalSkill;

        public SkillArchetype Archetype => _archetype;

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

            return HasEnoughDistinctColorMana(softStats);
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

            if (DistinctColorManaCost <= 0)
                return true;

            var spendOrder = new System.Collections.Generic.List<int>();
            if (!SkillCombat.TryCollectDistinctColorSpendOrder(softStats, this, spendOrder, subtractAuthoredCosts: false))
                return false;

            for (int i = 0; i < spendOrder.Count; i++)
            {
                int tileTypeId = spendOrder[i];
                softStats.SetManaForTileType(tileTypeId, softStats.GetManaForTileType(tileTypeId) - 1);
            }

            return true;
        }

        bool HasEnoughDistinctColorMana(SoftStats softStats)
        {
            if (DistinctColorManaCost <= 0)
                return true;

            var spendOrder = new System.Collections.Generic.List<int>();
            return SkillCombat.TryCollectDistinctColorSpendOrder(softStats, this, spendOrder, subtractAuthoredCosts: true);
        }

        /// <summary>Extra requirements from effect logics, such as souls or a non-empty discard pile.</summary>
        public bool MeetsCastRequirements(BattleCharacter caster, BattleCharacter target)
        {
            if (caster == null)
                return false;

            if (OncePerTurn && caster.HasUsedSkillThisTurn(this))
                return false;

            BattleEffect[] effects = Effects;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i]?.Logic is ISkillCastRequirement requirement
                    && !requirement.CanCast(caster, target, this))
                    return false;
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

        public void UseSkill(BattleCharacter caster, BattleCharacter target, SkillCastChoice choice = default)
        {
            BattleEffect[] effects = Effects;
            if (effects.Length == 0)
                return;

            float multiplier = 1f;
            if (PhysicalSkill && caster != null)
                multiplier = caster.Modifiers.ConsumeNextPhysicalSkillMultiplier();

            var context = new BattleEffectContext(
                caster,
                target,
                directHit: true,
                skillDamageMultiplier: multiplier,
                choicePrimary: choice.Primary,
                choiceSecondary: choice.Secondary);

            for (int i = 0; i < effects.Length; i++)
                effects[i]?.Apply(context);

            caster?.MarkSkillUsedThisTurn(this);
        }
    }
}
