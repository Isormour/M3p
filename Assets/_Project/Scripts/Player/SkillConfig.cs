using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The registry of every skill in the game and the one place skill ids come from. Ids are handed
    /// out once and never reused, so a skill referenced by a saved profile survives the list being
    /// reordered or trimmed.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "M3P/Skill Config", order = 1)]
    public class SkillConfig : ScriptableObject
    {
        /// <summary>Id of a skill that this config does not know about.</summary>
        public const int InvalidSkillId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans the skill in every profile that saved it.")]
            public int Id;
            public SkillDefinition Skill;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Only ever counts up, so removing a skill does not free its id for the next one.</summary>
        [SerializeField, HideInInspector] int _nextSkillId = InvalidSkillId + 1;

        Dictionary<SkillDefinition, int> _idsBySkill;
        Dictionary<int, SkillDefinition> _skillsById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        /// <summary>Id of a registered skill, or <see cref="InvalidSkillId"/> when it is not in this config.</summary>
        public int GetSkillId(SkillDefinition skill)
        {
            if (skill == null)
                return InvalidSkillId;

            EnsureLookups();
            return _idsBySkill.TryGetValue(skill, out int id) ? id : InvalidSkillId;
        }

        public bool TryGetSkill(int skillId, out SkillDefinition skill)
        {
            EnsureLookups();
            return _skillsById.TryGetValue(skillId, out skill);
        }

        public SkillDefinition GetSkill(int skillId)
        {
            return TryGetSkill(skillId, out SkillDefinition skill) ? skill : null;
        }

        void EnsureLookups()
        {
            if (_idsBySkill != null)
                return;

            Entry[] entries = Entries;
            _idsBySkill = new Dictionary<SkillDefinition, int>(entries.Length);
            _skillsById = new Dictionary<int, SkillDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Skill == null || entry.Id == InvalidSkillId)
                    continue;

                if (_skillsById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(SkillConfig)} '{name}': skill id {entry.Id} is used twice. Give '{entry.Skill.name}' a unique id.",
                        this);
                    continue;
                }

                _idsBySkill[entry.Skill] = entry.Id;
                _skillsById[entry.Id] = entry.Skill;
            }
        }

        /// <summary>
        /// Hands the next free id to every newly added skill. Existing ids are left alone so nothing
        /// already written to a save changes meaning.
        /// </summary>
        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            // Catches an id typed in by hand that sits above the counter.
            for (int i = 0; i < _entries.Length; i++)
                _nextSkillId = Mathf.Max(_nextSkillId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Skill == null || _entries[i].Id > InvalidSkillId)
                    continue;

                _entries[i].Id = _nextSkillId++;
            }
        }

        void OnEnable()
        {
            _idsBySkill = null;
            _skillsById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsBySkill = null;
            _skillsById = null;
        }
    }
}
