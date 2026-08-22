using System;
using UnityEngine;

namespace M3P
{
    public enum EEffectTarget
    {
        None = 0,
        Caster,
        Opponent,
        Tile
    }

    public enum EEffectSource
    {
        None = 0,
        Player,
        Enemy,
        Companion,
        Tile
    }

    public readonly struct BattleEffectContext
    {
        public BattleCharacter Caster { get; }
        public BattleCharacter Target { get; }
        public EEffectSource Source { get; }
        public bool DirectHit { get; }
        public float SkillDamageMultiplier { get; }
        public int ChoicePrimary { get; }
        public int ChoiceSecondary { get; }

        public BattleEffectContext(
            BattleCharacter caster,
            BattleCharacter target,
            bool directHit = false,
            float skillDamageMultiplier = 1f,
            int choicePrimary = 0,
            int choiceSecondary = 0)
        {
            Caster = caster;
            Target = target;
            Source = caster != null ? caster.EffectSource : EEffectSource.None;
            DirectHit = directHit;
            SkillDamageMultiplier = skillDamageMultiplier > 0f ? skillDamageMultiplier : 1f;
            ChoicePrimary = choicePrimary;
            ChoiceSecondary = choiceSecondary;
        }

        public BattleCharacter Resolve(EEffectTarget effectTarget)
        {
            return effectTarget switch
            {
                EEffectTarget.Caster => Caster,
                EEffectTarget.Opponent => Target,
                _ => null
            };
        }
    }

    [CreateAssetMenu(fileName = "BattleEffect", menuName = "M3P/Battle Effect", order = 10)]
    public class BattleEffect : ScriptableObject
    {
        [SerializeField] EEffectTarget _target = EEffectTarget.Opponent;
        [SerializeField, SerializeReference] BattleEffectLogic _logic;

        public EEffectTarget Target => _target;
        public BattleEffectLogic Logic => _logic;

        public void Apply(BattleEffectContext context)
        {
            _logic?.Apply(context, _target);
        }
    }
}
