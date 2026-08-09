using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class PlayerProfile
    {
        public int Level = LevelProgressionConfig.FirstLevel;

        /// <summary>Lifetime experience. <see cref="Level"/> is what the curve has already paid out for it.</summary>
        public int Experience;

        public int UnspentStatPoints;
        public List<CharacterSkill> Skills = new List<CharacterSkill>();
        public List<ShardAmount> Shards = new List<ShardAmount>();
        public HardStats HardStats;

        public PlayerProfile()
        {
            HardStats = new HardStats(1, 1, 1, 1);
        }

        public CharacterStats CreateBattleStats()
        {
            CharacterStats stats = new CharacterStats(HardStats);
            stats.RecalculateSoftStatsForBattle();
            return stats;
        }

        /// <summary>Spends one level-up point on a stat. Returns false when there is nothing to spend.</summary>
        public bool TrySpendStatPoint(EStatType stat)
        {
            if (UnspentStatPoints <= 0)
                return false;

            HardStats = HardStats.WithPointsAdded(stat);
            UnspentStatPoints--;
            return true;
        }

        /// <summary>Shards of one colour currently banked, or zero for a colour never earned.</summary>
        public int GetShards(string tileType)
        {
            int index = IndexOfShards(tileType);
            return index >= 0 ? Shards[index].Amount : 0;
        }

        /// <summary>Banks shards of one colour. Amounts of zero or less are ignored.</summary>
        public void AddShards(string tileType, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(tileType))
                return;

            int index = IndexOfShards(tileType);
            if (index >= 0)
                Shards[index] = new ShardAmount(tileType, Shards[index].Amount + amount);
            else
                Shards.Add(new ShardAmount(tileType, amount));
        }

        int IndexOfShards(string tileType)
        {
            if (Shards == null || string.IsNullOrEmpty(tileType))
                return -1;

            for (int i = 0; i < Shards.Count; i++)
            {
                if (string.Equals(Shards[i].TileType, tileType, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        /// <summary>Fills in fields a save written before levels existed never stored.</summary>
        public void NormalizeAfterLoad()
        {
            Level = Math.Max(LevelProgressionConfig.FirstLevel, Level);
            Experience = Math.Max(0, Experience);
            UnspentStatPoints = Math.Max(0, UnspentStatPoints);
            Skills ??= new List<CharacterSkill>();
            Shards ??= new List<ShardAmount>();
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(PlayerProfileSaveData.FromProfile(this), prettyPrint);
        }

        public static PlayerProfile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new PlayerProfile();

            PlayerProfileSaveData data = JsonUtility.FromJson<PlayerProfileSaveData>(json);
            return data.ToProfile();
        }

        public void CopyFrom(PlayerProfile source)
        {
            if (source == null)
                return;

            Level = source.Level;
            Experience = source.Experience;
            UnspentStatPoints = source.UnspentStatPoints;
            Skills = source.Skills != null
                ? new List<CharacterSkill>(source.Skills)
                : new List<CharacterSkill>();
            Shards = source.Shards != null
                ? new List<ShardAmount>(source.Shards)
                : new List<ShardAmount>();
            HardStats = source.HardStats;
        }

        [Serializable]
        struct PlayerProfileSaveData
        {
            public int Level;
            public int Experience;
            public int UnspentStatPoints;
            public CharacterSkill[] Skills;
            public ShardAmount[] Shards;
            public HardStats HardStats;

            public static PlayerProfileSaveData FromProfile(PlayerProfile profile)
            {
                return new PlayerProfileSaveData
                {
                    Level = profile.Level,
                    Experience = profile.Experience,
                    UnspentStatPoints = profile.UnspentStatPoints,
                    Skills = profile.Skills != null ? profile.Skills.ToArray() : Array.Empty<CharacterSkill>(),
                    Shards = profile.Shards != null ? profile.Shards.ToArray() : Array.Empty<ShardAmount>(),
                    HardStats = profile.HardStats,
                };
            }

            public PlayerProfile ToProfile()
            {
                return new PlayerProfile
                {
                    Level = Level,
                    Experience = Experience,
                    UnspentStatPoints = UnspentStatPoints,
                    Skills = Skills != null
                        ? new List<CharacterSkill>(Skills)
                        : new List<CharacterSkill>(),
                    Shards = Shards != null
                        ? new List<ShardAmount>(Shards)
                        : new List<ShardAmount>(),
                    HardStats = HardStats,
                };
            }
        }
    }
}
