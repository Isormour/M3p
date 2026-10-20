using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One step of <c>base + coefficient × floor((property − subtract) / divisor)</c>.
    /// Several terms add together. A divisor of 0 or 1 leaves the property whole.
    /// </summary>
    [Serializable]
    public struct SkillPropertyTerm
    {
        public PlayerProperty Property;

        [Min(0)]
        public int Coefficient;

        [Min(0)]
        public int Divisor;

        [Min(0)]
        public int Subtract;

        public SkillPropertyTerm(PlayerProperty property, int coefficient, int divisor = 1, int subtract = 0)
        {
            Property = property;
            Coefficient = Mathf.Max(0, coefficient);
            Divisor = Mathf.Max(0, divisor);
            Subtract = Mathf.Max(0, subtract);
        }
    }

    public static class SkillPropertyScaling
    {
        /// <summary>Returns <paramref name="baseAmount"/> plus every term evaluated against <paramref name="properties"/>.</summary>
        public static int Apply(int baseAmount, SkillPropertyTerm[] terms, PlayerProperties properties)
        {
            if (terms == null || terms.Length == 0)
                return baseAmount;

            int total = baseAmount;
            for (int i = 0; i < terms.Length; i++)
                total += Evaluate(terms[i], properties);

            return total;
        }

        public static int Evaluate(SkillPropertyTerm term, PlayerProperties properties)
        {
            int coefficient = Mathf.Max(0, term.Coefficient);
            if (coefficient == 0)
                return 0;

            int divisor = term.Divisor > 1 ? term.Divisor : 1;
            int value = properties.Get(term.Property) - Mathf.Max(0, term.Subtract);
            return coefficient * Mathf.FloorToInt(value / (float)divisor);
        }

        public static PlayerProperties Read(BattleEffectContext context)
        {
            return context.Caster != null ? context.Caster.GetPlayerProperties() : default;
        }
    }
}
