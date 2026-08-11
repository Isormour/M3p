using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class DealDamageLogic : BattleEffectLogic
    {
        [SerializeField] int _amount;

        public int Amount => _amount;

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_amount <= 0)
                return;

            BattleCharacter character = context.Resolve(target);
            SoftStats softStats = character?.Stats?.Soft;
            if (softStats == null)
                return;

            int amount = ResolveAmount(context);
            softStats.TakeDamage(amount);
        }

        int ResolveAmount(BattleEffectContext context)
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            StatProgressionConfig progression = config != null
                ? config.StatProgression
                : StatProgressionConfig.CreateDefault();

            HardStats casterHard = context.Caster?.Stats?.Hard ?? default;
            TalentBonuses talents = context.Caster?.Stats?.TalentBonuses ?? TalentBonuses.None;
            return progression.ScaleMagicEffect(casterHard, _amount, talents);
        }
    }
}
