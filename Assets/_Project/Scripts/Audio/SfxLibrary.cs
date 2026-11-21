using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "M3P/SFX Library", order = 20)]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public struct ClipEntry
        {
            public SfxId Id;
            public AudioClip Clip;
            public AudioClip[] Alternates;
        }

        [Serializable]
        public struct SkillClip
        {
            public string SkillAssetName;
            public AudioClip Layer;
            public AudioClip Accent;
        }

        [SerializeField] ClipEntry[] _clips = Array.Empty<ClipEntry>();
        [SerializeField] SkillClip[] _skills = Array.Empty<SkillClip>();

        Dictionary<SfxId, AudioClip[]> _byId;
        Dictionary<SfxId, int> _nextClip;
        Dictionary<string, SkillClip> _bySkill;

        public AudioClip GetClip(SfxId id)
        {
            if (id == SfxId.None)
                return null;

            EnsureLookups();
            if (!_byId.TryGetValue(id, out AudioClip[] clips))
                return null;

            _nextClip.TryGetValue(id, out int index);
            _nextClip[id] = (index + 1) % clips.Length;
            return clips[index];
        }

        public bool TryGetSkill(string skillAssetName, out AudioClip layer, out AudioClip accent)
        {
            layer = null;
            accent = null;
            if (string.IsNullOrEmpty(skillAssetName))
                return false;

            EnsureLookups();
            if (!_bySkill.TryGetValue(skillAssetName, out SkillClip entry))
                return false;

            layer = entry.Layer;
            accent = entry.Accent;
            return layer != null || accent != null;
        }

        void OnEnable()
        {
            _byId = null;
            _nextClip = null;
            _bySkill = null;
        }

        void EnsureLookups()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<SfxId, AudioClip[]>();
            _nextClip = new Dictionary<SfxId, int>();
            ClipEntry[] clips = _clips ?? Array.Empty<ClipEntry>();
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].Id == SfxId.None)
                    continue;

                var variants = new List<AudioClip>();
                if (clips[i].Clip != null)
                    variants.Add(clips[i].Clip);
                foreach (AudioClip alternate in clips[i].Alternates ?? Array.Empty<AudioClip>())
                {
                    if (alternate != null)
                        variants.Add(alternate);
                }

                if (variants.Count > 0)
                    _byId[clips[i].Id] = variants.ToArray();
            }

            _bySkill = new Dictionary<string, SkillClip>(StringComparer.Ordinal);
            SkillClip[] skills = _skills ?? Array.Empty<SkillClip>();
            for (int i = 0; i < skills.Length; i++)
            {
                if (string.IsNullOrEmpty(skills[i].SkillAssetName))
                    continue;

                _bySkill[skills[i].SkillAssetName] = skills[i];
            }
        }
    }
}
