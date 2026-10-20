using UnityEngine;

namespace M3P
{
    /// <summary>One active copy of a <see cref="StatusEffectDefinition"/> on a battle character.</summary>
    public sealed class StatusInstance
    {
        public StatusEffectDefinition Definition { get; }

        /// <summary>Original applier; used to scale periodic magic effects.</summary>
        public BattleCharacter Source { get; set; }

        public int RemainingTurns { get; set; }

        /// <summary>Piled applications of a stacking status. Always at least 1.</summary>
        public int Stacks { get; set; }

        /// <summary>
        /// Value captured when the status was applied, such as floor-scaled damage reduction.
        /// Zero means the definition's authored numbers are used instead.
        /// </summary>
        public int Magnitude { get; set; }

        /// <summary>Scaled counter stored with an incoming-guard status such as Riposta. Zero uses the definition.</summary>
        public int CounterMagnitude { get; set; }

        /// <summary>Flat action points removed when this status ticks. Zero uses the tick's authored formula.</summary>
        public int ActionPointDrain { get; set; }

        public StatusInstance(StatusEffectDefinition definition, BattleCharacter source, int remainingTurns, int stacks = 1)
        {
            Definition = definition;
            Source = source;
            RemainingTurns = remainingTurns;
            Stacks = Mathf.Max(1, stacks);
        }
    }
}
