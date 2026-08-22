using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Pending combat buffs that live on a character until they are consumed by a hit or skill.
    /// Armed by skills such as Riposte, Ice Ward, Expose Weakness and Unyielding.
    /// </summary>
    public sealed class CombatModifiers
    {
        public int IncomingDamageReduction { get; private set; }
        public int IncomingReductionHits { get; private set; }
        public int CounterDamage { get; private set; }
        public bool CounterPhysical { get; private set; }

        public int OutgoingHitBonus { get; private set; }
        public int OutgoingHitBonusHits { get; private set; }

        public float NextPhysicalSkillMultiplier { get; private set; } = 1f;

        public void ArmIncomingReduction(int reduction, int hits = 1, int counterDamage = 0, bool counterPhysical = true)
        {
            IncomingDamageReduction = Mathf.Max(0, reduction);
            IncomingReductionHits = Mathf.Max(1, hits);
            CounterDamage = Mathf.Max(0, counterDamage);
            CounterPhysical = counterPhysical;
        }

        public bool TryConsumeIncoming(out int reduction, out int counterDamage, out bool counterPhysical)
        {
            reduction = 0;
            counterDamage = 0;
            counterPhysical = false;

            if (IncomingReductionHits <= 0)
                return false;

            reduction = IncomingDamageReduction;
            counterDamage = CounterDamage;
            counterPhysical = CounterPhysical;
            IncomingReductionHits--;
            if (IncomingReductionHits <= 0)
            {
                IncomingDamageReduction = 0;
                CounterDamage = 0;
                CounterPhysical = false;
            }

            return true;
        }

        public void ArmOutgoingHitBonus(int bonus, int hits)
        {
            OutgoingHitBonus = Mathf.Max(0, bonus);
            OutgoingHitBonusHits = Mathf.Max(0, hits);
        }

        public int ConsumeOutgoingHitBonus()
        {
            if (OutgoingHitBonusHits <= 0 || OutgoingHitBonus <= 0)
                return 0;

            int bonus = OutgoingHitBonus;
            OutgoingHitBonusHits--;
            if (OutgoingHitBonusHits <= 0)
                OutgoingHitBonus = 0;

            return bonus;
        }

        public void ArmNextPhysicalSkillBonus(float multiplier)
        {
            NextPhysicalSkillMultiplier = Mathf.Max(1f, multiplier);
        }

        public float ConsumeNextPhysicalSkillMultiplier()
        {
            float multiplier = NextPhysicalSkillMultiplier > 0f ? NextPhysicalSkillMultiplier : 1f;
            NextPhysicalSkillMultiplier = 1f;
            return multiplier;
        }

        public void Clear()
        {
            IncomingDamageReduction = 0;
            IncomingReductionHits = 0;
            CounterDamage = 0;
            CounterPhysical = false;
            OutgoingHitBonus = 0;
            OutgoingHitBonusHits = 0;
            NextPhysicalSkillMultiplier = 1f;
        }
    }
}
