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

            profile.NormalizeAfterLoad();
            int cardsBeforeSeed = profile.Cards.Count;
            _config?.PlayerStart?.EnsureStarterCards(profile, _config.Cards);
            SetCurrentProfile(profile);

            if (hadSave && profile.Cards.Count > cardsBeforeSeed)
                Save();

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
                    $"{nameof(ProfileManager)}: assign {nameof(GameConfig.PlayerStart)} on {nameof(GameConfig)} or new characters begin with no skills or cards.");
                return new PlayerProfile();
            }

            return start.CreateProfile(_config.Skills, _config.Cards);
        }

        void SetCurrentProfile(PlayerProfile profile)
        {
            _currentProfile = profile;
            ProfileChanged?.Invoke();
        }
    }
}
