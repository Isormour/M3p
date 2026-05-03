using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class PlayerProfile
    {
        [SerializeField] int _experience;
        [SerializeField] List<CharacterSkill> _skills = new List<CharacterSkill>();

        public int Experience
        {
            get => _experience;
            set => _experience = value;
        }

        public List<CharacterSkill> Skills => _skills;
    }
}
