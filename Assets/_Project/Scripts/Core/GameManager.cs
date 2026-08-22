using UnityEngine;
using UnityEngine.SceneManagement;

namespace M3P
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum MapLaunchMode
        {
            None,
            NewGenerated,
            Debug,
            Continue,
        }

        [SerializeField] GameConfig _config;

        [Header("Flow")]
        [Tooltip("Loads the Menu scene after bootstrap so play can start from Boot.")]
        [SerializeField] bool _loadMenuOnStart = true;

        ProfileManager _profileManager;
        ProgressionService _progression;
        MapRunState _mapRun;

        public GameConfig Config => _config;

        /// <summary>Owns the saved player profile for the whole session.</summary>
        public ProfileManager ProfileManager => _profileManager ??= new ProfileManager(_config);

        /// <summary>Applies battle results to the profile owned by <see cref="ProfileManager"/>.</summary>
        public ProgressionService Progression => _progression ??= new ProgressionService(_config, ProfileManager);

        /// <summary>Current dungeon-map run (node position, cleared rooms, pending battle).</summary>
        public MapRunState MapRun
        {
            get
            {
                if (_mapRun == null)
                {
                    _mapRun = new MapRunState();
                    MapRunState.Active = _mapRun;
                }

                return _mapRun;
            }
        }

        /// <summary>How the next Map scene should build its floor. Consumed by <see cref="MapManager"/>.</summary>
        public MapLaunchMode LaunchMode { get; private set; }

        public bool HasContinuableRun
        {
            get
            {
                if (!M3P.ProfileManager.HasSave)
                    return false;

                MapRunSave save = ProfileManager.CurrentProfile != null
                    ? ProfileManager.CurrentProfile.MapRun
                    : null;
                return save != null && save.CanContinue;
            }
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
            MapRunState.Active = MapRun;
        }

        void Start()
        {
            if (Instance != this)
                return;

            if (_loadMenuOnStart && SceneManager.GetActiveScene().name == SceneFlow.BootScene)
                SceneFlow.LoadMenu();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void StartNewGeneratedMap()
        {
            MapRun.Clear();
            PersistMapRun();
            LaunchMode = MapLaunchMode.NewGenerated;
            SceneFlow.LoadMap();
        }

        public void StartDebugMap()
        {
            MapRun.Clear();
            PersistMapRun();
            LaunchMode = MapLaunchMode.Debug;
            SceneFlow.LoadMap();
        }

        public bool TryContinueMap()
        {
            if (!HasContinuableRun)
                return false;

            MapRun.Restore(ProfileManager.CurrentProfile.MapRun);
            LaunchMode = MapLaunchMode.Continue;
            SceneFlow.LoadMap();
            return true;
        }

        public void PersistMapRun()
        {
            PlayerProfile profile = ProfileManager.CurrentProfile;
            if (profile == null)
                return;

            profile.MapRun = MapRun.ToSave();
            ProfileManager.Save();
        }

        [ContextMenu("Reset Profile Save")]
        public void ResetProfileSave()
        {
            MapRun.Clear();
            LaunchMode = MapLaunchMode.None;
            ProfileManager.ResetToStartingProfile();
            Debug.Log($"{nameof(GameManager)}: profile save cleared ({M3P.ProfileManager.SavePath}).", this);
        }
    }
}
