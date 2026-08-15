#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Match3;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    /// <summary>
    /// Inspects and edits the player profile save, and the live profile while playing.
    /// </summary>
    public sealed class ProfileDebugWindow : EditorWindow
    {
        const string MenuPath = "M3P/Profile Debug";
        const string ConfigPrefKey = "M3P.ProfileDebugWindow.GameConfig";

        [SerializeField] GameConfig _config;
        [SerializeField] PlayerProfile _working;
        [SerializeField] bool _dirty;
        [SerializeField] bool _showProgression = true;
        [SerializeField] bool _showHardStats = true;
        [SerializeField] bool _showSkills = true;
        [SerializeField] bool _showCards = true;
        [SerializeField] bool _showShards = true;
        [SerializeField] bool _showTalents = true;

        Vector2 _scroll;
        ProfileManager _subscribedProfiles;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            ProfileDebugWindow window = GetWindow<ProfileDebugWindow>("Profile Debug");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            ResolveConfig();
            if (_working == null)
                Reload(force: true);
            SubscribeProfileChanged();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            UnsubscribeProfileChanged();
        }

        void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        void OnGUI()
        {
            ResolveConfig();
            SubscribeProfileChanged();
            DrawToolbar();
            DrawStatus();

            if (_working == null)
            {
                EditorGUILayout.HelpBox("No profile loaded.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawProgression();
            DrawHardStats();
            DrawCombatPreview();
            DrawSkills();
            DrawCards();
            DrawShards();
            DrawTalents();
            EditorGUILayout.EndScrollView();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    Reload(force: false);

                using (new EditorGUI.DisabledScope(!_dirty))
                {
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                        SaveWorkingCopy();
                }

                if (GUILayout.Button("Reset to Start", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    ResetToStart();

                if (GUILayout.Button("Reveal Save", EditorStyles.toolbarButton, GUILayout.Width(84f)))
                    RevealSave();

                GUILayout.FlexibleSpace();
            }
        }

        void DrawStatus()
        {
            EditorGUILayout.Space(4f);
            GameConfig picked = (GameConfig)EditorGUILayout.ObjectField("Game Config", _config, typeof(GameConfig), false);
            if (picked != _config)
            {
                _config = picked;
                StoreConfigPref(_config);
            }

            EditorGUILayout.LabelField("Save", ProfileManager.SavePath);
            EditorGUILayout.LabelField("Source", DescribeSource());

            if (_dirty)
                EditorGUILayout.HelpBox("Unsaved edits. Save writes the JSON and, in Play Mode, the live profile.", MessageType.Warning);
        }

        void DrawProgression()
        {
            _showProgression = EditorGUILayout.Foldout(_showProgression, "Progression", true);
            if (!_showProgression)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawInt("Level", ref _working.Level, LevelProgressionConfig.FirstLevel);
                DrawInt("Experience", ref _working.Experience, 0);
                DrawInt("Unspent Stat Points", ref _working.UnspentStatPoints, 0);
            }
        }

        void DrawHardStats()
        {
            _showHardStats = EditorGUILayout.Foldout(_showHardStats, "Hard Stats", true);
            if (!_showHardStats)
                return;

            HardStats stats = _working.HardStats;
            using (new EditorGUI.IndentLevelScope())
            {
                stats.Strength = DrawStat("Strength", stats.Strength);
                stats.Intelligence = DrawStat("Intelligence", stats.Intelligence);
                stats.Constitution = DrawStat("Constitution", stats.Constitution);
                stats.Agility = DrawStat("Agility", stats.Agility);
            }

            if (!stats.Equals(_working.HardStats))
            {
                _working.HardStats = stats;
                MarkDirty();
            }
        }

        void DrawCombatPreview()
        {
            if (_working == null)
                return;

            CharacterStats battleStats = _working.CreateBattleStats(
                _config != null ? _config.StatProgression : null,
                _config != null ? _config.Talents : null);

            EditorGUILayout.LabelField("Combat Preview", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Max HP", battleStats.MaxHealth);
                EditorGUILayout.IntField("Max Action Points", battleStats.Soft != null ? battleStats.Soft.MaxActionPoints : 0);
                EditorGUILayout.IntField("Max Hand Size", battleStats.Soft != null ? battleStats.Soft.MaxHandSize : 0);
            }
        }

        void DrawSkills()
        {
            _working.Skills ??= new List<CharacterSkill>();
            _showSkills = EditorGUILayout.Foldout(_showSkills, $"Skills ({_working.Skills.Count})", true);
            if (!_showSkills)
                return;

            SkillConfig skills = _config != null ? _config.Skills : null;
            int removeAt = -1;

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < _working.Skills.Count; i++)
                {
                    CharacterSkill skill = _working.Skills[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        SkillDefinition current = skills != null ? skills.GetSkill(skill.SkillId) : null;
                        SkillDefinition picked = (SkillDefinition)EditorGUILayout.ObjectField(
                            current, typeof(SkillDefinition), false);

                        if (picked != current)
                            skill = AssignSkill(skill, picked, skills);

                        int level = EditorGUILayout.IntField(skill.SkillLevel, GUILayout.Width(48f));
                        if (level != skill.SkillLevel)
                        {
                            skill.SkillLevel = Mathf.Max(1, level);
                            MarkDirty();
                        }

                        if (GUILayout.Button("–", GUILayout.Width(22f)))
                            removeAt = i;
                    }

                    if (skills != null && skills.GetSkill(skill.SkillId) == null && skill.SkillId != SkillConfig.InvalidSkillId)
                        EditorGUILayout.HelpBox($"Skill id {skill.SkillId} is missing from {nameof(SkillConfig)}.", MessageType.Warning);

                    _working.Skills[i] = skill;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Skill"))
                        ShowAddSkillMenu(skills);

                    if (GUILayout.Button("Add Empty", GUILayout.Width(90f)))
                    {
                        _working.Skills.Add(new CharacterSkill(SkillConfig.InvalidSkillId, 1));
                        MarkDirty();
                    }
                }
            }

            if (removeAt >= 0)
            {
                _working.Skills.RemoveAt(removeAt);
                MarkDirty();
            }
        }

        void DrawCards()
        {
            _working.Cards ??= new List<OwnedCard>();
            _showCards = EditorGUILayout.Foldout(_showCards, $"Cards ({_working.Cards.Count})", true);
            if (!_showCards)
                return;

            CardConfig cards = _config != null ? _config.Cards : null;
            int removeAt = -1;

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < _working.Cards.Count; i++)
                {
                    OwnedCard owned = _working.Cards[i].Normalized();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        BoardActionCardDefinition current = cards != null ? cards.GetCard(owned.CardId) : null;
                        BoardActionCardDefinition picked = (BoardActionCardDefinition)EditorGUILayout.ObjectField(
                            current, typeof(BoardActionCardDefinition), false);

                        if (picked != current)
                            owned = AssignCard(owned, picked, cards);

                        if (owned.UpgradeIds != null && owned.UpgradeIds.Length > 0)
                            EditorGUILayout.LabelField($"up {owned.UpgradeIds.Length}", GUILayout.Width(40f));

                        if (GUILayout.Button("–", GUILayout.Width(22f)))
                            removeAt = i;
                    }

                    if (cards != null && cards.GetCard(owned.CardId) == null && owned.CardId != CardConfig.InvalidCardId)
                        EditorGUILayout.HelpBox($"Card id {owned.CardId} is missing from {nameof(CardConfig)}.", MessageType.Warning);

                    _working.Cards[i] = owned;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Card"))
                        ShowAddCardMenu(cards);

                    if (GUILayout.Button("Add Empty", GUILayout.Width(90f)))
                    {
                        _working.Cards.Add(new OwnedCard(CardConfig.InvalidCardId));
                        MarkDirty();
                    }
                }
            }

            if (removeAt >= 0)
            {
                _working.Cards.RemoveAt(removeAt);
                MarkDirty();
            }
        }

        void DrawShards()
        {
            _working.Shards ??= new List<ShardAmount>();
            _showShards = EditorGUILayout.Foldout(_showShards, $"Shards ({_working.Shards.Count})", true);
            if (!_showShards)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                HashSet<string> drawn = new HashSet<string>();
                Match3TileTypeDefinition[] tileTypes = _config != null ? _config.TileTypes : null;
                if (tileTypes != null)
                {
                    for (int i = 0; i < tileTypes.Length; i++)
                    {
                        Match3TileTypeDefinition tileType = tileTypes[i];
                        if (tileType == null)
                            continue;

                        drawn.Add(tileType.name);
                        DrawShardAmount(tileType.name, tileType);
                    }
                }

                for (int i = 0; i < _working.Shards.Count; i++)
                {
                    string key = _working.Shards[i].TileType;
                    if (string.IsNullOrEmpty(key) || drawn.Contains(key))
                        continue;

                    DrawShardAmount(key, null);
                }
            }
        }

        void DrawTalents()
        {
            _working.UnlockedTalentIds ??= new List<int>();
            _showTalents = EditorGUILayout.Foldout(
                _showTalents,
                $"Talents ({_working.UnlockedTalentIds.Count})",
                true);
            if (!_showTalents)
                return;

            TalentConfig talents = _config != null ? _config.Talents : null;
            int removeAt = -1;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Pending Choice", EditorStyles.miniBoldLabel);
                PendingTalentChoice pending = _working.PendingTalent;
                bool hasPending = pending.IsValid;
                bool nextHasPending = EditorGUILayout.Toggle("Has Pending", hasPending);
                if (nextHasPending != hasPending)
                {
                    pending = nextHasPending
                        ? new PendingTalentChoice(EStatType.Strength, 1)
                        : default;
                    MarkDirty();
                }

                if (pending.IsValid)
                {
                    EStatType stat = (EStatType)EditorGUILayout.EnumPopup("Stat", pending.Stat);
                    int tier = EditorGUILayout.IntField("Milestone Tier", pending.MilestoneTier);
                    tier = Mathf.Max(1, tier);
                    if (stat != pending.Stat || tier != pending.MilestoneTier)
                    {
                        pending = new PendingTalentChoice(stat, tier);
                        MarkDirty();
                    }
                }

                _working.PendingTalent = pending;

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Unlocked", EditorStyles.miniBoldLabel);

                for (int i = 0; i < _working.UnlockedTalentIds.Count; i++)
                {
                    int talentId = _working.UnlockedTalentIds[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        TalentDefinition current = talents != null ? talents.GetTalent(talentId) : null;
                        TalentDefinition picked = (TalentDefinition)EditorGUILayout.ObjectField(
                            current, typeof(TalentDefinition), false);

                        if (picked != current)
                        {
                            int nextId = AssignTalentId(picked, talents);
                            if (nextId != talentId)
                            {
                                _working.UnlockedTalentIds[i] = nextId;
                                MarkDirty();
                            }
                        }

                        if (GUILayout.Button("–", GUILayout.Width(22f)))
                            removeAt = i;
                    }

                    if (talents != null && talents.GetTalent(talentId) == null && talentId != TalentConfig.InvalidTalentId)
                        EditorGUILayout.HelpBox($"Talent id {talentId} is missing from {nameof(TalentConfig)}.", MessageType.Warning);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Talent"))
                        ShowAddTalentMenu(talents);

                    if (GUILayout.Button("Add Empty", GUILayout.Width(90f)))
                    {
                        _working.UnlockedTalentIds.Add(TalentConfig.InvalidTalentId);
                        MarkDirty();
                    }
                }
            }

            if (removeAt >= 0)
            {
                _working.UnlockedTalentIds.RemoveAt(removeAt);
                MarkDirty();
            }
        }

        void DrawShardAmount(string tileTypeKey, Match3TileTypeDefinition tileType)
        {
            int amount = _working.GetShards(tileTypeKey);
            string label = tileType != null ? tileType.name : $"{tileTypeKey} (unknown)";
            int next = EditorGUILayout.IntField(label, amount);
            next = Mathf.Max(0, next);
            if (next == amount)
                return;

            SetShardAmount(tileTypeKey, next);
            MarkDirty();
        }

        void SetShardAmount(string tileTypeKey, int amount)
        {
            _working.Shards ??= new List<ShardAmount>();

            for (int i = 0; i < _working.Shards.Count; i++)
            {
                if (!string.Equals(_working.Shards[i].TileType, tileTypeKey, System.StringComparison.Ordinal))
                    continue;

                if (amount <= 0)
                    _working.Shards.RemoveAt(i);
                else
                    _working.Shards[i] = new ShardAmount(tileTypeKey, amount);
                return;
            }

            if (amount > 0)
                _working.Shards.Add(new ShardAmount(tileTypeKey, amount));
        }

        CharacterSkill AssignSkill(CharacterSkill skill, SkillDefinition picked, SkillConfig skills)
        {
            if (picked == null)
            {
                MarkDirty();
                return new CharacterSkill(SkillConfig.InvalidSkillId, Mathf.Max(1, skill.SkillLevel));
            }

            int id = skills != null ? skills.GetSkillId(picked) : SkillConfig.InvalidSkillId;
            if (id == SkillConfig.InvalidSkillId)
            {
                EditorUtility.DisplayDialog(
                    "Unknown Skill",
                    $"'{picked.name}' is not registered in {nameof(SkillConfig)}, so it cannot be saved to a profile.",
                    "OK");
                return skill;
            }

            MarkDirty();
            return new CharacterSkill(id, Mathf.Max(1, skill.SkillLevel), picked.name);
        }

        OwnedCard AssignCard(OwnedCard owned, BoardActionCardDefinition picked, CardConfig cards)
        {
            if (picked == null)
            {
                MarkDirty();
                return new OwnedCard(CardConfig.InvalidCardId, owned.UpgradeIds);
            }

            int id = cards != null ? cards.GetCardId(picked) : CardConfig.InvalidCardId;
            if (id == CardConfig.InvalidCardId)
            {
                EditorUtility.DisplayDialog(
                    "Unknown Card",
                    $"'{picked.name}' is not registered in {nameof(CardConfig)}, so it cannot be saved to a profile.",
                    "OK");
                return owned;
            }

            MarkDirty();
            return new OwnedCard(id, owned.UpgradeIds);
        }

        int AssignTalentId(TalentDefinition picked, TalentConfig talents)
        {
            if (picked == null)
                return TalentConfig.InvalidTalentId;

            int id = talents != null ? talents.GetTalentId(picked) : TalentConfig.InvalidTalentId;
            if (id == TalentConfig.InvalidTalentId)
            {
                EditorUtility.DisplayDialog(
                    "Unknown Talent",
                    $"'{picked.name}' is not registered in {nameof(TalentConfig)}, so it cannot be saved to a profile.",
                    "OK");
                return TalentConfig.InvalidTalentId;
            }

            return id;
        }

        void ShowAddSkillMenu(SkillConfig skills)
        {
            if (skills == null || skills.Entries.Length == 0)
            {
                _working.Skills.Add(new CharacterSkill(SkillConfig.InvalidSkillId, 1));
                MarkDirty();
                return;
            }

            GenericMenu menu = new GenericMenu();
            SkillConfig.Entry[] entries = skills.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                SkillDefinition skill = entries[i].Skill;
                if (skill == null || entries[i].Id == SkillConfig.InvalidSkillId)
                    continue;

                int skillId = entries[i].Id;
                string skillName = skill.name;
                menu.AddItem(new GUIContent(skillName), false, () =>
                {
                    _working.Skills.Add(new CharacterSkill(skillId, 1, skillName));
                    MarkDirty();
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        void ShowAddCardMenu(CardConfig cards)
        {
            if (cards == null || cards.Entries.Length == 0)
            {
                _working.Cards.Add(new OwnedCard(CardConfig.InvalidCardId));
                MarkDirty();
                return;
            }

            GenericMenu menu = new GenericMenu();
            CardConfig.Entry[] entries = cards.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                BoardActionCardDefinition card = entries[i].Card;
                if (card == null || entries[i].Id == CardConfig.InvalidCardId)
                    continue;

                int cardId = entries[i].Id;
                string cardName = card.DisplayName;
                menu.AddItem(new GUIContent(cardName), false, () =>
                {
                    _working.Cards.Add(new OwnedCard(cardId));
                    MarkDirty();
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        void ShowAddTalentMenu(TalentConfig talents)
        {
            if (talents == null || talents.Entries.Length == 0)
            {
                _working.UnlockedTalentIds.Add(TalentConfig.InvalidTalentId);
                MarkDirty();
                return;
            }

            GenericMenu menu = new GenericMenu();
            TalentConfig.Entry[] entries = talents.Entries;
            bool any = false;
            for (int i = 0; i < entries.Length; i++)
            {
                TalentDefinition talent = entries[i].Talent;
                if (talent == null || entries[i].Id == TalentConfig.InvalidTalentId)
                    continue;

                int talentId = entries[i].Id;
                if (_working.UnlockedTalentIds.Contains(talentId))
                    continue;

                any = true;
                string label = $"{talent.DisplayName} ({talent.Stat} T{talent.MilestoneTier})";
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    _working.UnlockedTalentIds.Add(talentId);
                    MarkDirty();
                    Repaint();
                });
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent("All registered talents are already unlocked"));

            menu.ShowAsContext();
        }

        void DrawInt(string label, ref int value, int min)
        {
            int next = EditorGUILayout.IntField(label, value);
            next = Mathf.Max(min, next);
            if (next == value)
                return;

            value = next;
            MarkDirty();
        }

        int DrawStat(string label, int value)
        {
            int next = EditorGUILayout.IntField(label, value);
            next = Mathf.Max(0, next);
            if (next != value)
                MarkDirty();
            return next;
        }

        void Reload(bool force)
        {
            if (!force && _dirty && !EditorUtility.DisplayDialog(
                    "Reload Profile",
                    "Discard unsaved profile edits?",
                    "Reload",
                    "Cancel"))
            {
                return;
            }

            _working = ReadProfile();
            _working.NormalizeAfterLoad();
            _dirty = false;
            UpdateTitle();
            Repaint();
        }

        void SaveWorkingCopy()
        {
            if (_working == null)
                return;

            _working.NormalizeAfterLoad();

            ProfileManager live = LiveProfiles();
            if (live != null)
            {
                live.CurrentProfile.CopyFrom(_working);
                live.Save();
            }
            else
            {
                WriteProfileToDisk(_working);
            }

            _dirty = false;
            UpdateTitle();
        }

        void ResetToStart()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Profile Save",
                    "Delete the saved player profile and recreate it from PlayerStartConfig?",
                    "Reset",
                    "Cancel"))
            {
                return;
            }

            ProfileManager live = LiveProfiles();
            if (live != null)
            {
                live.ResetToStartingProfile();
                live.Save();
                _working = CloneProfile(live.CurrentProfile);
            }
            else
            {
                if (ProfileManager.HasSave)
                    File.Delete(ProfileManager.SavePath);

                _working = CreateStartingProfile();
                _working.NormalizeAfterLoad();
                WriteProfileToDisk(_working);
            }

            _dirty = false;
            UpdateTitle();
        }

        void RevealSave()
        {
            string path = ProfileManager.HasSave
                ? ProfileManager.SavePath
                : Path.GetDirectoryName(ProfileManager.SavePath);
            if (!string.IsNullOrEmpty(path))
                EditorUtility.RevealInFinder(path);
        }

        PlayerProfile ReadProfile()
        {
            ProfileManager live = LiveProfiles();
            if (live != null)
                return CloneProfile(live.CurrentProfile);

            if (ProfileManager.HasSave)
            {
                PlayerProfile loaded = PlayerProfile.FromJson(File.ReadAllText(ProfileManager.SavePath));
                _config?.PlayerStart?.EnsureStarterCards(loaded, _config.Cards);
                return loaded;
            }

            return CreateStartingProfile();
        }

        PlayerProfile CreateStartingProfile()
        {
            PlayerStartConfig start = _config != null ? _config.PlayerStart : null;
            if (start == null)
                return new PlayerProfile();

            return start.CreateProfile(_config.Skills, _config.Cards);
        }

        static PlayerProfile CloneProfile(PlayerProfile source)
        {
            PlayerProfile copy = new PlayerProfile();
            copy.CopyFrom(source);
            copy.NormalizeAfterLoad();
            return copy;
        }

        static void WriteProfileToDisk(PlayerProfile profile)
        {
            string path = ProfileManager.SavePath;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, profile.ToJson());
        }

        void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                UnsubscribeProfileChanged();
                SubscribeProfileChanged();
                Reload(force: !_dirty);
            }
        }

        void HandleProfileChanged()
        {
            if (_dirty)
                return;

            Reload(force: true);
        }

        void SubscribeProfileChanged()
        {
            ProfileManager live = LiveProfiles();
            if (_subscribedProfiles == live)
                return;

            UnsubscribeProfileChanged();
            if (live == null)
                return;

            live.ProfileChanged += HandleProfileChanged;
            _subscribedProfiles = live;
        }

        void UnsubscribeProfileChanged()
        {
            if (_subscribedProfiles != null)
                _subscribedProfiles.ProfileChanged -= HandleProfileChanged;

            _subscribedProfiles = null;
        }

        static ProfileManager LiveProfiles()
        {
            return Application.isPlaying && GameManager.Instance != null
                ? GameManager.Instance.ProfileManager
                : null;
        }

        void ResolveConfig()
        {
            if (_config != null)
                return;

            _config = LoadConfigPref();
            if (_config != null)
                return;

            if (GameManager.Instance != null && GameManager.Instance.Config != null)
            {
                _config = GameManager.Instance.Config;
                StoreConfigPref(_config);
                return;
            }

            GameManager sceneManager = FindFirstObjectByType<GameManager>();
            if (sceneManager != null && sceneManager.Config != null)
            {
                _config = sceneManager.Config;
                StoreConfigPref(_config);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length == 0)
                return;

            _config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            StoreConfigPref(_config);
        }

        static GameConfig LoadConfigPref()
        {
            string guid = EditorPrefs.GetString(ConfigPrefKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameConfig>(path);
        }

        static void StoreConfigPref(GameConfig config)
        {
            if (config == null)
            {
                EditorPrefs.DeleteKey(ConfigPrefKey);
                return;
            }

            string path = AssetDatabase.GetAssetPath(config);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
                EditorPrefs.SetString(ConfigPrefKey, guid);
        }

        string DescribeSource()
        {
            if (LiveProfiles() != null)
                return _dirty ? "Live profile (edited)" : "Live profile";

            if (ProfileManager.HasSave)
                return _dirty ? "Save file (edited)" : "Save file";

            return _dirty ? "Starting profile (edited)" : "Starting profile (no save yet)";
        }

        void MarkDirty()
        {
            _dirty = true;
            UpdateTitle();
        }

        void UpdateTitle()
        {
            titleContent = new GUIContent(_dirty ? "Profile Debug *" : "Profile Debug");
        }
    }
}
#endif
