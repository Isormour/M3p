namespace M3P
{
    /// <summary>One active copy of a <see cref="StatusEffectDefinition"/> on a battle character.</summary>
    public sealed class StatusInstance
    {
        public StatusEffectDefinition Definition { get; }

        /// <summary>Original applier; used to scale periodic magic effects.</summary>
        public BattleCharacter Source { get; set; }

        public int RemainingTurns { get; set; }

        public StatusInstance(StatusEffectDefinition definition, BattleCharacter source, int remainingTurns)
        {
            Definition = definition;
            Source = source;
            RemainingTurns = remainingTurns;
        }
    }
}
