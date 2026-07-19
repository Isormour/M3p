using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class PlayerProfile
    {
        public int Experience;
        public List<CharacterSkill> Skills = new List<CharacterSkill>();
        public HardStats HardStats;

        public PlayerProfile()
        {
            HardStats = new HardStats(1, 1, 1);
        }

        public CharacterStats CreateBattleStats()
        {
            CharacterStats stats = new CharacterStats(HardStats);
            stats.RecalculateSoftStatsForBattle();
            return stats;
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

        public void SaveToJsonFile(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, ToJson());
        }

        public static PlayerProfile LoadFromJsonFile(string path)
        {
            if (!File.Exists(path))
                return new PlayerProfile();

            return FromJson(File.ReadAllText(path));
        }

        public static string DefaultSavePath =>
            Path.Combine(Application.persistentDataPath, "player_profile.json");

        public void Save()
        {
            SaveToJsonFile(DefaultSavePath);
        }

        public static PlayerProfile Load()
        {
            return LoadFromJsonFile(DefaultSavePath);
        }

        public void CopyFrom(PlayerProfile source)
        {
            if (source == null)
                return;

            Experience = source.Experience;
            Skills = source.Skills != null
                ? new List<CharacterSkill>(source.Skills)
                : new List<CharacterSkill>();
            HardStats = source.HardStats;
        }

        [Serializable]
        struct PlayerProfileSaveData
        {
            public int Experience;
            public CharacterSkill[] Skills;
            public HardStats HardStats;

            public static PlayerProfileSaveData FromProfile(PlayerProfile profile)
            {
                return new PlayerProfileSaveData
                {
                    Experience = profile.Experience,
                    Skills = profile.Skills != null ? profile.Skills.ToArray() : Array.Empty<CharacterSkill>(),
                    HardStats = profile.HardStats,
                };
            }

            public PlayerProfile ToProfile()
            {
                return new PlayerProfile
                {
                    Experience = Experience,
                    Skills = Skills != null
                        ? new List<CharacterSkill>(Skills)
                        : new List<CharacterSkill>(),
                    HardStats = HardStats,
                };
            }
        }
    }
}
