using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public abstract class BattleEffectLogic
    {
        public abstract void Apply(BattleEffectContext context, EEffectTarget target);
    }
}
