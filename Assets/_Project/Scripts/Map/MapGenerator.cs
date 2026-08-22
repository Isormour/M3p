using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Builds a branching floor from encounter pools, used by New Game.</summary>
    public static class MapGenerator
    {
        public const string GeneratedGraphName = "GeneratedMap";

        public static MapGraphDefinition Generate(
            EncounterConfig startEncounter,
            IReadOnlyList<EncounterConfig> battleEncounters,
            EncounterConfig bossEncounter,
            IReadOnlyList<EncounterConfig> chestEncounters,
            IReadOnlyList<EncounterConfig> shopEncounters,
            int layerCount,
            int nodesPerLayerMin,
            int nodesPerLayerMax,
            float layerSpacing,
            float nodeSpacing,
            int seed)
        {
            layerCount = Mathf.Max(2, layerCount);
            nodesPerLayerMin = Mathf.Max(1, nodesPerLayerMin);
            nodesPerLayerMax = Mathf.Max(nodesPerLayerMin, nodesPerLayerMax);
            layerSpacing = Mathf.Max(1f, layerSpacing);
            nodeSpacing = Mathf.Max(1f, nodeSpacing);

            var rng = new System.Random(seed);
            var nodes = new List<MapGraphDefinition.Node>();
            var edges = new List<MapGraphDefinition.Edge>();
            var layerIds = new List<string>[layerCount + 2];

            nodes.Add(new MapGraphDefinition.Node
            {
                Id = "start",
                Encounter = startEncounter,
                Type = MapNodeType.Start,
                Position = Vector2.zero,
            });
            layerIds[0] = new List<string> { "start" };

            for (int layer = 1; layer <= layerCount; layer++)
            {
                int count = rng.Next(nodesPerLayerMin, nodesPerLayerMax + 1);
                layerIds[layer] = new List<string>(count);
                float originX = -(count - 1) * 0.5f * nodeSpacing;
                for (int i = 0; i < count; i++)
                {
                    EncounterConfig encounter = PickLayerEncounter(
                        layer,
                        layerCount,
                        rng,
                        battleEncounters,
                        chestEncounters,
                        shopEncounters);
                    string id = $"n_{layer}_{i}";
                    nodes.Add(new MapGraphDefinition.Node
                    {
                        Id = id,
                        Encounter = encounter,
                        Type = encounter != null ? encounter.Type : MapNodeType.Battle,
                        Position = new Vector2(originX + i * nodeSpacing, layer * layerSpacing),
                    });
                    layerIds[layer].Add(id);
                }
            }

            int bossLayer = layerCount + 1;
            nodes.Add(new MapGraphDefinition.Node
            {
                Id = "boss",
                Encounter = bossEncounter,
                Type = MapNodeType.Battle,
                Position = new Vector2(0f, bossLayer * layerSpacing),
            });
            layerIds[bossLayer] = new List<string> { "boss" };

            for (int layer = 0; layer < layerIds.Length - 1; layer++)
                ConnectLayers(edges, layerIds[layer], layerIds[layer + 1], rng);

            var graph = ScriptableObject.CreateInstance<MapGraphDefinition>();
            graph.name = GeneratedGraphName;
            graph.ReplaceContents("start", nodes, edges);
            return graph;
        }

        static EncounterConfig PickLayerEncounter(
            int layer,
            int layerCount,
            System.Random rng,
            IReadOnlyList<EncounterConfig> battles,
            IReadOnlyList<EncounterConfig> chests,
            IReadOnlyList<EncounterConfig> shops)
        {
            if (layer <= 1)
                return Pick(battles, rng);

            int roll = rng.Next(100);
            if (roll < 55)
                return Pick(battles, rng);
            if (roll < 80)
                return Pick(chests, rng) ?? Pick(battles, rng);
            if (layer >= layerCount)
                return Pick(battles, rng);

            return Pick(shops, rng) ?? Pick(battles, rng);
        }

        static void ConnectLayers(
            List<MapGraphDefinition.Edge> edges,
            List<string> fromIds,
            List<string> toIds,
            System.Random rng)
        {
            if (fromIds == null || toIds == null || fromIds.Count == 0 || toIds.Count == 0)
                return;

            var incoming = new int[toIds.Count];
            for (int i = 0; i < fromIds.Count; i++)
            {
                int first = Mathf.Clamp(
                    Mathf.RoundToInt(i * (toIds.Count - 1) / (float)Mathf.Max(1, fromIds.Count - 1)),
                    0,
                    toIds.Count - 1);
                AddEdge(edges, fromIds[i], toIds[first]);
                incoming[first]++;

                if (toIds.Count > 1 && rng.Next(100) < 55)
                {
                    int second = first + (rng.Next(2) == 0 ? -1 : 1);
                    second = Mathf.Clamp(second, 0, toIds.Count - 1);
                    if (second != first)
                    {
                        AddEdge(edges, fromIds[i], toIds[second]);
                        incoming[second]++;
                    }
                }
            }

            for (int i = 0; i < toIds.Count; i++)
            {
                if (incoming[i] > 0)
                    continue;

                string from = fromIds[rng.Next(fromIds.Count)];
                AddEdge(edges, from, toIds[i]);
            }
        }

        static void AddEdge(List<MapGraphDefinition.Edge> edges, string fromId, string toId)
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId) || fromId == toId)
                return;

            for (int i = 0; i < edges.Count; i++)
            {
                MapGraphDefinition.Edge edge = edges[i];
                if (edge == null)
                    continue;

                bool same = edge.FromId == fromId && edge.ToId == toId;
                bool reverse = edge.FromId == toId && edge.ToId == fromId;
                if (same || reverse)
                    return;
            }

            edges.Add(new MapGraphDefinition.Edge { FromId = fromId, ToId = toId });
        }

        static EncounterConfig Pick(IReadOnlyList<EncounterConfig> pool, System.Random rng)
        {
            if (pool == null || pool.Count == 0)
                return null;

            int start = rng.Next(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                EncounterConfig encounter = pool[(start + i) % pool.Count];
                if (encounter != null)
                    return encounter;
            }

            return null;
        }
    }
}
