using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Encounter assets a generated floor may roll, harvested from the authored debug graph.</summary>
    public sealed class MapEncounterPools
    {
        public EncounterConfig Start;
        public EncounterConfig Boss;
        public readonly List<EncounterConfig> Battles = new List<EncounterConfig>();
        public readonly List<EncounterConfig> Elites = new List<EncounterConfig>();
        public readonly List<EncounterConfig> Chests = new List<EncounterConfig>();
        public readonly List<EncounterConfig> CardShops = new List<EncounterConfig>();
        public readonly List<EncounterConfig> Forges = new List<EncounterConfig>();

        /// <summary>Harvests encounters from an authored template graph, including the demo boss-by-id fallback.</summary>
        public static MapEncounterPools FromGraph(MapGraphDefinition graph)
        {
            var pools = new MapEncounterPools();
            IReadOnlyList<MapGraphDefinition.Node> nodes = graph != null ? graph.Nodes : null;
            if (nodes == null)
                return pools;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapGraphDefinition.Node node = nodes[i];
                if (node == null || node.Encounter == null)
                    continue;

                MapNodeType type = node.Type;
                if (node.Id != null &&
                    node.Id.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
                    type = MapNodeType.Boss;

                switch (type)
                {
                    case MapNodeType.Start:
                        if (pools.Start == null)
                            pools.Start = node.Encounter;
                        break;
                    case MapNodeType.Chest:
                        if (!pools.Chests.Contains(node.Encounter))
                            pools.Chests.Add(node.Encounter);
                        break;
                    case MapNodeType.Shop:
                        if (!pools.CardShops.Contains(node.Encounter))
                            pools.CardShops.Add(node.Encounter);
                        break;
                    case MapNodeType.Forge:
                        if (!pools.Forges.Contains(node.Encounter))
                            pools.Forges.Add(node.Encounter);
                        break;
                    case MapNodeType.Elite:
                        if (!pools.Elites.Contains(node.Encounter))
                            pools.Elites.Add(node.Encounter);
                        break;
                    case MapNodeType.Boss:
                        if (pools.Boss == null)
                            pools.Boss = node.Encounter;
                        break;
                    default:
                        if (!pools.Battles.Contains(node.Encounter))
                            pools.Battles.Add(node.Encounter);
                        break;
                }
            }

            return pools;
        }

        public EncounterConfig FindByName(string encounterName)
        {
            if (string.IsNullOrEmpty(encounterName))
                return null;

            if (Matches(Start, encounterName))
                return Start;
            if (Matches(Boss, encounterName))
                return Boss;

            return Find(Battles, encounterName)
                ?? Find(Elites, encounterName)
                ?? Find(Chests, encounterName)
                ?? Find(CardShops, encounterName)
                ?? Find(Forges, encounterName);
        }

        static EncounterConfig Find(List<EncounterConfig> pool, string encounterName)
        {
            if (pool == null)
                return null;

            for (int i = 0; i < pool.Count; i++)
            {
                if (Matches(pool[i], encounterName))
                    return pool[i];
            }

            return null;
        }

        static bool Matches(EncounterConfig encounter, string encounterName)
        {
            return encounter != null && encounter.name == encounterName;
        }
    }

    /// <summary>
    /// Builds one dungeon floor as a directed decision graph (Aneks A). Topology and node types
    /// are decided first; world positions are assigned only after the graph validates.
    /// </summary>
    public static class MapGenerator
    {
        public const string GeneratedGraphName = "GeneratedMap";
        public const int DecisionLayerCount = 6;

        enum RouteIdentity
        {
            Power,
            Cards,
            Tiles
        }

        sealed class DraftNode
        {
            public string Id;
            public int Layer;
            public int Slot;
            public MapNodeType Type;
            public EncounterConfig Encounter;
            public Vector2 Position;
            public readonly List<DraftNode> Next = new List<DraftNode>();
        }

        public static MapGraphDefinition Generate(
            MapEncounterPools pools,
            int seed,
            float layerSpacing,
            float nodeSpacing,
            int floorIndex = 1)
        {
            if (pools == null || pools.Battles.Count == 0)
                return null;

            layerSpacing = Mathf.Max(1f, layerSpacing);
            nodeSpacing = Mathf.Max(1f, nodeSpacing);
            floorIndex = Mathf.Max(1, floorIndex);

            MapGraphDefinition graph = null;
            for (int attempt = 0; attempt < 16 && graph == null; attempt++)
            {
                var rng = new System.Random(unchecked(seed + attempt * 9176 + floorIndex * 13));
                graph = TryGenerate(pools, rng, seed, layerSpacing, nodeSpacing);
            }

            return graph;
        }

        static MapGraphDefinition TryGenerate(
            MapEncounterPools pools,
            System.Random rng,
            int seed,
            float layerSpacing,
            float nodeSpacing)
        {
            List<List<DraftNode>> layers = BuildSkeleton(rng);
            ConnectLayers(layers, rng);
            AssignTypes(layers, rng);
            Repair(layers);
            AssignEncounters(layers, pools, rng);
            if (!Validate(layers))
                return null;

            AssignPositions(layers, layerSpacing, nodeSpacing);

            var nodes = new List<MapGraphDefinition.Node>();
            var edges = new List<MapGraphDefinition.Edge>();
            Flatten(layers, nodes, edges);

            var graph = ScriptableObject.CreateInstance<MapGraphDefinition>();
            graph.name = GeneratedGraphName;
            graph.ReplaceContents("start", nodes, edges, directed: true, seed: seed);
            return graph;
        }

        static List<List<DraftNode>> BuildSkeleton(System.Random rng)
        {
            int[] widths = BuildLayerWidths(rng);
            var layers = new List<List<DraftNode>>(DecisionLayerCount + 2);

            layers.Add(new List<DraftNode>
            {
                new DraftNode { Id = "start", Layer = 0, Slot = 0, Type = MapNodeType.Start }
            });

            for (int layer = 1; layer <= DecisionLayerCount; layer++)
            {
                int width = widths[layer - 1];
                var row = new List<DraftNode>(width);
                for (int slot = 0; slot < width; slot++)
                {
                    row.Add(new DraftNode
                    {
                        Id = $"n_{layer}_{slot}",
                        Layer = layer,
                        Slot = slot,
                        Type = MapNodeType.Battle
                    });
                }

                layers.Add(row);
            }

            layers.Add(new List<DraftNode>
            {
                new DraftNode
                {
                    Id = "boss",
                    Layer = DecisionLayerCount + 1,
                    Slot = 0,
                    Type = MapNodeType.Boss
                }
            });

            return layers;
        }

        /// <summary>
        /// Six decision layers, about 12 nodes including start and boss. Extra nodes stay within 12–18.
        /// </summary>
        static int[] BuildLayerWidths(System.Random rng)
        {
            int[] widths = { 2, 2, 2, 2, 1, 1 };
            int extra = rng.Next(0, 4);
            for (int i = 0; i < extra; i++)
            {
                int layer = rng.Next(0, 5);
                if (widths[layer] < 3)
                    widths[layer]++;
            }

            return widths;
        }

        static void ConnectLayers(List<List<DraftNode>> layers, System.Random rng)
        {
            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                List<DraftNode> from = layers[layer];
                List<DraftNode> to = layers[layer + 1];
                var incoming = new int[to.Count];

                for (int i = 0; i < from.Count; i++)
                {
                    int mapped = Mathf.Clamp(
                        Mathf.RoundToInt(i * (to.Count - 1) / (float)Mathf.Max(1, from.Count - 1)),
                        0,
                        to.Count - 1);
                    Link(from[i], to[mapped]);
                    incoming[mapped]++;

                    if (to.Count > 1 && rng.Next(100) < 50)
                    {
                        int side = mapped + (rng.Next(2) == 0 ? -1 : 1);
                        side = Mathf.Clamp(side, 0, to.Count - 1);
                        if (side != mapped)
                        {
                            Link(from[i], to[side]);
                            incoming[side]++;
                        }
                    }
                }

                for (int i = 0; i < to.Count; i++)
                {
                    if (incoming[i] > 0)
                        continue;

                    DraftNode source = from[NearestIndex(i, from.Count, to.Count)];
                    Link(source, to[i]);
                }
            }
        }

        static void AssignTypes(List<List<DraftNode>> layers, System.Random rng)
        {
            for (int layer = 1; layer <= DecisionLayerCount; layer++)
            {
                List<DraftNode> row = layers[layer];
                RouteIdentity[] identities = IdentitiesForRow(row.Count, layer, rng);
                for (int i = 0; i < row.Count; i++)
                    row[i].Type = TypeForIdentity(identities[i], layer, rng);
            }

            layers[0][0].Type = MapNodeType.Start;
            layers[layers.Count - 1][0].Type = MapNodeType.Boss;
        }

        static RouteIdentity[] IdentitiesForRow(int width, int layer, System.Random rng)
        {
            var identities = new RouteIdentity[width];
            if (layer == 1)
            {
                for (int i = 0; i < width; i++)
                    identities[i] = RouteIdentity.Power;
                return identities;
            }

            if (layer == DecisionLayerCount)
            {
                identities[0] = RouteIdentity.Power;
                for (int i = 1; i < width; i++)
                    identities[i] = i % 2 == 0 ? RouteIdentity.Cards : RouteIdentity.Tiles;
                return identities;
            }

            if (width == 1)
            {
                identities[0] = RouteIdentity.Power;
                return identities;
            }

            identities[0] = RouteIdentity.Power;
            var rest = new List<RouteIdentity> { RouteIdentity.Cards, RouteIdentity.Tiles };
            Shuffle(rest, rng);
            for (int i = 1; i < width; i++)
                identities[i] = rest[(i - 1) % rest.Count];

            return identities;
        }

        static MapNodeType TypeForIdentity(RouteIdentity identity, int layer, System.Random rng)
        {
            if (layer == 1)
                return MapNodeType.Battle;

            if (layer == DecisionLayerCount)
                return identity == RouteIdentity.Power ? MapNodeType.Battle : MapNodeType.Chest;

            int roll = rng.Next(100);
            switch (identity)
            {
                case RouteIdentity.Cards:
                    return roll < 45 ? MapNodeType.Shop : MapNodeType.Chest;
                case RouteIdentity.Tiles:
                    return roll < 45 ? MapNodeType.Forge : MapNodeType.Chest;
                default:
                    return roll < 28 ? MapNodeType.Elite : MapNodeType.Battle;
            }
        }

        static void Repair(List<List<DraftNode>> layers)
        {
            List<DraftNode> start = layers[0];
            EnsureOpeningBattles(layers);
            DiversifySiblings(layers);
            BreakConsecutiveRewards(layers);
            EnsureServiceOnAPath(layers, start[0]);
            EnsureOpeningBattles(layers);
        }

        static void EnsureOpeningBattles(List<List<DraftNode>> layers)
        {
            List<DraftNode> first = layers[1];
            for (int i = 0; i < first.Count; i++)
                first[i].Type = MapNodeType.Battle;
        }

        static void DiversifySiblings(List<List<DraftNode>> layers)
        {
            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                {
                    List<DraftNode> next = row[i].Next;
                    if (next.Count < 2)
                        continue;

                    for (int a = 0; a < next.Count; a++)
                    {
                        for (int b = a + 1; b < next.Count; b++)
                        {
                            if (next[a].Type != next[b].Type)
                                continue;

                            if (layer == 0 && next[a].Type == MapNodeType.Battle)
                                continue;

                            next[b].Type = AlternateType(next[a].Type, next[b].Layer);
                        }
                    }
                }
            }
        }

        static void BreakConsecutiveRewards(List<List<DraftNode>> layers)
        {
            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                {
                    DraftNode from = row[i];
                    for (int n = 0; n < from.Next.Count; n++)
                    {
                        DraftNode to = from.Next[n];
                        if (to.Type == MapNodeType.Boss)
                            continue;

                        bool sameService = from.Type == to.Type && from.Type.IsMajorReward();
                        bool stackedRewards = from.Type.IsMajorReward() && to.Type.IsMajorReward();
                        if (sameService || stackedRewards)
                            to.Type = MapNodeType.Battle;
                    }
                }
            }
        }

        static void EnsureServiceOnAPath(List<List<DraftNode>> layers, DraftNode start)
        {
            if (HasServiceOnPath(start, new HashSet<string>()))
                return;

            for (int layer = 3; layer <= Mathf.Min(5, DecisionLayerCount); layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                {
                    DraftNode node = row[i];
                    if (node.Type != MapNodeType.Battle || HasMajorRewardParent(layers, node))
                        continue;

                    node.Type = i == 0 ? MapNodeType.Shop : MapNodeType.Forge;
                    return;
                }
            }

            for (int layer = 3; layer <= Mathf.Min(5, DecisionLayerCount); layer++)
            {
                if (layers[layer].Count == 0)
                    continue;

                layers[layer][0].Type = MapNodeType.Shop;
                return;
            }
        }

        static bool HasMajorRewardParent(List<List<DraftNode>> layers, DraftNode node)
        {
            if (node.Layer <= 0)
                return false;

            List<DraftNode> previous = layers[node.Layer - 1];
            for (int i = 0; i < previous.Count; i++)
            {
                DraftNode parent = previous[i];
                if (!parent.Next.Contains(node))
                    continue;

                if (parent.Type.IsMajorReward())
                    return true;
            }

            return false;
        }

        static bool HasServiceOnPath(DraftNode node, HashSet<string> visiting)
        {
            if (node.Type.IsService())
                return true;

            if (!visiting.Add(node.Id))
                return false;

            for (int i = 0; i < node.Next.Count; i++)
            {
                if (HasServiceOnPath(node.Next[i], visiting))
                    return true;
            }

            visiting.Remove(node.Id);
            return false;
        }

        static MapNodeType AlternateType(MapNodeType current, int layer)
        {
            if (layer == DecisionLayerCount)
                return current == MapNodeType.Chest ? MapNodeType.Battle : MapNodeType.Chest;

            switch (current)
            {
                case MapNodeType.Battle: return MapNodeType.Chest;
                case MapNodeType.Chest: return MapNodeType.Battle;
                case MapNodeType.Shop: return MapNodeType.Forge;
                case MapNodeType.Forge: return MapNodeType.Shop;
                case MapNodeType.Elite: return MapNodeType.Battle;
                default: return MapNodeType.Battle;
            }
        }

        static void AssignEncounters(List<List<DraftNode>> layers, MapEncounterPools pools, System.Random rng)
        {
            for (int layer = 0; layer < layers.Count; layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                    row[i].Encounter = PickEncounter(pools, row[i].Type, rng);
            }
        }

        static EncounterConfig PickEncounter(MapEncounterPools pools, MapNodeType type, System.Random rng)
        {
            switch (type)
            {
                case MapNodeType.Start: return pools.Start;
                case MapNodeType.Boss: return pools.Boss ?? Pick(pools.Battles, rng);
                case MapNodeType.Elite: return Pick(pools.Elites, rng) ?? Pick(pools.Battles, rng);
                case MapNodeType.Chest: return Pick(pools.Chests, rng);
                case MapNodeType.Shop: return Pick(pools.CardShops, rng);
                case MapNodeType.Forge: return Pick(pools.Forges, rng) ?? Pick(pools.CardShops, rng);
                default: return Pick(pools.Battles, rng);
            }
        }

        static bool Validate(List<List<DraftNode>> layers)
        {
            DraftNode start = layers[0][0];
            DraftNode boss = layers[layers.Count - 1][0];
            if (!CanReach(start, boss.Id, new HashSet<string>()))
                return false;

            var reachable = new HashSet<string>();
            CollectReachable(start, reachable);
            for (int layer = 0; layer < layers.Count; layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                {
                    if (!reachable.Contains(row[i].Id))
                        return false;
                }
            }

            if (!HasServiceOnPath(start, new HashSet<string>()))
                return false;

            if (!OpeningHasBattleBeforeService(start))
                return false;

            if (boss.Encounter == null)
                return false;

            if (HasStackedMajorRewards(start, new HashSet<string>()))
                return false;

            return true;
        }

        static bool HasStackedMajorRewards(DraftNode node, HashSet<string> visiting)
        {
            if (!visiting.Add(node.Id))
                return false;

            for (int i = 0; i < node.Next.Count; i++)
            {
                DraftNode next = node.Next[i];
                if (next.Type != MapNodeType.Boss &&
                    node.Type.IsMajorReward() &&
                    next.Type.IsMajorReward())
                    return true;

                if (HasStackedMajorRewards(next, visiting))
                    return true;
            }

            return false;
        }

        static bool OpeningHasBattleBeforeService(DraftNode start)
        {
            for (int i = 0; i < start.Next.Count; i++)
            {
                if (start.Next[i].Type != MapNodeType.Battle)
                    return false;
            }

            return start.Next.Count > 0;
        }

        static bool CanReach(DraftNode node, string targetId, HashSet<string> visiting)
        {
            if (node.Id == targetId)
                return true;

            if (!visiting.Add(node.Id))
                return false;

            for (int i = 0; i < node.Next.Count; i++)
            {
                if (CanReach(node.Next[i], targetId, visiting))
                    return true;
            }

            visiting.Remove(node.Id);
            return false;
        }

        static void CollectReachable(DraftNode node, HashSet<string> reachable)
        {
            if (!reachable.Add(node.Id))
                return;

            for (int i = 0; i < node.Next.Count; i++)
                CollectReachable(node.Next[i], reachable);
        }

        static void AssignPositions(List<List<DraftNode>> layers, float layerSpacing, float nodeSpacing)
        {
            for (int layer = 0; layer < layers.Count; layer++)
            {
                List<DraftNode> row = layers[layer];
                float originX = -(row.Count - 1) * 0.5f * nodeSpacing;
                for (int i = 0; i < row.Count; i++)
                    row[i].Position = new Vector2(originX + i * nodeSpacing, layer * layerSpacing);
            }
        }

        static void Flatten(
            List<List<DraftNode>> layers,
            List<MapGraphDefinition.Node> nodes,
            List<MapGraphDefinition.Edge> edges)
        {
            for (int layer = 0; layer < layers.Count; layer++)
            {
                List<DraftNode> row = layers[layer];
                for (int i = 0; i < row.Count; i++)
                {
                    DraftNode draft = row[i];
                    nodes.Add(new MapGraphDefinition.Node
                    {
                        Id = draft.Id,
                        Encounter = draft.Encounter,
                        Type = draft.Type,
                        Position = draft.Position
                    });

                    for (int n = 0; n < draft.Next.Count; n++)
                        AddDirectedEdge(edges, draft.Id, draft.Next[n].Id);
                }
            }
        }

        static void Link(DraftNode from, DraftNode to)
        {
            if (from == null || to == null || from == to)
                return;

            if (!from.Next.Contains(to))
                from.Next.Add(to);
        }

        static void AddDirectedEdge(List<MapGraphDefinition.Edge> edges, string fromId, string toId)
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId) || fromId == toId)
                return;

            for (int i = 0; i < edges.Count; i++)
            {
                MapGraphDefinition.Edge edge = edges[i];
                if (edge != null && edge.FromId == fromId && edge.ToId == toId)
                    return;
            }

            edges.Add(new MapGraphDefinition.Edge { FromId = fromId, ToId = toId });
        }

        static int NearestIndex(int toIndex, int fromCount, int toCount)
        {
            if (fromCount <= 1)
                return 0;

            return Mathf.Clamp(
                Mathf.RoundToInt(toIndex * (fromCount - 1) / (float)Mathf.Max(1, toCount - 1)),
                0,
                fromCount - 1);
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }

        static EncounterConfig Pick(List<EncounterConfig> pool, System.Random rng)
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
