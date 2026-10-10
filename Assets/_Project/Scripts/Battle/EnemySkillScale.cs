using UnityEngine;

namespace M3P
{
    /// <summary>Which runtime number multiplies an authored enemy-skill coefficient.</summary>
    public enum EnemyScaleAxis
    {
        None = 0,
        Floor = 1,
        Rank = 2,
        FloorAndRank = 3,
    }

    /// <summary>
    /// Floor and rank used by enemy skills. Floor is the fight's effective floor.
    /// Rank is the number of named thresholds reached, and is at least 1.
    /// </summary>
    public static class EnemySkillScale
    {
        public static int Floor(BattleCharacter caster)
        {
            if (caster is EnemyBattleCharacter enemy && enemy.RuntimeSpec != null)
                return Mathf.Max(1, enemy.RuntimeSpec.EffectiveFloor);

            return 1;
        }

        public static int Rank(BattleCharacter caster)
        {
            if (caster is EnemyBattleCharacter enemy && enemy.RuntimeSpec != null)
                return Mathf.Max(1, enemy.RuntimeSpec.Rank);

            return 1;
        }

        public static int Scale(int amount, EnemyScaleAxis axis, BattleCharacter caster)
        {
            if (amount <= 0)
                return 0;

            switch (axis)
            {
                case EnemyScaleAxis.Floor:
                    return amount * Floor(caster);
                case EnemyScaleAxis.Rank:
                    return amount * Rank(caster);
                case EnemyScaleAxis.FloorAndRank:
                    return amount * Floor(caster) * Rank(caster);
                default:
                    return amount;
            }
        }
    }
}
