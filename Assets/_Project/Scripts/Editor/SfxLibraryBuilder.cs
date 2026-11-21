using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    public static class SfxLibraryBuilder
    {
        const string LibraryPath = "Assets/_Project/Audio/SfxLibrary.asset";
        const string SfxRoot = "Assets/_Project/Audio/SFX";
        const string AcceptedRoot = "Assets/_Project/Audio/AcceptedSFX";
        const string AcceptedManifestPath = "Assets/_Project/Audio/AcceptedSfxManifest.json";
        const string SkillConfigRoot = "Assets/_Project/Configs/Skills";

        [MenuItem("M3P/Build SFX Library")]
        public static void Build()
        {
            // Validate every approved file and mapping before touching the library.
            AcceptedBindings accepted = LoadAcceptedBindings();
            SfxLibrary library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SfxLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var existingClips = accepted != null
                ? ReadExistingClips(library)
                : new Dictionary<SfxId, SfxLibrary.ClipEntry>();
            var clips = new List<SfxLibrary.ClipEntry>();
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None)
                    continue;

                string relative = RelativePath(id);
                if (relative == null)
                    continue;

                SfxLibrary.ClipEntry entry;
                bool hasExisting = existingClips.TryGetValue(id, out entry);
                if (!hasExisting)
                {
                    entry = new SfxLibrary.ClipEntry
                    {
                        Id = id,
                        Clip = LoadClip(relative),
                        Alternates = id == SfxId.MapNodeSelect
                            ? new[] { LoadClip("MenuMap/encounter_select_2.mp3") }
                            : Array.Empty<AudioClip>(),
                    };
                }
                else if (entry.Clip == null)
                    entry.Clip = LoadClip(relative);

                AudioClip approved;
                if (accepted != null && accepted.General.TryGetValue(id, out approved))
                    entry.Clip = approved;
                if (entry.Clip == null)
                {
                    Debug.LogWarning($"SFX library: missing clip for {id} at {SfxRoot}/{relative}");
                    continue;
                }
                entry.Alternates = entry.Alternates ?? Array.Empty<AudioClip>();
                clips.Add(entry);
            }

            var skills = new List<SfxLibrary.SkillClip>();
            AddSkillFolder(skills, "Skills/Basic", "basic_layer.mp3");
            AddSkillFolder(skills, "Skills/Enemy", "enemy_layer.mp3");
            AddSkillFolder(skills, "Skills/Mage", "mage_layer.mp3");
            AddSkillFolder(skills, "Skills/Warrior", "warrior_layer.mp3");
            AddSkillFolder(skills, "Skills/Shadow", "shadow_layer.mp3");
            if (accepted != null)
                skills = MergeAcceptedSkills(library, skills, accepted);

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty clipProp = serialized.FindProperty("_clips");
            clipProp.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                SerializedProperty element = clipProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Id").enumValueIndex = (int)clips[i].Id;
                element.FindPropertyRelative("Clip").objectReferenceValue = clips[i].Clip;
                SerializedProperty alternates = element.FindPropertyRelative("Alternates");
                alternates.arraySize = clips[i].Alternates.Length;
                for (int j = 0; j < alternates.arraySize; j++)
                    alternates.GetArrayElementAtIndex(j).objectReferenceValue = clips[i].Alternates[j];
            }

            SerializedProperty skillProp = serialized.FindProperty("_skills");
            skillProp.arraySize = skills.Count;
            for (int i = 0; i < skills.Count; i++)
            {
                SerializedProperty element = skillProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("SkillAssetName").stringValue = skills[i].SkillAssetName;
                element.FindPropertyRelative("Layer").objectReferenceValue = skills[i].Layer;
                element.FindPropertyRelative("Accent").objectReferenceValue = skills[i].Accent;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);

            string[] configs = AssetDatabase.FindAssets("t:GameConfig");
            for (int i = 0; i < configs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(configs[i]);
                GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                if (config == null)
                    continue;

                SerializedObject configObject = new SerializedObject(config);
                configObject.FindProperty("_sfx").objectReferenceValue = library;
                configObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"SFX library built with {clips.Count} clips and {skills.Count} skills.");
        }

        [Serializable]
        sealed class AcceptedManifest
        {
            public int schemaVersion;
            public AcceptedEntry[] entries;
        }

        [Serializable]
        sealed class AcceptedEntry
        {
            public string id;
            public string assetPath;
            public string directory;
            public string sha256;
        }

        sealed class SkillTarget
        {
            public string Name;
            public string Folder;
        }

        sealed class AcceptedBindings
        {
            public readonly Dictionary<SfxId, AudioClip> General = new Dictionary<SfxId, AudioClip>();
            public readonly Dictionary<string, AudioClip> Accents = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            public readonly Dictionary<string, AudioClip> Layers = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            public readonly List<SkillTarget> Targets = new List<SkillTarget>();
        }

        static string SkillAlias(string folder, string name)
        {
            return folder + "/" + name.Replace("_", string.Empty).ToLowerInvariant();
        }

        static string SkillFolder(string path)
        {
            if (path.StartsWith(SkillConfigRoot + "/Enemies/", StringComparison.Ordinal)) return "Skills/Enemy";
            foreach (string group in new[] { "Mage", "Warrior", "Shadow" })
                if (path.StartsWith(SkillConfigRoot + "/Player/" + group + "/", StringComparison.Ordinal))
                    return "Skills/" + group;
            string directory = Path.GetDirectoryName(path).Replace('\\', '/');
            if (directory == SkillConfigRoot || directory == SkillConfigRoot + "/Player") return "Skills/Basic";
            return null;
        }

        static Dictionary<string, SkillTarget> FindSkillTargets(List<SkillTarget> targets)
        {
            var aliases = new Dictionary<string, SkillTarget>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:SkillDefinition", new[] { SkillConfigRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
                string folder = SkillFolder(path);
                if (skill == null || folder == null) continue;
                var target = new SkillTarget { Name = skill.name, Folder = folder };
                targets.Add(target);
                foreach (string name in new[] { skill.name, Path.GetFileNameWithoutExtension(path) })
                {
                    string key = SkillAlias(folder, name);
                    SkillTarget other;
                    if (aliases.TryGetValue(key, out other) && other.Name != target.Name)
                        throw new InvalidOperationException("Ambiguous skill audio key: " + key);
                    aliases[key] = target;
                }
            }
            return aliases;
        }

        static string AbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        static AcceptedBindings LoadAcceptedBindings()
        {
            string manifestPath = AbsoluteAssetPath(AcceptedManifestPath);
            if (!File.Exists(manifestPath)) return null;
            AcceptedManifest manifest = JsonUtility.FromJson<AcceptedManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.schemaVersion != 1 || manifest.entries == null || manifest.entries.Length == 0)
                throw new InvalidOperationException("Invalid accepted SFX manifest: " + AcceptedManifestPath);
            var result = new AcceptedBindings();
            var aliases = FindSkillTargets(result.Targets);
            var events = new Dictionary<string, SfxId>(StringComparer.Ordinal);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                string path = RelativePath(id);
                if (path != null) events.Add(Path.GetFileNameWithoutExtension(path), id);
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (AcceptedEntry item in manifest.entries)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id)
                    || string.IsNullOrEmpty(item.directory) || string.IsNullOrEmpty(item.assetPath)
                    || string.IsNullOrEmpty(item.sha256) || item.sha256.Length != 64)
                    throw new InvalidOperationException("Invalid or duplicate accepted SFX entry");
                string expectedPrefix = AcceptedRoot + "/" + item.directory + "/";
                string extension = Path.GetExtension(item.assetPath).ToLowerInvariant();
                if (item.assetPath.Contains("\\") || item.assetPath.Contains("/../") || item.assetPath.Contains("/./")
                    || !item.assetPath.StartsWith(expectedPrefix, StringComparison.Ordinal)
                    || Path.GetFileNameWithoutExtension(item.assetPath) != item.id
                    || (extension != ".mp3" && extension != ".wav")
                    || item.assetPath != expectedPrefix + item.id + extension)
                    throw new InvalidOperationException("Invalid accepted SFX path: " + item.assetPath);
                using (var hash = SHA256.Create())
                using (var stream = File.OpenRead(AbsoluteAssetPath(item.assetPath)))
                {
                    string digest = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
                    if (!digest.Equals(item.sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Accepted SFX hash mismatch: " + item.id);
                }
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(item.assetPath);
                if (clip == null) throw new InvalidOperationException("Accepted SFX is not imported: " + item.assetPath);
                if (item.directory.StartsWith("Skills/", StringComparison.Ordinal))
                {
                    if (item.id.EndsWith("_layer", StringComparison.Ordinal))
                    {
                        string group = Path.GetFileName(item.directory).ToLowerInvariant();
                        if (item.id != group + "_layer" || !result.Targets.Exists(t => t.Folder == item.directory))
                            throw new InvalidOperationException("Unknown accepted skill layer: " + item.id);
                        result.Layers.Add(item.directory, clip);
                    }
                    else
                    {
                        SkillTarget target;
                        if (!aliases.TryGetValue(SkillAlias(item.directory, item.id), out target))
                            throw new InvalidOperationException("No actual skill for accepted audio: " + item.id);
                        result.Accents.Add(target.Name, clip);
                    }
                }
                else
                {
                    SfxId id;
                    if (!events.TryGetValue(item.id, out id)
                        || Path.GetDirectoryName(RelativePath(id)).Replace('\\', '/') != item.directory)
                        throw new InvalidOperationException("Unknown accepted SFX event: " + item.id);
                    result.General.Add(id, clip);
                }
            }
            return result;
        }

        static Dictionary<SfxId, SfxLibrary.ClipEntry> ReadExistingClips(SfxLibrary library)
        {
            var result = new Dictionary<SfxId, SfxLibrary.ClipEntry>();
            SerializedProperty entries = new SerializedObject(library).FindProperty("_clips");
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty item = entries.GetArrayElementAtIndex(i);
                SfxId id = (SfxId)item.FindPropertyRelative("Id").intValue;
                SerializedProperty alternates = item.FindPropertyRelative("Alternates");
                var clips = new AudioClip[alternates.arraySize];
                for (int j = 0; j < clips.Length; j++) clips[j] = alternates.GetArrayElementAtIndex(j).objectReferenceValue as AudioClip;
                result[id] = new SfxLibrary.ClipEntry { Id = id,
                    Clip = item.FindPropertyRelative("Clip").objectReferenceValue as AudioClip, Alternates = clips };
            }
            return result;
        }

        static List<SfxLibrary.SkillClip> MergeAcceptedSkills(SfxLibrary library, List<SfxLibrary.SkillClip> legacy, AcceptedBindings accepted)
        {
            var entries = new Dictionary<string, SfxLibrary.SkillClip>(StringComparer.Ordinal);
            SerializedProperty existing = new SerializedObject(library).FindProperty("_skills");
            for (int i = 0; i < existing.arraySize; i++)
            {
                SerializedProperty item = existing.GetArrayElementAtIndex(i);
                string name = item.FindPropertyRelative("SkillAssetName").stringValue;
                if (string.IsNullOrEmpty(name)) continue;
                entries[name] = new SfxLibrary.SkillClip { SkillAssetName = name,
                    Layer = item.FindPropertyRelative("Layer").objectReferenceValue as AudioClip,
                    Accent = item.FindPropertyRelative("Accent").objectReferenceValue as AudioClip };
            }
            foreach (SfxLibrary.SkillClip old in legacy)
            {
                if (!accepted.Targets.Exists(t => t.Name == old.SkillAssetName)) continue;
                SfxLibrary.SkillClip item;
                if (!entries.TryGetValue(old.SkillAssetName, out item)) item = old;
                else
                {
                    if (item.Layer == null) item.Layer = old.Layer;
                    if (item.Accent == null) item.Accent = old.Accent;
                }
                entries[item.SkillAssetName] = item;
            }
            foreach (SkillTarget target in accepted.Targets)
            {
                AudioClip accent, layer;
                bool hasAccent = accepted.Accents.TryGetValue(target.Name, out accent);
                bool hasLayer = accepted.Layers.TryGetValue(target.Folder, out layer);
                if (!hasAccent && !hasLayer) continue;
                SfxLibrary.SkillClip item;
                if (!entries.TryGetValue(target.Name, out item))
                    item = new SfxLibrary.SkillClip { SkillAssetName = target.Name };
                if (hasAccent) item.Accent = accent;
                if (hasLayer) item.Layer = layer;
                else if (item.Layer == null)
                    item.Layer = LoadClip(target.Folder + "/" + Path.GetFileName(target.Folder).ToLowerInvariant() + "_layer.mp3");
                entries[target.Name] = item;
            }
            var result = new List<SfxLibrary.SkillClip>(entries.Values);
            result.Sort((a, b) => string.CompareOrdinal(a.SkillAssetName, b.SkillAssetName));
            return result;
        }

        static AudioClip LoadClip(string relative)
        {
            string rooted = $"{SfxRoot}/{relative}";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rooted);
            if (clip != null)
                return clip;

            string extension = Path.GetExtension(rooted);
            string alternate = extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
                ? Path.ChangeExtension(rooted, ".mp3")
                : Path.ChangeExtension(rooted, ".wav");
            if (alternate == rooted)
                return null;

            return AssetDatabase.LoadAssetAtPath<AudioClip>(alternate);
        }

        static void AddSkillFolder(List<SfxLibrary.SkillClip> skills, string folder, string layerFile)
        {
            AudioClip layer = LoadClip($"{folder}/{layerFile}");
            string absolute = Path.Combine(Application.dataPath, "_Project/Audio/SFX", folder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absolute))
                return;

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(absolute, "*.mp3"));
            files.AddRange(Directory.GetFiles(absolute, "*.wav"));
            for (int i = 0; i < files.Count; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName.EndsWith("_layer.mp3", StringComparison.OrdinalIgnoreCase)
                    || fileName.EndsWith("_layer.wav", StringComparison.OrdinalIgnoreCase))
                    continue;

                string assetName = SnakeToPascal(Path.GetFileNameWithoutExtension(fileName));
                AudioClip accent = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxRoot}/{folder}/{fileName}");
                skills.Add(new SfxLibrary.SkillClip
                {
                    SkillAssetName = assetName,
                    Layer = layer,
                    Accent = accent,
                });
            }
        }

        static string SnakeToPascal(string snake)
        {
            var builder = new StringBuilder(snake.Length);
            bool upper = true;
            for (int i = 0; i < snake.Length; i++)
            {
                char c = snake[i];
                if (c == '_')
                {
                    upper = true;
                    continue;
                }

                builder.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }

            return builder.ToString();
        }

        static string RelativePath(SfxId id)
        {
            switch (id)
            {
                case SfxId.MenuConfirm: return "MenuMap/menu_confirm.mp3";
                case SfxId.StartGame: return "MenuMap/start_game.mp3";
                case SfxId.SceneTransition: return "MenuMap/scene_transition.mp3";
                case SfxId.MapNodeSelect: return "MenuMap/encounter_select.mp3";
                case SfxId.RoutePreview: return "MenuMap/route_preview.mp3";
                case SfxId.RouteConfirm: return "MenuMap/route_confirm.mp3";
                case SfxId.RouteCancel: return "MenuMap/route_cancel.mp3";
                case SfxId.MapTokenStep: return "MenuMap/map_token_step.mp3";
                case SfxId.MapArrival: return "MenuMap/map_arrival.mp3";
                case SfxId.EncounterNormal: return "MenuMap/encounter_normal.mp3";
                case SfxId.EncounterElite: return "MenuMap/encounter_elite.mp3";
                case SfxId.EncounterBoss: return "MenuMap/encounter_boss.mp3";
                case SfxId.CardShopOpen: return "MenuMap/card_shop_open.mp3";
                case SfxId.ForgeOpen: return "MenuMap/forge_open.mp3";
                case SfxId.ChestOpen: return "MenuMap/chest_open.mp3";
                case SfxId.ChestReward: return "MenuMap/chest_reward.mp3";
                case SfxId.ChestEmpty: return "MenuMap/chest_empty.mp3";
                case SfxId.PanelOpen: return "MenuMap/panel_open.mp3";
                case SfxId.PanelClose: return "MenuMap/panel_close.mp3";
                case SfxId.BattleStart: return "Cards/battle_start.mp3";
                case SfxId.PlayerTurnStart: return "Cards/player_turn_start.mp3";
                case SfxId.HandDraw: return "Cards/hand_draw.mp3";
                case SfxId.SingleCardDraw: return "Cards/single_card_draw.mp3";
                case SfxId.CardSelect: return "Cards/card_select.mp3";
                case SfxId.CardDeselect: return "Cards/card_deselect.mp3";
                case SfxId.TargetTileSelect: return "Cards/target_tile_select.mp3";
                case SfxId.TargetSelectionComplete: return "Cards/target_selection_complete.mp3";
                case SfxId.CardChoiceOpen: return "Cards/card_choice_open.mp3";
                case SfxId.CardChoiceConfirm: return "Cards/card_choice_confirm.mp3";
                case SfxId.CardChoiceCancel: return "Cards/card_choice_cancel.mp3";
                case SfxId.CardQueueAdd: return "Cards/card_queue_add.mp3";
                case SfxId.CardQueueUndo: return "Cards/card_queue_undo.mp3";
                case SfxId.ResolveStart: return "Cards/resolve_start.mp3";
                case SfxId.CardsDiscard: return "Cards/cards_discard.mp3";
                case SfxId.PlayerTurnEnd: return "Cards/player_turn_end.mp3";
                case SfxId.EnemyTurnStart: return "Cards/enemy_turn_start.mp3";
                case SfxId.EnemyTurnEnd: return "Cards/enemy_turn_end.mp3";
                case SfxId.TileSwap: return "Board/tile_swap_timed.wav";
                case SfxId.TileRotate: return "Board/tile_rotate.mp3";
                case SfxId.RowShift: return "Board/row_shift.mp3";
                case SfxId.TileRecolor: return "Board/tile_recolor.mp3";
                case SfxId.TileCrackMark: return "Board/tile_crack_mark.mp3";
                case SfxId.TileBreak: return "Board/tile_break.mp3";
                case SfxId.TilePurge: return "Board/tile_purge.mp3";
                case SfxId.GravityShift: return "Board/gravity_shift.mp3";
                case SfxId.BoardShuffle: return "Board/board_shuffle.mp3";
                case SfxId.TileFall: return "Board/tile_fall.mp3";
                case SfxId.TileSpawn: return "Board/tile_spawn.mp3";
                case SfxId.MatchWaveComplete: return "Board/match_wave_complete.mp3";
                case SfxId.BigMatch: return "Board/big_match.mp3";
                case SfxId.CascadeWave: return "Board/cascade_wave.mp3";
                case SfxId.SuperMatch: return "Board/super_match.mp3";
                case SfxId.ManaGain: return "Board/mana_gain.mp3";
                case SfxId.ShardCollect: return "Board/shard_collect.mp3";
                case SfxId.TileBonusMana: return "Board/tile_bonus_mana.mp3";
                case SfxId.TileBonusHeal: return "Board/tile_bonus_heal.mp3";
                case SfxId.TileBonusAction: return "Board/tile_bonus_action.mp3";
                case SfxId.TileBonusShield: return "Board/tile_bonus_shield.mp3";
                case SfxId.TileBonusCard: return "Board/tile_bonus_card.mp3";
                case SfxId.TileBonusIgnite: return "Board/tile_bonus_ignite.mp3";
                case SfxId.TileBonusNeighborBreak: return "Board/tile_bonus_neighbor_break.mp3";
                case SfxId.EnemyIntent: return "Combat/enemy_intent.mp3";
                case SfxId.PlayerAttackWindup: return "Combat/player_attack_windup.mp3";
                case SfxId.PlayerProjectileLaunch: return "Combat/player_projectile_launch.mp3";
                case SfxId.EnemyProjectileHit: return "Combat/enemy_projectile_hit.mp3";
                case SfxId.PlayerHit: return "Combat/player_hit.mp3";
                case SfxId.ShieldHit: return "Combat/shield_hit.mp3";
                case SfxId.ShieldAppear: return "Combat/shield_appear.mp3";
                case SfxId.ShieldFade: return "Combat/shield_fade.mp3";
                case SfxId.EnemyDeath: return "Combat/enemy_death.mp3";
                case SfxId.PlayerDeath: return "Combat/player_death.mp3";
                case SfxId.PlayerSkillCast: return "Combat/player_skill_cast.mp3";
                case SfxId.EnemySkillCast: return "Combat/enemy_skill_cast.mp3";
                case SfxId.MultiHitTick: return "Combat/multi_hit_tick.mp3";
                case SfxId.Healing: return "Combat/healing.mp3";
                case SfxId.RegenerationTick: return "Combat/regeneration_tick.mp3";
                case SfxId.ShieldApply: return "Combat/shield_apply.mp3";
                case SfxId.StatusApply: return "Combat/status_apply.mp3";
                case SfxId.StatusStrengthen: return "Combat/status_strengthen.mp3";
                case SfxId.StatusExpire: return "Combat/status_expire.mp3";
                case SfxId.BurnTick: return "Combat/burn_tick_timed.wav";
                case SfxId.ManaTransmute: return "Combat/mana_transmute.mp3";
                case SfxId.HpSacrifice: return "Combat/hp_sacrifice.mp3";
                case SfxId.CardRecover: return "Combat/card_recover.mp3";
                case SfxId.SoulCollect: return "Combat/soul_collect.mp3";
                case SfxId.Victory: return "Rewards/victory.mp3";
                case SfxId.Defeat: return "Rewards/defeat.mp3";
                case SfxId.BattleRewardsReveal: return "Rewards/battle_rewards_reveal.mp3";
                case SfxId.ExperienceFill: return "Rewards/experience_fill.mp3";
                case SfxId.LevelUp: return "Rewards/level_up.mp3";
                case SfxId.BattleResultClose: return "Rewards/battle_result_close.mp3";
                case SfxId.BossRewardReveal: return "Rewards/boss_reward_reveal.mp3";
                case SfxId.NewSkillChoose: return "Rewards/new_skill_choose.mp3";
                case SfxId.SkillEquip: return "Rewards/skill_equip.mp3";
                case SfxId.SkillUnequip: return "Rewards/skill_unequip.mp3";
                case SfxId.DeckCardAdd: return "Rewards/deck_card_add.mp3";
                case SfxId.DeckCardRemove: return "Rewards/deck_card_remove.mp3";
                case SfxId.CardFilterChange: return "Rewards/card_filter_change.mp3";
                case SfxId.CardPageChange: return "Rewards/card_page_change.mp3";
                case SfxId.StatPointAdd: return "Rewards/stat_point_add.mp3";
                case SfxId.StatPointUndo: return "Rewards/stat_point_undo.mp3";
                case SfxId.StatPointsConfirm: return "Rewards/stat_points_confirm.mp3";
                case SfxId.CardRecipeSelect: return "Rewards/card_recipe_select.mp3";
                case SfxId.CardCraftSuccess: return "Rewards/card_craft_success.mp3";
                case SfxId.TileTypeSelect: return "Rewards/tile_type_select.mp3";
                case SfxId.TileUpgradeSelect: return "Rewards/tile_upgrade_select.mp3";
                case SfxId.TileSlotSelect: return "Rewards/tile_slot_select.mp3";
                case SfxId.TileUpgradeRemove: return "Rewards/tile_upgrade_remove.mp3";
                case SfxId.TileCraftSuccess: return "Rewards/tile_craft_success.mp3";
                case SfxId.UiHover: return "Rewards/ui_hover.mp3";
                default: return null;
            }
        }
    }
}
