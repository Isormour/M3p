using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    public static class SfxLibraryBuilder
    {
        const string LibraryPath = "Assets/_Project/Audio/SfxLibrary.asset";
        const string SfxRoot = "Assets/_Project/Audio/SFX";

        [MenuItem("M3P/Build SFX Library")]
        public static void Build()
        {
            SfxLibrary library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SfxLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var clips = new List<SfxLibrary.ClipEntry>();
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None)
                    continue;

                string relative = RelativePath(id);
                if (relative == null)
                    continue;

                AudioClip clip = LoadClip(relative);
                if (clip == null)
                {
                    Debug.LogWarning($"SFX library: missing clip for {id} at {SfxRoot}/{relative}");
                    continue;
                }

                clips.Add(new SfxLibrary.ClipEntry { Id = id, Clip = clip });
            }

            var skills = new List<SfxLibrary.SkillClip>();
            AddSkillFolder(skills, "Skills/Basic", "basic_layer.mp3");
            AddSkillFolder(skills, "Skills/Enemy", "enemy_layer.mp3");
            AddSkillFolder(skills, "Skills/Mage", "mage_layer.mp3");
            AddSkillFolder(skills, "Skills/Warrior", "warrior_layer.mp3");
            AddSkillFolder(skills, "Skills/Shadow", "shadow_layer.mp3");

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty clipProp = serialized.FindProperty("_clips");
            clipProp.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                SerializedProperty element = clipProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Id").enumValueIndex = (int)clips[i].Id;
                element.FindPropertyRelative("Clip").objectReferenceValue = clips[i].Clip;
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
                case SfxId.SceneTransition: return "MenuMap/scene_transition.mp3";
                case SfxId.MapNodeSelect: return "MenuMap/map_node_select.mp3";
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
