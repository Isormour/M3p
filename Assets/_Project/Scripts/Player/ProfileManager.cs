using System;
using System.IO;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Reads and writes the player profile as JSON and hands out the one instance the game plays with.
    /// Everything that needs persistent character data goes through <see cref="CurrentProfile"/>.
    /// </summary>
    public sealed class ProfileManager
    {
        const string SaveFileName = "player_profile.json";
        const string PlayerNamePrefsKey = "M3P.PlayerName";

        readonly GameConfig _config;

        PlayerProfile _currentProfile;

        public ProfileManager(GameConfig config)
        {
            _config = config;
        }

        /// <summary>Raised when the profile is loaded, replaced or written to disk.</summary>
        public event Action ProfileChanged;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave => File.Exists(SavePath);

        /// <summary>Last name typed in the main menu, kept across restarts independently of the profile file.</summary>
        public static string RememberedPlayerName =>
            PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty);

        /// <summary>False when the menu has never stored a name. An empty string is a name the player cleared.</summary>
        public static bool TryGetRememberedPlayerName(out string name)
        {
            if (!PlayerPrefs.HasKey(PlayerNamePrefsKey))
            {
                name = string.Empty;
                return false;
            }

            name = PlayerProfile.SanitizeName(PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty));
            return true;
        }

        public bool IsLoaded => _currentProfile != null;

        /// <summary>The profile the game plays with, read from disk on first access.</summary>
        public PlayerProfile CurrentProfile
        {
            get
            {
                if (_currentProfile == null)
                    Load();

                return _currentProfile;
            }
        }

        /// <summary>Reads the save, falling back to the authored starting build on a first run.</summary>
        public PlayerProfile Load()
        {
            bool hadSave = HasSave;
            PlayerProfile profile = hadSave
                ? PlayerProfile.FromJson(File.ReadAllText(SavePath))
                : CreateStartingProfile();

            bool missingProfileId = string.IsNullOrEmpty(profile.ProfileId);
            profile.NormalizeAfterLoad();
            bool filledName = string.IsNullOrEmpty(profile.PlayerName) && PlayerPrefs.HasKey(PlayerNamePrefsKey);
            if (filledName)
                profile.PlayerName = PlayerProfile.SanitizeName(RememberedPlayerName);
            int cardsBeforeSeed = profile.Cards.Count;
            int tilesBeforeSeed = profile.Tiles.Count;
            int treesBeforeSeed = profile.PerkTreePoints != null ? profile.PerkTreePoints.Count : 0;
            _config?.PlayerStart?.EnsureStarterCards(profile, _config.Cards);
            _config?.PlayerStart?.EnsureStarterTiles(profile, _config.Tiles);
            bool seededUnlocks = _config?.PlayerStart != null &&
                _config.PlayerStart.EnsureDefaultPerkTrees(profile, _config.StatProgression);
            SetCurrentProfile(profile);

            bool seededCards = profile.Cards.Count > cardsBeforeSeed;
            bool seededTiles = profile.Tiles.Count > tilesBeforeSeed;
            bool seededTrees = seededUnlocks ||
                (profile.PerkTreePoints != null ? profile.PerkTreePoints.Count : 0) > treesBeforeSeed;
            if (!hadSave || missingProfileId || seededCards || seededTiles || seededTrees || filledName)
                Save();

            if (!hadSave)
                GameAnalytics.RecordProfileCreated(profile.PlayerName);

            return profile;
        }

        public void Save()
        {
            if (_currentProfile == null)
                return;

            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, _currentProfile.ToJson());
            ProfileChanged?.Invoke();
        }

        /// <summary>Throws the save away and starts over from the authored starting build.</summary>
        public void ResetToStartingProfile()
        {
            if (HasSave)
                File.Delete(SavePath);

            _currentProfile = null;
            Load();
        }

        PlayerProfile CreateStartingProfile()
        {
            PlayerStartConfig start = _config != null ? _config.PlayerStart : null;
            if (start == null)
            {
                Debug.LogError(
                    $"{nameof(ProfileManager)}: assign {nameof(GameConfig.PlayerStart)} on {nameof(GameConfig)} or new characters begin with no skills, cards or tiles.");
                return new PlayerProfile();
            }

            PlayerProfile profile = start.CreateProfile(
                _config.Skills, _config.Cards, _config.Tiles, _config.StatProgression);
            if (PlayerPrefs.HasKey(PlayerNamePrefsKey))
                profile.PlayerName = PlayerProfile.SanitizeName(RememberedPlayerName);

            return profile;
        }

        /// <summary>
        /// Stores the menu name on the live profile and in PlayerPrefs.
        /// PlayerPrefs is what the name field reads on the next launch, including after a profile reset.
        /// </summary>
        public void SetPlayerName(string name)
        {
            name = PlayerProfile.SanitizeName(name);
            RememberPlayerName(name);

            PlayerProfile profile = CurrentProfile;
            if (profile == null || profile.PlayerName == name)
                return;

            profile.PlayerName = name;
            Save();
        }

        /// <summary>Writes the name field's last value. Empty is stored only once a name was saved before.</summary>
        public static void RememberPlayerName(string name)
        {
            name = PlayerProfile.SanitizeName(name);
            bool hasKey = PlayerPrefs.HasKey(PlayerNamePrefsKey);
            if (!hasKey && name.Length == 0)
                return;

            if (hasKey && PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty) == name)
                return;

            PlayerPrefs.SetString(PlayerNamePrefsKey, name);
            PlayerPrefs.Save();
        }

        void SetCurrentProfile(PlayerProfile profile)
        {
            _currentProfile = profile;
            ProfileChanged?.Invoke();
        }
    }
}
