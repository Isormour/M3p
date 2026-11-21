using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Music and SFX loudness for this install. Stored in PlayerPrefs, separate from the profile save.
    /// </summary>
    public static class AudioPreferences
    {
        public const string MusicKey = "M3P.MusicVolume";
        public const string SfxKey = "M3P.SfxVolume";
        public const float DefaultVolume = 0.1f;

        public static float Music { get; private set; } = DefaultVolume;
        public static float Sfx { get; private set; } = DefaultVolume;

        public static event Action Changed;

        static bool _loaded;

        public static void Load()
        {
            if (_loaded)
                return;

            Music = Read(MusicKey);
            Sfx = Read(SfxKey);
            _loaded = true;
        }

        public static void SetMusic(float volume)
        {
            Load();
            Set(MusicKey, ref volume, Music, value => Music = value);
        }

        public static void SetSfx(float volume)
        {
            Load();
            Set(SfxKey, ref volume, Sfx, value => Sfx = value);
        }

        static void Set(string key, ref float volume, float current, Action<float> assign)
        {
            volume = Mathf.Clamp01(volume);
            bool same = Mathf.Approximately(current, volume);
            assign(volume);
            if (!same || !PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetFloat(key, volume);
                PlayerPrefs.Save();
            }

            GameAudio.Apply();
            if (!same && Changed != null)
                Changed();
        }

        static float Read(string key)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
        }
    }
}
