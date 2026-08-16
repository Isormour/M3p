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

    [Serializable]
    public sealed class AddApUpgradeLogic : TileUpgradeLogic
    {
        [Min(1), SerializeField] int _amount = 1;

        public int Amount => Mathf.Max(1, _amount);

        public override void OnCleared(TileUpgradeContext context)
        {
            context.Player?.Stats?.Soft?.AddActionPoints(Amount);
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

    [Serializable]
    public sealed class StackBurnUpgradeLogic : TileUpgradeLogic
    {
        [SerializeField] StatusEffectDefinition _burn;

        public StatusEffectDefinition Burn => _burn;

        public override void OnCleared(TileUpgradeContext context)
        {
            if (_burn == null || context.Opponent == null)
                return;

            context.Opponent.ApplyStatus(_burn, context.Player);
        }
    }

    [Serializable]
    public sealed class MatchNeighborUpgradeLogic : TileUpgradeLogic
    {
        public override void CollectExtraClears(Match3Board board, Match3Tile tile, HashSet<Match3Tile> destination)
        {
            if (board == null || tile == null || destination == null)
                return;

            TryAdd(board, destination, tile.X + 1, tile.Y);
            TryAdd(board, destination, tile.X - 1, tile.Y);
            TryAdd(board, destination, tile.X, tile.Y + 1);
            TryAdd(board, destination, tile.X, tile.Y - 1);
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
