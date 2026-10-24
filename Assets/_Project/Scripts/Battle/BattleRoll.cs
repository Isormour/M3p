using System;

namespace M3P
{
    /// <summary>
    /// Enemy stream for one battle: skill choice, mana-colour drains, and an off-map enemy pick.
    /// <see cref="Begin"/> starts it from the enemy seed before the first of those rolls.
    /// </summary>
    public static class BattleRoll
    {
        static System.Random _random = new System.Random(0);

        public static System.Random Source => _random;

        public static void Begin(int streamSeed)
        {
            _random = new System.Random(streamSeed);
        }

        public static void End()
        {
            _random = new System.Random(0);
        }

        public static int NextIndex(int count)
        {
            if (count <= 0)
                return -1;

            return _random.Next(count);
        }
    }
}
