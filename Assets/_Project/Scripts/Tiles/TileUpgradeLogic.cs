using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>Runtime payload for a tile upgrade that just paid out because its tile was cleared.</summary>
    public readonly struct TileUpgradeContext
    {
        public BattleCharacter Player { get; }
        public BattleCharacter Opponent { get; }
        public int TileTypeId { get; }
        public Vector3 WorldPosition { get; }

        public TileUpgradeContext(
            BattleCharacter player,
            BattleCharacter opponent,
            int tileTypeId,
            Vector3 worldPosition)
        {
            Player = player;
            Opponent = opponent;
            TileTypeId = tileTypeId;
            WorldPosition = worldPosition;
        }
    }

    [Serializable]
    public abstract class TileUpgradeLogic
    {
        /// <summary>True when this upgrade extra-clears a neighbour in the slot's direction.</summary>
        public virtual bool AffectsNeighbor => false;

        /// <summary>
        /// Adds extra tiles to a clear set. Called once per origin tile before anything is destroyed.
        /// <paramref name="slotIndex"/> is the upgrade slot on that tile (0 up, 1 down, 2 left, 3 right).
        /// </summary>
        public virtual void CollectExtraClears(
            Match3Board board,
            Match3Tile tile,
            int slotIndex,
            HashSet<Match3Tile> destination)
        {
        }

        /// <summary>Runs after the tile has been counted for mana, just before it is destroyed.</summary>
        public virtual void OnCleared(TileUpgradeContext context)
        {
        }
    }
}
