using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Persistent progress for the current map run (lives on <see cref="GameManager"/>).</summary>
    public sealed class MapRunState
    {
        /// <summary>Shared pointer so Battle can return to Map even if GameManager is missing.</summary>
        public static MapRunState Active { get; set; }

        readonly HashSet<string> _clearedNodeIds = new HashSet<string>();
        readonly List<string> _pathNodeIds = new List<string>();

        public string GraphName { get; set; }
        public string CurrentNodeId { get; set; }
        public string PreviousNodeId { get; set; }
        public string PendingBattleNodeId { get; private set; }
        public EncounterConfig PendingEncounter { get; private set; }
        public MapNodeType PendingEncounterType { get; private set; } = MapNodeType.Battle;
        public bool IsActive { get; private set; }
        public bool IsGenerated { get; private set; }
        public MapGraphSnapshot GraphSnapshot { get; private set; }
        public int FloorIndex { get; private set; } = 1;

        public bool HasPendingBattle => !string.IsNullOrEmpty(PendingBattleNodeId);
        public IReadOnlyList<string> PathNodeIds => _pathNodeIds;

        /// <summary>True when the fight the Battle scene is resolving is the floor boss.</summary>
        public bool IsPendingBossBattle
        {
            get
            {
                if (string.IsNullOrEmpty(PendingBattleNodeId))
                    return false;

                if (PendingEncounterType == MapNodeType.Boss)
                    return true;

                if (PendingEncounter != null && PendingEncounter.Type == MapNodeType.Boss)
                    return true;

                if (PendingBattleNodeId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (GraphSnapshot?.Nodes == null)
                    return false;

                for (int i = 0; i < GraphSnapshot.Nodes.Length; i++)
                {
                    MapGraphSnapshot.Node node = GraphSnapshot.Nodes[i];
                    if (node != null && node.Id == PendingBattleNodeId)
                        return node.Type == MapNodeType.Boss;
                }

                return false;
            }
        }

        /// <summary>Enemy picked from the pending battle encounter, if any.</summary>
        public EnemyDefinition PendingEnemy { get; private set; }

        /// <summary>Picks one enemy from <paramref name="encounter"/> for this node. Stable for the run.</summary>
        public EnemyDefinition PickEncounterEnemy(EncounterConfig encounter, string nodeId)
        {
            if (encounter == null || !encounter.IsBattle)
                return null;

            int seed = GraphSnapshot != null ? GraphSnapshot.Seed : 0;
            return encounter.PickEnemy(unchecked(seed + FloorIndex * 13), nodeId);
        }

        public IReadOnlyCollection<string> ClearedNodeIds => _clearedNodeIds;

        public void BeginRun(
            string graphName,
            string startNodeId,
            MapGraphSnapshot generatedGraph = null,
            int floorIndex = 1)
        {
            GraphName = graphName;
            CurrentNodeId = startNodeId;
            PreviousNodeId = startNodeId;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            PendingEncounterType = MapNodeType.Battle;
            PendingEnemy = null;
            _clearedNodeIds.Clear();
            _pathNodeIds.Clear();
            if (!string.IsNullOrEmpty(startNodeId))
            {
                _clearedNodeIds.Add(startNodeId);
                _pathNodeIds.Add(startNodeId);
            }

            IsGenerated = generatedGraph != null;
            GraphSnapshot = generatedGraph;
            FloorIndex = Mathf.Max(1, floorIndex);
            IsActive = true;
            Active = this;
        }

        public void SetFloorIndex(int floorIndex)
        {
            FloorIndex = Mathf.Max(1, floorIndex);
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
            PendingEncounterType = MapNodeType.Battle;
            PendingEnemy = null;
            _clearedNodeIds.Clear();
            _pathNodeIds.Clear();
            if (save.ClearedNodeIds != null)
            {
                for (int i = 0; i < save.ClearedNodeIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(save.ClearedNodeIds[i]))
                        _clearedNodeIds.Add(save.ClearedNodeIds[i]);
                }
            }

            if (save.PathNodeIds != null)
            {
                for (int i = 0; i < save.PathNodeIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(save.PathNodeIds[i]) && !_pathNodeIds.Contains(save.PathNodeIds[i]))
                        _pathNodeIds.Add(save.PathNodeIds[i]);
                }
            }

            IsGenerated = save.IsGenerated;
            GraphSnapshot = save.Graph != null ? save.Graph.Clone() : null;
            FloorIndex = save.FloorIndex > 0 ? save.FloorIndex : 1;
            IsActive = true;
            Active = this;
        }

        public MapRunSave ToSave()
        {
            if (!IsActive)
                return new MapRunSave();

            var cleared = new string[_clearedNodeIds.Count];
            _clearedNodeIds.CopyTo(cleared);
            var path = new string[_pathNodeIds.Count];
            _pathNodeIds.CopyTo(path);

            return new MapRunSave
            {
                IsActive = true,
                IsGenerated = IsGenerated,
                FloorIndex = FloorIndex,
                GraphName = GraphName,
                CurrentNodeId = CurrentNodeId,
                PreviousNodeId = PreviousNodeId,
                ClearedNodeIds = cleared,
                PathNodeIds = path,
                Graph = GraphSnapshot != null ? GraphSnapshot.Clone() : null,
            };
        }

        public bool IsCleared(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _clearedNodeIds.Contains(nodeId);
        }

        public bool IsOnPath(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _pathNodeIds.Contains(nodeId);
        }

        public bool HasWalkedEdge(string fromId, string toId)
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
                return false;

            for (int i = 0; i < _pathNodeIds.Count - 1; i++)
            {
                if (_pathNodeIds[i] == fromId && _pathNodeIds[i + 1] == toId)
                    return true;
            }

            return false;
        }

        public void EnsurePath(MapGraphDefinition graph)
        {
            if (IsPathValid(graph))
                return;

            ReconstructPath(graph);
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
            AddToPath(nodeId);
        }

        public void BeginBattle(
            string nodeId,
            EncounterConfig encounter = null,
            MapNodeType encounterType = MapNodeType.Battle)
        {
            PendingBattleNodeId = nodeId;
            PendingEncounter = encounter;
            PendingEncounterType = encounterType;
            PendingEnemy = PickEncounterEnemy(encounter, nodeId);
        }

        public void ResolveBattle(bool won)
        {
            string battleNodeId = PendingBattleNodeId;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            PendingEncounterType = MapNodeType.Battle;
            PendingEnemy = null;

            if (string.IsNullOrEmpty(battleNodeId))
                return;

            if (won)
            {
                MarkCleared(battleNodeId);
                CurrentNodeId = battleNodeId;
                AddToPath(battleNodeId);
                return;
            }

            // Loss: step back so the player can pick another route or retry.
            if (!string.IsNullOrEmpty(PreviousNodeId))
            {
                CurrentNodeId = PreviousNodeId;
                AddToPath(PreviousNodeId);
            }
        }

        public void Clear()
        {
            GraphName = null;
            CurrentNodeId = null;
            PreviousNodeId = null;
            PendingBattleNodeId = null;
            PendingEncounter = null;
            PendingEncounterType = MapNodeType.Battle;
            PendingEnemy = null;
            _clearedNodeIds.Clear();
            _pathNodeIds.Clear();
            IsGenerated = false;
            GraphSnapshot = null;
            FloorIndex = 1;
            IsActive = false;
        }

        void AddToPath(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            int existing = _pathNodeIds.IndexOf(nodeId);
            if (existing >= 0)
            {
                int removeFrom = existing + 1;
                if (removeFrom < _pathNodeIds.Count)
                    _pathNodeIds.RemoveRange(removeFrom, _pathNodeIds.Count - removeFrom);
                return;
            }

            _pathNodeIds.Add(nodeId);
        }

        bool IsPathValid(MapGraphDefinition graph)
        {
            if (_pathNodeIds.Count == 0 || string.IsNullOrEmpty(CurrentNodeId))
                return false;

            if (_pathNodeIds[_pathNodeIds.Count - 1] != CurrentNodeId)
                return false;

            if (graph != null &&
                !string.IsNullOrEmpty(graph.StartNodeId) &&
                _pathNodeIds[0] != graph.StartNodeId)
                return false;

            if (graph == null)
                return true;

            for (int i = 0; i < _pathNodeIds.Count; i++)
            {
                if (!graph.TryGetNode(_pathNodeIds[i], out _))
                    return false;
            }

            for (int i = 0; i < _pathNodeIds.Count - 1; i++)
            {
                if (!AreNeighbors(graph, _pathNodeIds[i], _pathNodeIds[i + 1]))
                    return false;
            }

            return true;
        }

        void ReconstructPath(MapGraphDefinition graph)
        {
            _pathNodeIds.Clear();
            string startId = graph != null ? graph.StartNodeId : null;
            if (string.IsNullOrEmpty(CurrentNodeId))
            {
                if (!string.IsNullOrEmpty(startId))
                    _pathNodeIds.Add(startId);
                return;
            }

            if (graph == null || string.IsNullOrEmpty(startId) || CurrentNodeId == startId)
            {
                if (!string.IsNullOrEmpty(startId))
                    _pathNodeIds.Add(startId);
                if (!string.IsNullOrEmpty(CurrentNodeId) && CurrentNodeId != startId)
                    _pathNodeIds.Add(CurrentNodeId);
                return;
            }

            var allowed = new HashSet<string>(_clearedNodeIds) { CurrentNodeId };
            var visited = new HashSet<string> { startId };
            var parent = new Dictionary<string, string>();
            var queue = new Queue<string>();
            queue.Enqueue(startId);

            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (id == CurrentNodeId)
                    break;

                List<string> neighbors = graph.GetNeighborIds(id);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    string next = neighbors[i];
                    if (string.IsNullOrEmpty(next) || !allowed.Contains(next) || !visited.Add(next))
                        continue;

                    parent[next] = id;
                    queue.Enqueue(next);
                }
            }

            if (CurrentNodeId != startId && !parent.ContainsKey(CurrentNodeId))
            {
                _pathNodeIds.Add(startId);
                _pathNodeIds.Add(CurrentNodeId);
                return;
            }

            var reversed = new List<string>();
            string cursor = CurrentNodeId;
            while (true)
            {
                reversed.Add(cursor);
                if (cursor == startId)
                    break;
                if (!parent.TryGetValue(cursor, out cursor))
                    break;
            }

            for (int i = reversed.Count - 1; i >= 0; i--)
                _pathNodeIds.Add(reversed[i]);
        }

        static bool AreNeighbors(MapGraphDefinition graph, string fromId, string toId)
        {
            List<string> neighbors = graph.GetNeighborIds(fromId);
            return neighbors.Contains(toId);
        }
    }
}
