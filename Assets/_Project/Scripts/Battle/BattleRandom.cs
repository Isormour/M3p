using System;

namespace M3P
{
    /// <summary>
    /// Splits one battle seed into four streams that do not advance each other: board, card deck,
    /// random card commands, and the enemy. Visual effects stay on <see cref="UnityEngine.Random"/>.
    /// The salts are part of the battle rules; changing them changes every derived sequence.
    /// </summary>
    public static class BattleRandom
    {
        const int BoardSalt = 0x4D335042;
        const int DeckSalt = 0x4D335044;
        const int CommandSalt = 0x4D335043;
        const int EnemySalt = 0x4D335045;

        public readonly struct Streams
        {
            public Streams(int battleSeed, int board, int deck, int commands, int enemy)
            {
                BattleSeed = battleSeed;
                Board = board;
                Deck = deck;
                Commands = commands;
                Enemy = enemy;
            }

            public int BattleSeed { get; }
            public int Board { get; }
            public int Deck { get; }
            public int Commands { get; }
            public int Enemy { get; }
        }

        public static Streams Create(int battleSeed)
        {
            return new Streams(
                battleSeed,
                Mix(battleSeed, BoardSalt),
                Mix(battleSeed, DeckSalt),
                Mix(battleSeed, CommandSalt),
                Mix(battleSeed, EnemySalt));
        }

        /// <summary>
        /// Seed for the battle being opened. A map node is stable for the run. A fight with no pending
        /// node takes one clock sample and fans that single value out into the four streams.
        /// </summary>
        public static int ResolveBattleSeed()
        {
            MapRunState run = MapRunState.Active;
            if (run != null && run.IsActive && run.HasPendingBattle)
                return DeriveFromRun(run);

            return Environment.TickCount;
        }

        static int DeriveFromRun(MapRunState run)
        {
            int graphSeed = run.GraphSnapshot != null
                ? run.GraphSnapshot.Seed
                : HashStable(run.GraphName);
            int identity = Mix(graphSeed, run.FloorIndex);
            return Mix(identity, HashStable(run.PendingBattleNodeId));
        }

        /// <summary>Process-stable string hash. <see cref="string.GetHashCode"/> is not.</summary>
        public static int HashStable(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }

        static int Mix(int value, int salt)
        {
            unchecked
            {
                uint x = (uint)value;
                x ^= (uint)salt + 0x9E3779B9u;
                x *= 0x85EBCA6Bu;
                x ^= x >> 13;
                x *= 0xC2B2AE35u;
                x ^= x >> 16;
                return (int)x;
            }
        }
    }
}
