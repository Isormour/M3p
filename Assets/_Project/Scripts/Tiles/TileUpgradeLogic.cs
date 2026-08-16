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
        /// <summary>
        /// Adds extra tiles to a clear set. Called once per origin tile before anything is destroyed.
        /// </summary>
        public virtual void CollectExtraClears(Match3Board board, Match3Tile tile, HashSet<Match3Tile> destination)
        {
        }

        /// <summary>Runs after the tile has been counted for mana, just before it is destroyed.</summary>
        public virtual void OnCleared(TileUpgradeContext context)
        {
        }
    }
}
