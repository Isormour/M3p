using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The build a character begins the game with, used to seed a profile that has never been saved.
    /// Skills are authored as assets here and stored as ids in the profile.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStartConfig", menuName = "M3P/Player Start Config", order = 2)]
    public class PlayerStartConfig : ScriptableObject
    {
        [SerializeField] HardStats _hardStats = new HardStats(1, 1, 1, 1);

        [Tooltip("Skills the character owns from the first battle. Each must be registered in the skill config.")]
        [SerializeField] SkillDefinition[] _skills = Array.Empty<SkillDefinition>();

        public HardStats HardStats => _hardStats;

        public SkillDefinition[] Skills => _skills ?? Array.Empty<SkillDefinition>();

        public PlayerProfile CreateProfile(SkillConfig skillConfig)
        {
            PlayerProfile profile = new PlayerProfile { HardStats = _hardStats };

            SkillDefinition[] skills = Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                    continue;

                int skillId = skillConfig != null ? skillConfig.GetSkillId(skill) : SkillConfig.InvalidSkillId;
                if (skillId == SkillConfig.InvalidSkillId)
                {
                    Debug.LogError(
                        $"{nameof(PlayerStartConfig)} '{name}': skill '{skill.name}' is not registered in {nameof(SkillConfig)}, so it cannot be saved to a profile.",
                        this);
                    continue;
                }

                profile.Skills.Add(new CharacterSkill(skillId, 1, skill.name));
            }

            return profile;
        }
    }
}
