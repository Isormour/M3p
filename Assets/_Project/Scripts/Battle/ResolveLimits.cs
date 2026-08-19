using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Per-Resolve budgets that still apply to card draws and burn stacks. Stamina from tile upgrades
    /// is not capped — every slot pays when its tile is cleared.
    /// </summary>
    public sealed class ResolveLimits
    {
        /// <summary>Card draws allowed per Resolve.</summary>
        public const int MaxCardDraws = 1;

        /// <summary>Fire/Burn stacks tiles may apply in one Resolve.</summary>
        public const int MaxBurnStacks = 3;

        int _cardDraws;
        int _burnStacks;

        /// <summary>Stamina handed back during the Resolve, reported to the turn summary.</summary>
        public int StaminaRefunded { get; private set; }

        public void BeginResolve()
        {
            _cardDraws = 0;
            _burnStacks = 0;
            StaminaRefunded = 0;
        }

        public void RecordStaminaRefund(int amount)
        {
            if (amount > 0)
                StaminaRefunded += amount;
        }

        public bool TrySpendCardDraw()
        {
            if (_cardDraws >= MaxCardDraws)
                return false;

            _cardDraws++;
            return true;
        }

        public bool TrySpendBurnStack()
        {
            if (_burnStacks >= MaxBurnStacks)
                return false;

            _burnStacks++;
            return true;
        }

        public int RemainingBurnStacks => Mathf.Max(0, MaxBurnStacks - _burnStacks);
    }
}
