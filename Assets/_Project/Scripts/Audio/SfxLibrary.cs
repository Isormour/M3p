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

        Dictionary<SfxId, AudioClip> _byId;
        Dictionary<string, SkillClip> _bySkill;

        public AudioClip GetClip(SfxId id)
        {
            if (id == SfxId.None)
                return null;

            EnsureLookups();
            return _byId.TryGetValue(id, out AudioClip clip) ? clip : null;
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
            _bySkill = null;
        }

        void EnsureLookups()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<SfxId, AudioClip>();
            ClipEntry[] clips = _clips ?? Array.Empty<ClipEntry>();
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].Id == SfxId.None || clips[i].Clip == null)
                    continue;

                _byId[clips[i].Id] = clips[i].Clip;
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
