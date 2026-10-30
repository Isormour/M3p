using System.Collections.Generic;
using System.IO;
using M3P.RemoteConfig;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    [InitializeOnLoad]
    static class RemoteConfigSelfCheck
    {
        const string ReportDirectory = "Library/M3PRemoteConfig";
        const string RequestPath = ReportDirectory + "/SelfCheck.request";
        const string ReportPath = ReportDirectory + "/SelfCheck.txt";
        static double _nextRequestCheck;

        static RemoteConfigSelfCheck()
        {
            EditorApplication.update += CheckRequestedRun;
        }

        static void CheckRequestedRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (EditorApplication.timeSinceStartup < _nextRequestCheck)
                return;
            _nextRequestCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);
            RunFromMenu();
        }

        [MenuItem("M3P/Remote Config/Self Check")]
        public static void RunFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            string report;
            try
            {
                report = Run();
            }
            catch (System.Exception exception)
            {
                report = "M3P Remote Config self-check FAIL\n" + exception;
            }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportPath, report);
            Debug.Log(report);
            if (report.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                Debug.LogError(report);
        }

        public static string Run()
        {
            var failures = new List<string>();
            CheckDocument(failures);
            CheckBattleExport(failures);
            CheckLevelValidation(failures);
            CheckIdentity(failures);
            CheckEnemyRuntimeGraph(failures);
            CheckPartialReferenceApplication(failures);
            return failures.Count == 0
                ? "M3P Remote Config self-check: OK"
                : "M3P Remote Config self-check FAIL\n" + string.Join("\n", failures);
        }

        static void CheckDocument(List<string> failures)
        {
            const string reordered = "{\"schemaVersion\":1,\"revision\":4,\"objects\":{\"abc\":{\"typeId\":\"m3p.battle\",\"typeSchemaVersion\":1,\"data\":{\"b\":1,\"a\":[1,2]}}}}";
            const string original = "{\"objects\":{\"abc\":{\"data\":{\"a\":[1,2],\"b\":1},\"typeSchemaVersion\":1,\"typeId\":\"m3p.battle\"}},\"revision\":9,\"schemaVersion\":1}";
            if (!M3pConfigDocument.TryParse(reordered, out JObject first, out string error))
                failures.Add("parse reordered: " + error);
            if (!M3pConfigDocument.TryParse(original, out JObject second, out error))
                failures.Add("parse original: " + error);
            if (first != null && second != null && M3pConfigDocument.ContentHash(first) != M3pConfigDocument.ContentHash(second))
                failures.Add("field order or revision changed the content hash");

            var moved = JObject.Parse(reordered);
            ((JArray)moved["objects"]["abc"]["data"]["a"])[0] = 2;
            ((JArray)moved["objects"]["abc"]["data"]["a"])[1] = 1;
            if (first != null && M3pConfigDocument.ContentHash(first) == M3pConfigDocument.ContentHash(moved))
                failures.Add("array reorder was ignored");

            const string duplicate = "{\"schemaVersion\":1,\"revision\":1,\"objects\":{},\"objects\":{}}";
            if (M3pConfigDocument.TryParse(duplicate, out _, out _))
                failures.Add("duplicate keys were accepted");

            var intField = new ConfigField { name = "value", kind = ConfigContracts.Int };
            if (ConfigValueCodec.ValuesEqual(new JValue(0), JValue.CreateNull(), intField))
                failures.Add("null compared equal to zero");
            bool nanAccepted = false;
            try
            {
                nanAccepted = ConfigValueCodec.TryNormalize(
                    new JValue(double.NaN),
                    new ConfigField { kind = ConfigContracts.Float },
                    out _,
                    out _);
            }
            catch (System.Exception)
            {
                nanAccepted = false;
            }

            if (nanAccepted)
                failures.Add("NaN was accepted");
            var text = new ConfigField { kind = ConfigContracts.String };
            if (!ConfigValueCodec.ValuesEqual(new JValue(" a "), new JValue(" a "), text))
                failures.Add("internal whitespace was not preserved");
            if (ConfigValueCodec.ValuesEqual(new JValue("a"), new JValue(" a "), text))
                failures.Add("different strings compared equal");
        }

        static void CheckBattleExport(List<string> failures)
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            try
            {
                var serialized = new SerializedObject(config);
                serialized.FindProperty("_additionalAttackPerCascade").intValue = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ExportResult export = ConfigExporter.Export(config, "0123456789abcdef0123456789abcdef", "39f6c8ce4b814acb9f32a5591058b3e7", true);
                if (export.Support != SupportKind.Supported)
                    failures.Add("BattleConfig support: " + export.Support + " " + export.Reason);
                if (export.Contract == null || export.Contract.typeId != M3pConfigTypes.Battle)
                    failures.Add("BattleConfig type id");
                if (export.Data == null || export.Data.Value<int>("additionalAttackPerCascade") != 2)
                    failures.Add("additionalAttackPerCascade was not exported as 2");
                ConfigField bonuses = export.Contract != null ? export.Contract.FindField("damagePerSupermatchSize") : null;
                if (bonuses == null || bonuses.exactArrayLength != 8)
                    failures.Add("supermatch contract length");

                var copy = ScriptableObject.CreateInstance<BattleConfig>();
                try
                {
                    var errors = new List<string>();
                    if (!ConfigValueApplier.Apply(copy, export.Contract, export.Data, errors))
                        failures.Add("apply battle: " + string.Join("; ", errors));
                    else if (copy.AdditionalAttackPerCascade != 2)
                        failures.Add("runtime copy did not receive cascade attacks");
                }
                finally
                {
                    Object.DestroyImmediate(copy);
                }

                var shortArray = (JObject)export.Data.DeepClone();
                shortArray["damagePerSupermatchSize"] = new JArray(0, 0, 0);
                var validation = new List<string>();
                BalanceConfigValidator.Validate(M3pConfigTypes.Battle, export.Contract, shortArray, validation);
                if (validation.Count == 0)
                    failures.Add("short supermatch array was accepted");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        static void CheckLevelValidation(List<string> failures)
        {
            LevelProgressionConfig config = ScriptableObject.CreateInstance<LevelProgressionConfig>();
            try
            {
                ExportResult export = ConfigExporter.Export(config, "abcdefabcdefabcdefabcdefabcdefab", "7f37a563458a4fdbabfdb9ba3ca8dd93", true);
                if (export.Support != SupportKind.Supported)
                    failures.Add("LevelProgressionConfig support: " + export.Support + " " + export.Reason);
                var broken = (JObject)export.Data.DeepClone();
                broken["baseExperiencePerLevel"] = 0;
                var errors = new List<string>();
                BalanceConfigValidator.Validate(M3pConfigTypes.LevelProgression, export.Contract, broken, errors);
                if (errors.Count == 0)
                    failures.Add("non-positive experience was accepted");

                broken["baseExperiencePerLevel"] = 1;
                broken["levelCap"] = 100000;
                broken["experienceIncreasePerLevel"] = 100000;
                errors.Clear();
                BalanceConfigValidator.Validate(M3pConfigTypes.LevelProgression, export.Contract, broken, errors);
                if (errors.Count == 0)
                    failures.Add("overflowing progression was accepted");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        static void CheckIdentity(List<string> failures)
        {
            var manifest = new RemoteAssetManifestData();
            var first = RemoteAssetManifest.EnsureIdentity(manifest, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var again = RemoteAssetManifest.EnsureIdentity(manifest, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var copy = RemoteAssetManifest.EnsureIdentity(manifest, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            if (first.remoteId != again.remoteId)
                failures.Add("the same guid received a new remote id");
            if (first.remoteId == copy.remoteId)
                failures.Add("a copied guid reused a remote id");
            if (first.remoteId == null || first.remoteId.Length != 32)
                failures.Add("remote id is not a stable 32-character token");
            again.remoteId = copy.remoteId;
            if (!RemoteAssetManifest.HasDuplicateRemoteIds(manifest, out _))
                failures.Add("duplicate remote ids were not detected");
        }

        [System.Serializable]
        struct EncounterSlot
        {
            public EncounterConfig Encounter;
        }

        [System.Serializable]
        sealed class EncounterLink
        {
            [SerializeReference] public EncounterLink Next;
            public EncounterConfig Encounter;
        }

        [System.Serializable]
        sealed class EncounterOwner
        {
            [SerializeField] EncounterConfig _encounter;
            public List<EncounterSlot> Routes = new List<EncounterSlot>();
            [SerializeReference] public EncounterLink Cycle;
            [System.NonSerialized] public EnemyDefinition CachedEnemy;
            public EncounterConfig Encounter => _encounter;

            public EncounterOwner(EncounterConfig encounter, EnemyDefinition cachedEnemy)
            {
                _encounter = encounter;
                Routes.Add(new EncounterSlot { Encounter = encounter });
                Cycle = new EncounterLink { Encounter = encounter };
                Cycle.Next = Cycle;
                CachedEnemy = cachedEnemy;
            }
        }

        static void CheckEnemyRuntimeGraph(List<string> failures)
        {
            var sources = new List<ScriptableObject>();
            RuntimeConfigSession session = null;
            RuntimeConfigSession tileSession = null;
            try
            {
                var enemy = NewSource<EnemyDefinition>(sources, "Leszinio");
                var enemies = NewSource<EnemiesConfig>(sources, "Enemies");
                var encounter = NewSource<EncounterConfig>(sources, "Forest encounter");
                var game = NewSource<GameConfig>(sources, "Game");
                var progression = NewSource<StatProgressionConfig>(sources, "Progression");
                var tree = NewSource<PerkTree>(sources, "Constitution");
                var healthPerk = NewSource<PerkDefinition>(sources, "Health +10");
                var skill = NewSource<SkillDefinition>(sources, "Vine attack");
                var tile = NewSource<Match3.Match3TileTypeDefinition>(sources, "Nature");
                var catalog = NewSource<RemoteConfigCatalogAsset>(sources, "Test catalog");

                EditSource(healthPerk, data => data.FindProperty("_logic").managedReferenceValue = new AddMaxHP(10));
                EditSource(tree, data =>
                {
                    data.FindProperty("id").intValue = 7;
                    data.FindProperty("TreeStatBonusLogic").managedReferenceValue = new AddHardStatBonus(EStatType.Constitution);
                    SerializedProperty perks = data.FindProperty("perks");
                    int[] thresholds = { 1, 3, 6, 8, 10 };
                    perks.arraySize = thresholds.Length;
                    for (int i = 0; i < thresholds.Length; i++)
                    {
                        SerializedProperty entry = perks.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("StatLevel").intValue = thresholds[i];
                        entry.FindPropertyRelative("Perk").objectReferenceValue = i < 4 ? healthPerk : null;
                    }
                });
                EditSource(progression, data => SetReferences(data.FindProperty("_perkTrees"), tree));
                EditSource(skill, data =>
                {
                    SerializedProperty costs = data.FindProperty("_manaCosts");
                    costs.arraySize = 1;
                    costs.GetArrayElementAtIndex(0).FindPropertyRelative("TileType").objectReferenceValue = tile;
                    costs.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = 2;
                });
                EditSource(enemy, data =>
                {
                    data.FindProperty("_name").stringValue = "Leszinio";
                    data.FindProperty("_hardStats.Constitution").intValue = 3;
                    SetReferences(data.FindProperty("_skills"), skill);
                });
                EditSource(enemies, data =>
                {
                    SerializedProperty entries = data.FindProperty("_entries");
                    entries.arraySize = 1;
                    entries.GetArrayElementAtIndex(0).FindPropertyRelative("Id").intValue = 37;
                    entries.GetArrayElementAtIndex(0).FindPropertyRelative("Enemy").objectReferenceValue = enemy;
                });
                EditSource(encounter, data => SetReferences(data.FindProperty("_enemies"), enemy));
                EditSource(game, data =>
                {
                    data.FindProperty("_enemies").objectReferenceValue = enemies;
                    data.FindProperty("_statProgression").objectReferenceValue = progression;
                    SetReferences(data.FindProperty("_tileTypes"), tile);
                });

                const string remoteId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                ExportResult export = ConfigExporter.Export(enemy, string.Empty, remoteId, true);
                if (export.Support != SupportKind.Supported && export.Support != SupportKind.Partial)
                {
                    failures.Add("enemy graph fixture export: " + export.Support + " " + export.Reason);
                    return;
                }
                catalog.entries = new[]
                {
                    new RemoteConfigCatalogEntry
                    {
                        remoteId = remoteId,
                        source = enemy,
                        typeId = export.Contract.typeId,
                        typeSchemaVersion = export.Contract.typeSchemaVersion,
                        contractJson = ConfigValueApplier.ToJson(export.Contract),
                    },
                };

                // Populate source lookup caches before cloning, as a running editor may have done.
                if (enemies.GetEnemy(37) != enemy || enemies.GetEnemyId(enemy) != 37)
                    failures.Add("enemy graph fixture has no stable enemy id");
                session = RuntimeConfigFactory.Build(game, catalog);
                EnemyDefinition registryEnemy = session.Config.Enemies.GetEnemy(37);
                var owner = new EncounterOwner(encounter, enemy);
                RuntimeConfigFactory.RewireOwner(owner, session);
                EnemyDefinition mapEnemy = owner.Encounter.PickEnemy(42, "node");

                if (session.Config == game || session.Config.Enemies == enemies || owner.Encounter == encounter)
                    failures.Add("enemy graph retained an original owner config");
                if (registryEnemy == enemy || mapEnemy != registryEnemy)
                    failures.Add("enemy registry and map encounter do not share one runtime enemy copy");
                if (owner.Routes[0].Encounter != owner.Encounter)
                    failures.Add("encounter inside list-of-struct owner was not rewired");
                if (owner.Cycle.Next != owner.Cycle || owner.Cycle.Encounter != owner.Encounter)
                    failures.Add("managed owner cycle lost its identity or runtime encounter");
                if (owner.CachedEnemy != enemy)
                    failures.Add("nonserialized owner cache was traversed");
                if (HealthFor(registryEnemy, session.Config.StatProgression) != 40)
                    failures.Add("enemy graph local CON 3 should give 40 HP");

                var document = M3pConfigDocument.CreateEmpty(1);
                JObject remoteData = (JObject)export.Data.DeepClone();
                remoteData["hardStats"]["constitution"] = 10;
                M3pConfigDocument.Objects(document)[remoteId] = new JObject
                {
                    ["typeId"] = export.Contract.typeId,
                    ["typeSchemaVersion"] = export.Contract.typeSchemaVersion,
                    ["data"] = remoteData,
                };
                if (!RuntimeConfigFactory.ApplyDocument(session, catalog, document, out string error))
                    failures.Add("apply enemy graph document: " + error);
                if (HealthFor(session.Config.Enemies.GetEnemy(37), session.Config.StatProgression) != 60 ||
                    HealthFor(owner.Encounter.PickEnemy(42, "node"), session.Config.StatProgression) != 60)
                    failures.Add("remote CON 10 did not reach both registry and map preview as 60 HP");
                if (session.Config.Enemies.GetEnemyId(mapEnemy) != 37)
                    failures.Add("runtime enemy lost its stable registry id");
                if (mapEnemy.Skills.Length != 1 || mapEnemy.Skills[0] != skill ||
                    mapEnemy.Skills[0].ManaCosts[0].TileType != session.Config.GetTileType(0))
                    failures.Add("enemy-only selection changed an unrelated skill/mana graph");
                if (session.Config.GetTileTypeKey(0) != "Nature" || mapEnemy.name != "Leszinio")
                    failures.Add("runtime cloning changed asset names used as save keys");
                if (enemy.HardStats.Constitution != 3 || HealthFor(enemy, progression) != 40 ||
                    enemies.GetEnemy(37) != enemy || encounter.PickEnemy(42, "node") != enemy ||
                    skill.ManaCosts[0].TileType != tile || enemy.Skills[0] != skill ||
                    game.Enemies != enemies || enemy.name != "Leszinio" || tile.name != "Nature")
                    failures.Add("remote application mutated the original config graph");

                RuntimeConfigFactory.RewireOwner(owner, session);
                if (owner.Encounter.PickEnemy(42, "node") != mapEnemy)
                    failures.Add("rewiring an existing runtime owner created another enemy copy");

                // Selecting a nested leaf must clone its owners, including the mana-cost struct array.
                catalog.entries = new[] { new RemoteConfigCatalogEntry { source = tile } };
                tileSession = RuntimeConfigFactory.Build(game, catalog);
                EnemyDefinition nestedEnemy = tileSession.Config.Enemies.GetEnemy(37);
                var nestedOwner = new EncounterOwner(encounter, enemy);
                RuntimeConfigFactory.RewireOwner(nestedOwner, tileSession);
                if (tileSession.Config.GetTileType(0) == tile || nestedEnemy == enemy ||
                    nestedEnemy.Skills[0] == skill ||
                    nestedEnemy.Skills[0].ManaCosts[0].TileType != tileSession.Config.GetTileType(0) ||
                    nestedOwner.Encounter.PickEnemy(42, "node") != nestedEnemy)
                    failures.Add("selected nested tile did not reach the shared skill/encounter owner graph");
                if (tileSession.Config.GetTileTypeKey(0) != "Nature" || skill.ManaCosts[0].TileType != tile)
                    failures.Add("selected nested tile changed its save key or source skill");
            }
            catch (System.Exception exception)
            {
                failures.Add("enemy runtime graph: " + exception);
            }
            finally
            {
                var cleanup = new HashSet<Object>();
                if (session != null)
                {
                    cleanup.Add(session.Config);
                    foreach (Object copy in session.Copies.Values)
                        cleanup.Add(copy);
                }
                if (tileSession != null)
                {
                    cleanup.Add(tileSession.Config);
                    foreach (Object copy in tileSession.Copies.Values)
                        cleanup.Add(copy);
                }
                foreach (ScriptableObject source in sources)
                    cleanup.Add(source);
                foreach (Object value in cleanup)
                    if (value != null)
                        Object.DestroyImmediate(value);
            }
        }

        static T NewSource<T>(List<ScriptableObject> sources, string name) where T : ScriptableObject
        {
            T source = ScriptableObject.CreateInstance<T>();
            source.name = name;
            sources.Add(source);
            return source;
        }

        static void CheckPartialReferenceApplication(List<string> failures)
        {
            var sources = new List<ScriptableObject>();
            EnemyDefinition copy = null;
            try
            {
                var enemy = NewSource<EnemyDefinition>(sources, "Skill owner");
                var skill = NewSource<SkillDefinition>(sources, "New skill");
                var replaces = NewSource<SkillDefinition>(sources, "Previous skill");
                EditSource(enemy, data =>
                {
                    SerializedProperty unlocks = data.FindProperty("_skillUnlocks");
                    unlocks.arraySize = 1;
                    SerializedProperty unlock = unlocks.GetArrayElementAtIndex(0);
                    unlock.FindPropertyRelative("Skill").objectReferenceValue = skill;
                    unlock.FindPropertyRelative("Replaces").objectReferenceValue = replaces;
                    unlock.FindPropertyRelative("MinFloor").intValue = 1;
                });
                ExportResult export = ConfigExporter.Export(enemy, string.Empty, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", true);
                copy = Object.Instantiate(enemy);
                JObject remoteData = (JObject)export.Data.DeepClone();
                remoteData["skillUnlocks"][0]["minFloor"] = 4;
                remoteData["rankThresholds"] = new JArray(
                    new JObject { ["minFloor"] = 1, ["rankName"] = "Scout" },
                    new JObject { ["minFloor"] = 6, ["rankName"] = "Veteran" });
                var errors = new List<string>();
                if (!ConfigValueApplier.Apply(copy, export.Contract, remoteData, errors))
                    failures.Add("partial-reference apply: " + string.Join("; ", errors));
                var actualUnlocks = new List<EnemySkillUnlock>();
                copy.CollectSkillUnlocks(actualUnlocks);
                if (actualUnlocks.Count != 1 || actualUnlocks[0].Skill != skill ||
                    actualUnlocks[0].Replaces != replaces || actualUnlocks[0].MinFloor != 4)
                    failures.Add("remote skill unlock threshold erased its local Skill/Replaces references");
                if (copy.RankThresholds == null || copy.RankThresholds.Count != 2 || copy.RankThresholds[1].RankName != "Veteran")
                    failures.Add("fully exported rank array could not resize");

                string acceptedSnapshot = EditorJsonUtility.ToJson(copy);
                JObject invalid = (JObject)remoteData.DeepClone();
                invalid["name"] = "Must not apply";
                invalid["hardStats"]["constitution"] = 99;
                ((JArray)invalid["skillUnlocks"]).Add(new JObject { ["minFloor"] = 8 });
                errors.Clear();
                if (ConfigValueApplier.Apply(copy, export.Contract, invalid, errors))
                    failures.Add("resizing partially exported skill unlocks was accepted");
                if (EditorJsonUtility.ToJson(copy) != acceptedSnapshot)
                    failures.Add("failed partial-reference resize changed earlier fields");

                var originalUnlocks = new List<EnemySkillUnlock>();
                enemy.CollectSkillUnlocks(originalUnlocks);
                if (originalUnlocks.Count != 1 || originalUnlocks[0].MinFloor != 1 ||
                    originalUnlocks[0].Skill != skill || originalUnlocks[0].Replaces != replaces)
                    failures.Add("partial-reference apply mutated the source unlocks");
            }
            catch (System.Exception exception)
            {
                failures.Add("partial-reference application: " + exception);
            }
            finally
            {
                if (copy != null)
                    Object.DestroyImmediate(copy);
                foreach (ScriptableObject source in sources)
                    if (source != null)
                        Object.DestroyImmediate(source);
            }
        }

        static void EditSource(ScriptableObject source, System.Action<SerializedObject> edit)
        {
            using (var data = new SerializedObject(source))
            {
                edit(data);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetReferences(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static int HealthFor(EnemyDefinition definition, StatProgressionConfig progression)
        {
            EnemyRuntimeSpec spec = EnemyProgressionResolver.Resolve(definition, 1, MapNodeType.Battle);
            var stats = new CharacterStats(spec.HardStats, progression);
            stats.Soft.ScaleMaxHealth(spec.HealthMultiplier);
            stats.Soft.AddBonusMaxHealth(spec.Definition.BonusMaxHp);
            return stats.MaxHealth;
        }
    }
}
