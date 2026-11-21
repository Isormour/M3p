using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace M3P
{
    /// <summary>
    /// Playback for the clips listed in <see cref="SfxLibrary"/>.
    /// One-shots share a small pool; controlled loops use separate sources.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        const int VoiceCount = 12;

        public static SfxPlayer Instance { get; private set; }

        [SerializeField] SfxLibrary _library;

        AudioSource[] _voices;
        int _nextVoice;
        readonly Dictionary<SfxId, float> _nextAllowed = new Dictionary<SfxId, float>();
        readonly Dictionary<SfxId, AudioSource> _loopVoices = new Dictionary<SfxId, AudioSource>();

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

            Instance.RouteAll();
        }

        public static void ApplyRoute()
        {
            if (Instance != null)
                Instance.RouteAll();
        }

        public static void Play(SfxId id)
        {
            if (id == SfxId.None || Instance == null)
                return;

            Instance.PlayId(id);
        }

        public static void StartLoop(SfxId id)
        {
            if (id == SfxId.None || Instance == null)
                return;

            Instance.StartLoopId(id);
        }

        public static void StopLoop(SfxId id)
        {
            if (Instance == null || !Instance._loopVoices.TryGetValue(id, out AudioSource source) || source == null)
                return;

            source.Stop();
            source.clip = null;
        }

        public static void PlaySkill(SkillDefinition skill, bool enemyCaster)
        {
            if (Instance == null)
                return;

            if (skill != null && Instance._library != null
                && Instance._library.TryGetSkill(skill.name, out AudioClip layer, out AudioClip accent))
            {
                Instance.PlayClip(layer, 1f);
                if (accent != null)
                {
                    Instance.PlayClip(accent, 1f);
                    return;
                }
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
                _voices[i] = CreateVoice(false);
            RouteAll();
        }

        void OnEnable()
        {
            if (Instance != this)
                return;

            AudioPreferences.Changed += OnPreferencesChanged;
        }

        void OnDisable()
        {
            AudioPreferences.Changed -= OnPreferencesChanged;
        }

        void OnPreferencesChanged()
        {
            RouteAll();
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

        void StartLoopId(SfxId id)
        {
            if (_library == null)
                return;

            if (_loopVoices.TryGetValue(id, out AudioSource source) && source != null && source.isPlaying)
                return;

            AudioClip clip = _library.GetClip(id);
            if (clip == null)
                return;

            if (source == null)
            {
                source = CreateVoice(true);
                _loopVoices[id] = source;
            }

            source.clip = clip;
            source.volume = PlaybackFor(id).Volume * GameAudio.SfxSourceVolume;
            source.Play();
        }

        AudioSource CreateVoice(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = loop;
            AssignGroup(source);
            source.volume = GameAudio.SfxSourceVolume;
            return source;
        }

        void RouteAll()
        {
            float gain = GameAudio.SfxSourceVolume;
            if (_voices != null)
            {
                for (int i = 0; i < _voices.Length; i++)
                {
                    AudioSource source = _voices[i];
                    if (source == null)
                        continue;

                    AssignGroup(source);
                    source.volume = gain;
                }
            }

            foreach (KeyValuePair<SfxId, AudioSource> pair in _loopVoices)
            {
                AudioSource source = pair.Value;
                if (source == null)
                    continue;

                AssignGroup(source);
                source.volume = PlaybackFor(pair.Key).Volume * gain;
            }
        }

        static void AssignGroup(AudioSource source)
        {
            if (source == null)
                return;

            AudioMixerGroup group = GameAudio.SfxGroup;
            if (group != null)
                source.outputAudioMixerGroup = group;
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
