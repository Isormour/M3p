using UnityEngine;

namespace M3P
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameConfig _config;

        [Header("Flow")]
        [Tooltip("Loads the Map scene after bootstrap so play can start from Main.")]
        [SerializeField] bool _loadMapOnStart = true;

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

            if (_loadMapOnStart &&
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneFlow.MainScene)
            {
                SceneFlow.LoadMap();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        [ContextMenu("Reset Profile Save")]
        void ResetProfileSave()
        {
            ProfileManager.ResetToStartingProfile();
            Debug.Log($"{nameof(GameManager)}: profile save cleared ({M3P.ProfileManager.SavePath}).", this);
        }
    }
}
