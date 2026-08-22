namespace M3P
{
    /// <summary>Optional extra input collected before a skill spends AP and mana.</summary>
    public readonly struct SkillCastChoice
    {
        public int Primary { get; }
        public int Secondary { get; }

        public SkillCastChoice(int primary, int secondary = 0)
        {
            Primary = primary;
            Secondary = secondary;
        }
    }

    public enum SkillCastPrompt
    {
        None = 0,
        DiscardCard = 1,
        TransmuteMana = 2,
    }

    public enum SkillArchetype
    {
        None = 0,
        Warrior = 1,
        Mage = 2,
        Shadow = 3,
    }
}
