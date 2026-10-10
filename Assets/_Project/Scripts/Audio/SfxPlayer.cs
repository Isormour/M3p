using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One-shot playback for the clips listed in <see cref="SfxLibrary"/>.
    /// Overlapping events share a small pool. Noisy per-tile cues are quiet and rate-limited.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        const int VoiceCount = 12;

        public static SfxPlayer Instance { get; private set; }

        [SerializeField] SfxLibrary _library;

        AudioSource[] _voices;
        int _nextVoice;
        readonly Dictionary<SfxId, float> _nextAllowed = new Dictionary<SfxId, float>();

        public static void Ensure(SfxLibrary library)
        {
            if (Instance == null)
            {
                var host = new GameObject(nameof(SfxPlayer));
                DontDestroyOnLoad(host);
                Instance = host.AddComponent<SfxPlayer>();
            }

            if (library != null)
                Instance._library = library;
        }

        public static void Play(SfxId id)
        {
            if (id == SfxId.None || Instance == null)
                return;

            Instance.PlayId(id);
        }

        public static void PlaySkill(SkillDefinition skill, bool enemyCaster)
        {
            if (Instance == null)
                return;

            if (skill != null && Instance._library != null
                && Instance._library.TryGetSkill(skill.name, out AudioClip layer, out AudioClip accent))
            {
                Instance.PlayClip(layer, 1f);
                Instance.PlayClip(accent, 1f);
                return;
            }

            Play(enemyCaster ? SfxId.EnemySkillCast : SfxId.PlayerSkillCast);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _voices[i] = source;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void PlayId(SfxId id)
        {
            if (_library == null)
                return;

            float now = Time.unscaledTime;
            if (_nextAllowed.TryGetValue(id, out float allowed) && now < allowed)
                return;

            Playback playback = PlaybackFor(id);
            _nextAllowed[id] = now + playback.MinInterval;
            PlayClip(_library.GetClip(id), playback.Volume);
        }

        void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || _voices == null || _voices.Length == 0)
                return;

            AudioSource source = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            source.PlayOneShot(clip, volume);
        }

        static Playback PlaybackFor(SfxId id)
        {
            switch (id)
            {
                case SfxId.TileBreak:
                    return new Playback(0.4f, 0.06f);
                case SfxId.TileFall:
                case SfxId.TileSpawn:
                    return new Playback(0.45f, 0.18f);
                case SfxId.UiHover:
                    return new Playback(0.28f, 0.08f);
                case SfxId.MapTokenStep:
                    return new Playback(0.7f, 0.22f);
                case SfxId.ManaGain:
                case SfxId.ShardCollect:
                    return new Playback(0.8f, 0.12f);
                case SfxId.TileBonusMana:
                case SfxId.TileBonusHeal:
                case SfxId.TileBonusAction:
                case SfxId.TileBonusShield:
                case SfxId.TileBonusCard:
                case SfxId.TileBonusIgnite:
                case SfxId.TileBonusNeighborBreak:
                    return new Playback(0.85f, 0.08f);
                case SfxId.MultiHitTick:
                case SfxId.StatusExpire:
                    return new Playback(0.8f, 0.05f);
                case SfxId.PanelOpen:
                case SfxId.PanelClose:
                    return new Playback(0.7f, 0.05f);
                default:
                    return new Playback(1f, 0f);
            }
        }

        readonly struct Playback
        {
            public readonly float Volume;
            public readonly float MinInterval;

            public Playback(float volume, float minInterval)
            {
                Volume = volume;
                MinInterval = minInterval;
            }
        }
    }
}
