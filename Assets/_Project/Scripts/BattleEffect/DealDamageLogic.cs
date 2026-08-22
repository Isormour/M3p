using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class DealDamageLogic : BattleEffectLogic
    {
        [SerializeField] int _amount;
        [Tooltip("When true, Strength scales the hit. When false, Intelligence scales it as a magic effect.")]
        [SerializeField] bool _physical;

        public int Amount => _amount;
        public bool Physical => _physical;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SkillCombat.DealScaledDamage(context, target, _amount, _physical);
        }
    }
}
