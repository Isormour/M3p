using UnityEngine;

namespace M3P
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameConfig _config;

        ProfileManager _profileManager;
        ProgressionService _progression;

        public GameConfig Config => _config;

        /// <summary>Owns the saved player profile for the whole session.</summary>
        public ProfileManager ProfileManager => _profileManager ??= new ProfileManager(_config);

        /// <summary>Applies battle results to the profile owned by <see cref="ProfileManager"/>.</summary>
        public ProgressionService Progression => _progression ??= new ProgressionService(_config, ProfileManager);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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
