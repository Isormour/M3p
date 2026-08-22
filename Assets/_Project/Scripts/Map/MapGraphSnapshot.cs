using System;
using UnityEngine;

namespace M3P
{
    /// <summary>JSON-friendly copy of a floor graph so a generated map can be saved and rebuilt.</summary>
    [Serializable]
    public class MapGraphSnapshot
    {
        public string Name;
        public string StartNodeId;
        public Node[] Nodes;
        public Edge[] Edges;

        [Serializable]
        public class Node
        {
            public string Id;
            public string EncounterName;
            public MapNodeType Type;
            public Vector2 Position;
        }

        [Serializable]
        public class Edge
        {
            public string FromId;
            public string ToId;
        }

        public MapGraphSnapshot Clone()
        {
            return new MapGraphSnapshot
            {
                Name = Name,
                StartNodeId = StartNodeId,
                Nodes = CloneNodes(Nodes),
                Edges = CloneEdges(Edges),
            };
        }

        static Node[] CloneNodes(Node[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<Node>();

            Node[] copy = new Node[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Node node = source[i];
                copy[i] = node == null
                    ? null
                    : new Node
                    {
                        Id = node.Id,
                        EncounterName = node.EncounterName,
                        Type = node.Type,
                        Position = node.Position,
                    };
            }

            return copy;
        }

        static Edge[] CloneEdges(Edge[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<Edge>();

            Edge[] copy = new Edge[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Edge edge = source[i];
                copy[i] = edge == null
                    ? null
                    : new Edge { FromId = edge.FromId, ToId = edge.ToId };
            }

            return copy;
        }
    }

    /// <summary>JSON-friendly copy of map-run progress stored on the player profile.</summary>
    [Serializable]
    public class MapRunSave
    {
        public bool IsActive;
        public bool IsGenerated;
        public string GraphName;
        public string CurrentNodeId;
        public string PreviousNodeId;
        public string[] ClearedNodeIds;
        public MapGraphSnapshot Graph;

        public bool CanContinue =>
            IsActive && !string.IsNullOrEmpty(CurrentNodeId) && !string.IsNullOrEmpty(GraphName);

        public MapRunSave Clone()
        {
            return new MapRunSave
            {
                IsActive = IsActive,
                IsGenerated = IsGenerated,
                GraphName = GraphName,
                CurrentNodeId = CurrentNodeId,
                PreviousNodeId = PreviousNodeId,
                ClearedNodeIds = ClearedNodeIds != null ? (string[])ClearedNodeIds.Clone() : Array.Empty<string>(),
                Graph = Graph != null ? Graph.Clone() : null,
            };
        }
    }
}
