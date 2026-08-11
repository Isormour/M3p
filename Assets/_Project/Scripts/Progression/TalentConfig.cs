using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Registry of every talent in the game and the ids profiles use to remember picks.</summary>
    [CreateAssetMenu(fileName = "TalentConfig", menuName = "M3P/Talent Config", order = 5)]
    public class TalentConfig : ScriptableObject
    {
        public const int InvalidTalentId = 0;

        [Serializable]
        public struct Entry
        {
            [Tooltip("Assigned automatically. Editing it orphans the talent in every profile that saved it.")]
            public int Id;
            public TalentDefinition Talent;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();
        [SerializeField, HideInInspector] int _nextTalentId = InvalidTalentId + 1;

        Dictionary<TalentDefinition, int> _idsByTalent;
        Dictionary<int, TalentDefinition> _talentsById;

        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        public int GetTalentId(TalentDefinition talent)
        {
            if (talent == null)
                return InvalidTalentId;

            EnsureLookups();
            return _idsByTalent.TryGetValue(talent, out int id) ? id : InvalidTalentId;
        }

        public bool TryGetTalent(int talentId, out TalentDefinition talent)
        {
            EnsureLookups();
            return _talentsById.TryGetValue(talentId, out talent);
        }

        public TalentDefinition GetTalent(int talentId)
        {
            return TryGetTalent(talentId, out TalentDefinition talent) ? talent : null;
        }

        public IReadOnlyList<TalentDefinition> GetChoices(EStatType stat, int milestoneTier)
        {
            List<TalentDefinition> choices = new List<TalentDefinition>();
            Entry[] entries = Entries;

            for (int i = 0; i < entries.Length; i++)
            {
                TalentDefinition talent = entries[i].Talent;
                if (talent == null || talent.Stat != stat || talent.MilestoneTier != milestoneTier)
                    continue;

                choices.Add(talent);
            }

            return choices;
        }

        public TalentBonuses BuildBonuses(IReadOnlyList<int> unlockedTalentIds)
        {
            TalentBonuses total = TalentBonuses.None;

            if (unlockedTalentIds == null)
                return total;

            for (int i = 0; i < unlockedTalentIds.Count; i++)
            {
                if (!TryGetTalent(unlockedTalentIds[i], out TalentDefinition talent))
                    continue;

                total = TalentBonuses.Combine(total, talent.ToBonuses());
            }

            return total;
        }

        void EnsureLookups()
        {
            if (_idsByTalent != null)
                return;

            Entry[] entries = Entries;
            _idsByTalent = new Dictionary<TalentDefinition, int>(entries.Length);
            _talentsById = new Dictionary<int, TalentDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry.Talent == null || entry.Id == InvalidTalentId)
                    continue;

                if (_talentsById.ContainsKey(entry.Id))
                {
                    Debug.LogError(
                        $"{nameof(TalentConfig)} '{name}': talent id {entry.Id} is used twice. Give '{entry.Talent.name}' a unique id.",
                        this);
                    continue;
                }

                _idsByTalent[entry.Talent] = entry.Id;
                _talentsById[entry.Id] = entry.Talent;
            }
        }

        void AssignMissingIds()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Length; i++)
                _nextTalentId = Mathf.Max(_nextTalentId, _entries[i].Id + 1);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Talent == null || _entries[i].Id > InvalidTalentId)
                    continue;

                _entries[i].Id = _nextTalentId++;
            }
        }

        void OnEnable()
        {
            _idsByTalent = null;
            _talentsById = null;
        }

        void OnValidate()
        {
            AssignMissingIds();
            _idsByTalent = null;
            _talentsById = null;
        }
    }
}
