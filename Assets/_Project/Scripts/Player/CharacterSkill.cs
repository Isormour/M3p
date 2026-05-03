using System;

namespace M3P
{
    [Serializable]
    public struct CharacterSkill
    {
        public int SkillId;
        public int SkillLevel;
        public string Name;

        public CharacterSkill(int skillId, int skillLevel = 1, string name = "")
        {
            SkillId = skillId;
            SkillLevel = skillLevel;
            Name = name ?? "";
        }
    }
}
