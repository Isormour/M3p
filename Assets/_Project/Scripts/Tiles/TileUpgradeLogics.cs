using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public sealed class AddManaUpgradeLogic : TileUpgradeLogic
    {
        [Min(1), SerializeField] int _amount = 1;

        public int Amount => Mathf.Max(1, _amount);

        public override void OnCleared(TileUpgradeContext context)
        {
            context.Player?.Stats?.Soft?.AddManaFromBrokenTiles(context.TileTypeId, Amount);
        }
    }

    [Serializable]
    public sealed class AddHpUpgradeLogic : TileUpgradeLogic
    {
        [Min(1), SerializeField] int _amount = 1;

        public int Amount => Mathf.Max(1, _amount);

        public override void OnCleared(TileUpgradeContext context)
        {
            context.Player?.Stats?.Soft?.Heal(Amount);
        }
    }

    /// <summary>Hands stamina back. Every stamina upgrade on a cleared tile pays in full, with no Resolve cap.</summary>
    [Serializable]
    public sealed class AddApUpgradeLogic : TileUpgradeLogic
    {
        [Min(1), SerializeField] int _amount = 1;

        public int Amount => Mathf.Max(1, _amount);

        public override void OnCleared(TileUpgradeContext context)
        {
            context.Limits?.RecordStaminaRefund(Amount);
            context.Player?.Stats?.Soft?.AddActionPoints(Amount);
        }
    }

    /// <summary>Draws a card, capped to one draw per Resolve.</summary>
    [Serializable]
    public sealed class DrawCardUpgradeLogic : TileUpgradeLogic
    {
        public override void OnCleared(TileUpgradeContext context)
        {
            if (context.CardPlay == null)
                return;

            if (context.Limits != null && !context.Limits.TrySpendCardDraw())
                return;

            context.CardPlay.DrawCards(1);
        }
    }

    [Serializable]
    public sealed class AddShieldUpgradeLogic : TileUpgradeLogic
    {
        [Min(1), SerializeField] int _amount = 1;

        public int Amount => Mathf.Max(1, _amount);

        public override void OnCleared(TileUpgradeContext context)
        {
            context.Player?.Stats?.Soft?.AddShield(Amount);
        }
    }

    /// <summary>
    /// Stacks Burn on the opponent. Capped per Resolve so a wide sequence cannot dump an entire Ignite
    /// payoff in one go; the stack itself is meant to build up to a skill, not replace it.
    /// </summary>
    [Serializable]
    public sealed class StackBurnUpgradeLogic : TileUpgradeLogic
    {
        [SerializeField] StatusEffectDefinition _burn;

        public StatusEffectDefinition Burn => _burn;

        public override void OnCleared(TileUpgradeContext context)
        {
            if (_burn == null || context.Opponent == null)
                return;

            if (context.Limits != null && !context.Limits.TrySpendBurnStack())
                return;

            context.Opponent.ApplyStatus(_burn, context.Player);
        }
    }

    [Serializable]
    public sealed class MatchNeighborUpgradeLogic : TileUpgradeLogic
    {
        public override bool AffectsNeighbor => true;

        public override void CollectExtraClears(
            Match3Board board,
            Match3Tile tile,
            int slotIndex,
            HashSet<Match3Tile> destination)
        {
            if (board == null || tile == null || destination == null)
                return;

            OffsetForSlot(slotIndex, out int dx, out int dy);
            TryAdd(board, destination, tile.X + dx, tile.Y + dy);
        }

        static void OffsetForSlot(int slotIndex, out int dx, out int dy)
        {
            switch (slotIndex)
            {
                case 0:
                    dx = 0;
                    dy = 1;
                    return;
                case 1:
                    dx = 0;
                    dy = -1;
                    return;
                case 2:
                    dx = -1;
                    dy = 0;
                    return;
                case 3:
                    dx = 1;
                    dy = 0;
                    return;
                default:
                    dx = 0;
                    dy = 0;
                    return;
            }
        }

        static void TryAdd(Match3Board board, HashSet<Match3Tile> destination, int x, int y)
        {
            if (!board.CanDestroyTile(x, y))
                return;

            Match3Tile neighbour = board.GetTile(x, y);
            if (neighbour != null)
                destination.Add(neighbour);
        }
    }
}
