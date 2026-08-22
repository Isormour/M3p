using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Deals damage equal to the caster's current Shield, then clears that Shield.
    /// </summary>
    [Serializable]
    public class ShieldBashLogic : BattleEffectLogic
    {
        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            SoftStats casterSoft = context.Caster?.Stats?.Soft;
            BattleCharacter character = context.Resolve(target);
            SoftStats targetSoft = character?.Stats?.Soft;
            if (casterSoft == null || targetSoft == null)
                return;

            int amount = casterSoft.CurrentShield;
            if (amount <= 0)
                return;

            casterSoft.ClearShield();
            targetSoft.TakeDamage(amount);
        }
    }
}
