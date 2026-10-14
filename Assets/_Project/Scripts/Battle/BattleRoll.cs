using System;

namespace M3P
{
    /// <summary>
    /// Deterministic index picks for enemy skills. The sequence restarts at the beginning of each battle
    /// and follows the map-run seed.
    /// </summary>
    public static class BattleRoll
    {
        static int _cursor;

        public static void Reset()
        {
            _cursor = 0;
        }

        public static int NextIndex(int count)
        {
            if (count <= 0)
                return -1;

            int seed = 1;
            MapRunState run = MapRunState.Active;
            if (run != null)
            {
                int graphSeed = run.GraphSnapshot != null ? run.GraphSnapshot.Seed : 0;
                seed = unchecked(graphSeed + run.FloorIndex * 13);
            }

            _cursor++;
            var rng = new Random(unchecked(seed + _cursor * 997));
            return rng.Next(count);
        }
    }
}
