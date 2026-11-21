using UnityEngine;
using UnityEngine.Audio;

namespace M3P
{
    /// <summary>
    /// Routes music and SFX through <see cref="AudioMixer"/> groups and applies the saved loudness.
    /// </summary>
    public static class GameAudio
    {
        public const string MusicGroupName = "Music";
        public const string SfxGroupName = "SFX";
        public const string MusicVolumeParameter = "MusicVolume";
        public const string SfxVolumeParameter = "SfxVolume";

        static AudioMixer _mixer;
        static AudioMixerGroup _musicGroup;
        static AudioMixerGroup _sfxGroup;
        static bool _sfxParam;
        static bool _warnedSfx;

        public static AudioMixerGroup MusicGroup => _musicGroup;
        public static AudioMixerGroup SfxGroup => _sfxGroup;

        public static float SfxSourceVolume => _sfxParam ? 1f : AudioPreferences.Sfx;

        public static void Install(AudioMixer mixer)
        {
            AudioPreferences.Load();
            _mixer = mixer;
            _musicGroup = Find(mixer, MusicGroupName);
            _sfxGroup = Find(mixer, SfxGroupName);
            _warnedSfx = false;
            Apply();
            SfxPlayer.ApplyRoute();
            MusicPlayer.ApplyRoute();
        }

        public static void Apply()
        {
            AudioPreferences.Load();
            // Music percent is the AudioSource volume. Keep this bus at unity so it does not stack.
            if (_mixer != null)
                _mixer.SetFloat(MusicVolumeParameter, 0f);
            _sfxParam = Set(_mixer, SfxVolumeParameter, AudioPreferences.Sfx, ref _warnedSfx, SfxGroupName);
        }

        public static float ToDecibels(float linear)
        {
            if (linear <= 0.0001f)
                return -80f;

            return Mathf.Clamp(Mathf.Log10(linear) * 20f, -80f, 0f);
        }

        static bool Set(AudioMixer mixer, string parameter, float linear, ref bool warned, string groupName)
        {
            if (mixer == null)
                return false;

            bool set = mixer.SetFloat(parameter, ToDecibels(linear));
            if (!set && !warned)
            {
                warned = true;
                Debug.LogWarning(
                    $"GameAudio: {parameter} is not exposed on the mixer. {groupName} loudness falls back to the AudioSource.");
            }

            return set;
        }

        static AudioMixerGroup Find(AudioMixer mixer, string groupName)
        {
            if (mixer == null || string.IsNullOrEmpty(groupName))
                return null;

            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].name == groupName)
                    return groups[i];
            }

            return null;
        }
    }
}
