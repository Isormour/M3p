using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace M3P
{
    /// <summary>
    /// Looping scene music on the Music mixer group. One player survives scene loads.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        const float FadeSeconds = 0.45f;

        public static MusicPlayer Instance { get; private set; }

        MusicLibrary _library;
        AudioSource _source;
        Coroutine _fade;
        AudioClip _current;
        float _fadeLevel = 1f;
        int _token;

        public static void Ensure(MusicLibrary library)
        {
            if (Instance == null)
            {
                var host = new GameObject(nameof(MusicPlayer));
                DontDestroyOnLoad(host);
                Instance = host.AddComponent<MusicPlayer>();
            }

            if (library != null)
                Instance._library = library;

            Instance.Route();
            Instance.PlayActiveScene();
        }

        public static void ApplyRoute()
        {
            if (Instance != null)
                Instance.Route();
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
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            Route();
        }

        void OnEnable()
        {
            if (Instance != this)
                return;

            AudioPreferences.Changed += OnPreferencesChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            AudioPreferences.Changed -= OnPreferencesChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnPreferencesChanged()
        {
            ApplyGain();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayScene(scene.name);
        }

        void PlayActiveScene()
        {
            PlayScene(SceneManager.GetActiveScene().name);
        }

        void PlayScene(string sceneName)
        {
            if (_library == null)
                return;

            MapBiome biome = MapBiome.Default;
            if (MapRunState.Active != null)
                biome = MapRunState.Active.PendingBiome;

            if (!_library.TryGetClip(sceneName, biome, out AudioClip clip))
                return;

            if (clip == _current && _source != null && _source.isPlaying)
                return;

            _current = clip;
            _token++;
            if (_fade != null)
                StopCoroutine(_fade);

            _fade = StartCoroutine(FadeTo(clip, _token));
        }

        IEnumerator FadeTo(AudioClip clip, int token)
        {
            bool playing = _source != null && _source.isPlaying && _source.clip != null;
            if (playing)
            {
                float from = _fadeLevel;
                float elapsed = 0f;
                while (elapsed < FadeSeconds)
                {
                    if (token != _token)
                        yield break;

                    elapsed += Time.unscaledDeltaTime;
                    _fadeLevel = Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / FadeSeconds));
                    ApplyGain();
                    yield return null;
                }
            }
            else
            {
                _fadeLevel = 0f;
            }

            if (token != _token || _source == null)
                yield break;

            _source.clip = clip;
            _source.loop = true;
            _source.Play();

            float fadeIn = 0f;
            while (fadeIn < FadeSeconds)
            {
                if (token != _token)
                    yield break;

                fadeIn += Time.unscaledDeltaTime;
                _fadeLevel = Mathf.Clamp01(fadeIn / FadeSeconds);
                ApplyGain();
                yield return null;
            }

            if (token != _token)
                yield break;

            _fadeLevel = 1f;
            ApplyGain();
            _fade = null;
        }

        void Route()
        {
            if (_source == null)
                return;

            AudioMixerGroup group = GameAudio.MusicGroup;
            if (group != null)
                _source.outputAudioMixerGroup = group;

            ApplyGain();
        }

        void ApplyGain()
        {
            if (_source == null)
                return;

            _source.volume = _fadeLevel * AudioPreferences.Music;
        }
    }
}
