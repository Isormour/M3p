using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Authorable floor graph: nodes with encounters and undirected connections.</summary>
    [CreateAssetMenu(fileName = "MapGraphDefinition", menuName = "M3P/Map Graph", order = 20)]
    public class MapGraphDefinition : ScriptableObject
    {
        [Serializable]
        public class Node
        {
            [Tooltip("Stable id used by run state and edges.")]
            public string Id;

            [Tooltip("Encounter played on this node. Type, marker and battle enemy come from here.")]
            public EncounterConfig Encounter;

            [Tooltip("Fallback type used only when Encounter is missing.")]
            public MapNodeType Type = MapNodeType.Battle;

            [Tooltip("Position on the map plane (X/Z). Y is ignored at runtime.")]
            public Vector2 Position;

            public MapNodeType ResolvedType => Encounter != null ? Encounter.Type : Type;
        }

        [Serializable]
        public class Edge
        {
            public string FromId;
            public string ToId;
        }

        [SerializeField] string _startNodeId = "start";
        [SerializeField] List<Node> _nodes = new List<Node>();
        [SerializeField] List<Edge> _edges = new List<Edge>();

        public string StartNodeId => _startNodeId;
        public IReadOnlyList<Node> Nodes => _nodes;
        public IReadOnlyList<Edge> Edges => _edges;

        public bool TryGetNode(string nodeId, out Node node)
        {
            node = null;
            if (string.IsNullOrEmpty(nodeId) || _nodes == null)
                return false;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i] == null || _nodes[i].Id != nodeId)
                    continue;

                node = _nodes[i];
                return true;
            }

            return false;
        }

        public List<string> GetNeighborIds(string nodeId)
        {
            var neighbors = new List<string>();
            if (string.IsNullOrEmpty(nodeId) || _edges == null)
                return neighbors;

            for (int i = 0; i < _edges.Count; i++)
            {
                Edge edge = _edges[i];
                if (edge == null)
                    continue;

                if (edge.FromId == nodeId && !string.IsNullOrEmpty(edge.ToId) && !neighbors.Contains(edge.ToId))
                    neighbors.Add(edge.ToId);
                else if (edge.ToId == nodeId && !string.IsNullOrEmpty(edge.FromId) && !neighbors.Contains(edge.FromId))
                    neighbors.Add(edge.FromId);
            }

            return neighbors;
        }

        /// <summary>Small branching demo floor used when no asset is assigned.</summary>
        public static MapGraphDefinition CreateRuntimeDemo()
        {
            var graph = CreateInstance<MapGraphDefinition>();
            graph.name = "RuntimeDemoMap";
            graph._startNodeId = "start";
            graph._nodes = new List<Node>
            {
                new Node { Id = "start", Type = MapNodeType.Start, Position = new Vector2(0f, 0f) },
                new Node { Id = "battle_a", Type = MapNodeType.Battle, Position = new Vector2(-3f, 3f) },
                new Node { Id = "battle_b", Type = MapNodeType.Battle, Position = new Vector2(3f, 3f) },
                new Node { Id = "chest_a", Type = MapNodeType.Chest, Position = new Vector2(-5f, 6f) },
                new Node { Id = "shop_a", Type = MapNodeType.Shop, Position = new Vector2(5f, 6f) },
                new Node { Id = "battle_c", Type = MapNodeType.Battle, Position = new Vector2(0f, 6f) },
                new Node { Id = "chest_b", Type = MapNodeType.Chest, Position = new Vector2(-2f, 9f) },
                new Node { Id = "shop_b", Type = MapNodeType.Shop, Position = new Vector2(2f, 9f) },
                new Node { Id = "battle_boss", Type = MapNodeType.Battle, Position = new Vector2(0f, 12f) }
            };
            graph._edges = new List<Edge>
            {
                new Edge { FromId = "start", ToId = "battle_a" },
                new Edge { FromId = "start", ToId = "battle_b" },
                new Edge { FromId = "battle_a", ToId = "chest_a" },
                new Edge { FromId = "battle_a", ToId = "battle_c" },
                new Edge { FromId = "battle_b", ToId = "shop_a" },
                new Edge { FromId = "battle_b", ToId = "battle_c" },
                new Edge { FromId = "chest_a", ToId = "chest_b" },
                new Edge { FromId = "shop_a", ToId = "shop_b" },
                new Edge { FromId = "battle_c", ToId = "chest_b" },
                new Edge { FromId = "battle_c", ToId = "shop_b" },
                new Edge { FromId = "chest_b", ToId = "battle_boss" },
                new Edge { FromId = "shop_b", ToId = "battle_boss" }
            };
            return graph;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_nodes == null)
                return;

            var seen = new HashSet<string>();
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node node = _nodes[i];
                if (node == null)
                    continue;

                if (string.IsNullOrWhiteSpace(node.Id))
                    node.Id = $"node_{i}";

                if (node.Encounter != null)
                    node.Type = node.Encounter.Type;

                if (!seen.Add(node.Id))
                    Debug.LogWarning($"{nameof(MapGraphDefinition)} '{name}': duplicate node id '{node.Id}'.", this);
            }
        }
#endif
    }
}
