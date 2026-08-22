using System;
using System.Collections.Generic;

namespace M3P
{
    /// <summary>Persistent progress for the current map run (lives on <see cref="GameManager"/>).</summary>
    public sealed class MapRunState
    {
        /// <summary>Shared pointer so Battle can return to Map even if GameManager is missing.</summary>
        public static MapRunState Active { get; set; }

        readonly HashSet<string> _clearedNodeIds = new HashSet<string>();

        public string GraphName { get; set; }
        public string CurrentNodeId { get; set; }
        public string PreviousNodeId { get; set; }
        public string PendingBattleNodeId { get; private set; }
        public EncounterConfig PendingEncounter { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsGenerated { get; private set; }
        public MapGraphSnapshot GraphSnapshot { get; private set; }

        public bool HasPendingBattle => !string.IsNullOrEmpty(PendingBattleNodeId);

        /// <summary>Enemy from the pending battle encounter, if any.</summary>
        public EnemyDefinition PendingEnemy =>
            PendingEncounter != null && PendingEncounter.IsBattle ? PendingEncounter.Enemy : null;

        public IReadOnlyCollection<string> ClearedNodeIds => _clearedNodeIds;

        public void BeginRun(string graphName, string startNodeId, MapGraphSnapshot generatedGraph = null)
        {
            GraphName = graphName;
            CurrentNodeId = startNodeId;
            PreviousNodeId = startNodeId;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            _clearedNodeIds.Clear();
            if (!string.IsNullOrEmpty(startNodeId))
                _clearedNodeIds.Add(startNodeId);
            IsGenerated = generatedGraph != null;
            GraphSnapshot = generatedGraph;
            IsActive = true;
            Active = this;
        }

        public void Restore(MapRunSave save)
        {
            if (save == null || !save.CanContinue)
            {
                Clear();
                return;
            }

            GraphName = save.GraphName;
            CurrentNodeId = save.CurrentNodeId;
            PreviousNodeId = string.IsNullOrEmpty(save.PreviousNodeId) ? save.CurrentNodeId : save.PreviousNodeId;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            _clearedNodeIds.Clear();
            if (save.ClearedNodeIds != null)
            {
                for (int i = 0; i < save.ClearedNodeIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(save.ClearedNodeIds[i]))
                        _clearedNodeIds.Add(save.ClearedNodeIds[i]);
                }
            }

            IsGenerated = save.IsGenerated;
            GraphSnapshot = save.Graph != null ? save.Graph.Clone() : null;
            IsActive = true;
            Active = this;
        }

        public MapRunSave ToSave()
        {
            if (!IsActive)
                return new MapRunSave();

            var cleared = new string[_clearedNodeIds.Count];
            _clearedNodeIds.CopyTo(cleared);

            return new MapRunSave
            {
                IsActive = true,
                IsGenerated = IsGenerated,
                GraphName = GraphName,
                CurrentNodeId = CurrentNodeId,
                PreviousNodeId = PreviousNodeId,
                ClearedNodeIds = cleared,
                Graph = GraphSnapshot != null ? GraphSnapshot.Clone() : null,
            };
        }

        public bool IsCleared(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _clearedNodeIds.Contains(nodeId);
        }

        public void MarkCleared(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
                _clearedNodeIds.Add(nodeId);
        }

        public void MoveTo(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || nodeId == CurrentNodeId)
                return;

            PreviousNodeId = CurrentNodeId;
            CurrentNodeId = nodeId;
        }

        public void BeginBattle(string nodeId, EncounterConfig encounter = null)
        {
            PendingBattleNodeId = nodeId;
            PendingEncounter = encounter;
        }

        public void ResolveBattle(bool won)
        {
            string battleNodeId = PendingBattleNodeId;
            PendingBattleNodeId = null;
            PendingEncounter = null;

            if (string.IsNullOrEmpty(battleNodeId))
                return;

            if (won)
            {
                MarkCleared(battleNodeId);
                CurrentNodeId = battleNodeId;
                return;
            }

            // Loss: step back so the player can pick another route or retry.
            if (!string.IsNullOrEmpty(PreviousNodeId))
                CurrentNodeId = PreviousNodeId;
        }

        public void Clear()
        {
            GraphName = null;
            CurrentNodeId = null;
            PreviousNodeId = null;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            _clearedNodeIds.Clear();
            IsGenerated = false;
            GraphSnapshot = null;
            IsActive = false;
        }
    }
}
