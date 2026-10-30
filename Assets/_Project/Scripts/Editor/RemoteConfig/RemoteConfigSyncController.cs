using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using M3P.RemoteConfig;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    public sealed class DiffRow
    {
        public string Key;
        public string RemoteId;
        public string AssetName;
        public string FieldName;
        public string EditorText;
        public string RemoteText;
        public bool IsError;
        public bool IsNewObject;
        public bool IsResource;
        public bool Selected = true;
        public bool Locked;
        public string Error;
        public UnityEngine.Object Asset;
    }

    public static class RemoteConfigSyncController
    {
        const string SnoozeKey = "M3P.RemoteConfig.Snooze";
        const double NetworkInterval = 60d;

        public static bool Busy { get; private set; }
        public static string Message = "Wybierz ScriptableObjecty.";
        public static CloudSession Cloud;
        public static SettingsSnapshot Remote = new SettingsSnapshot();
        public static List<ExportResult> Assets = new List<ExportResult>();
        public static List<DiffRow> Diffs = new List<DiffRow>();
        public static RemoteAssetManifestData Manifest = new RemoteAssetManifestData();
        public static string Signature = string.Empty;
        public static bool LastCheckOk;
        public static DateTime? LastSuccessLocal;
        public static int Generation { get; private set; }

        public static event Action Changed;

        public static void LoadManifest()
        {
            Manifest = RemoteAssetManifest.Load();
            if (Manifest.schemaVersion < 0)
                Message = "Manifest tożsamości jest uszkodzony. Rozstrzygnij konflikt przed publikacją.";
        }

        public static void LoadLocalList()
        {
            LoadManifest();
            if (Manifest.schemaVersion < 0)
            {
                Notify();
                return;
            }

            RebuildAssets();
            Message = "Lista lokalna: " + Assets.Count + " assetów. Sprawdź teraz porównuje je z Remote Config.";
            Notify();
        }

        public static async void RequestCheck(bool forceNetwork, bool openPopup)
        {
            try
            {
                await CheckAsync(forceNetwork, openPopup);
            }
            catch (Exception exception)
            {
                Busy = false;
                Message = "Nie udało się sprawdzić. " + exception.Message;
                Notify();
            }
        }

        public static async Task CheckAsync(bool forceNetwork, bool openPopup)
        {
            if (Busy || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            Busy = true;
            Notify();
            int generation = Generation;
            try
            {
                LoadManifest();
                if (Manifest.schemaVersion < 0)
                {
                    LastCheckOk = false;
                    Message = "Manifest tożsamości jest uszkodzony. Rozstrzygnij konflikt przed publikacją.";
                    return;
                }

                CloudSession recognized = await EditorCloudBootstrap.RecognizeAsync(Manifest.environmentId);
                if (generation != Generation)
                    return;

                if (!recognized.EnvironmentsLoaded && Cloud != null && Cloud.ProjectId == recognized.ProjectId && Cloud.Target != null)
                {
                    recognized.Target = Cloud.Target;
                    recognized.Environments = Cloud.Environments;
                }

                Cloud = recognized;
                RememberTarget();
                RebuildAssets();
                await RefreshRemoteAsync(forceNetwork);
                if (generation != Generation)
                    return;

                BuildDiff(preserveSelection: true);
                UpdateMessage();
                if (openPopup && Manifest.autoCheck && LastCheckOk && HasSendableChange() && !Application.isPlaying)
                    MaybePopup();
            }
            finally
            {
                Busy = false;
                Notify();
            }
        }

        public static void SetTarget(string environmentId)
        {
            if (Cloud == null)
                return;

            CloudEnvironment environment = EditorCloudBootstrap.Find(Cloud.Environments, environmentId);
            if (environment == null || (Cloud.Target != null && Cloud.Target.Id == environment.Id))
                return;

            Generation++;
            Cloud.Target = environment;
            Manifest.environmentId = environment.Id;
            Manifest.environmentName = environment.Name;
            RemoteAssetManifest.Save(Manifest);
            SessionState.EraseString(SnoozeKey);
            RequestCheck(true, false);
        }

        public static void SetSelected(ExportResult export, bool selected)
        {
            if (export == null || string.IsNullOrEmpty(export.Guid))
                return;
            if (!selected)
            {
                RemoteAssetManifestEntry entry = RemoteAssetManifest.Find(Manifest, export.Guid);
                if (entry != null)
                    entry.selected = false;
                export.Selected = false;
            }
            else if (export.Support == SupportKind.Supported || export.Support == SupportKind.Partial)
            {
                RemoteAssetManifestEntry entry = RemoteAssetManifest.EnsureIdentity(Manifest, export.Guid);
                entry.selected = true;
                export.RemoteId = entry.remoteId;
                export.Selected = true;
            }

            RemoteAssetManifest.Save(Manifest);
            ExportResult refreshed = ConfigExporter.Export(export.Asset, export.Guid, export.RemoteId, export.Selected);
            int index = Assets.IndexOf(export);
            if (index >= 0)
                Assets[index] = refreshed;
            BuildDiff(preserveSelection: false);
            WriteCatalog();
            UpdateMessage();
            Notify();
            if (selected)
                RequestCheck(false, true);
        }

        public static void AddSelectionFromProject()
        {
            UnityEngine.Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not ScriptableObject asset)
                    continue;
                ExportResult row = FindAsset(asset);
                if (row == null)
                {
                    RebuildAssets();
                    row = FindAsset(asset);
                }

                if (row != null)
                    SetSelected(row, true);
            }
        }

        public static void SelectRelated(ExportResult export)
        {
            if (export == null)
                return;

            for (int i = 0; i < export.Related.Count; i++)
            {
                EditorGUIUtility.PingObject(export.Related[i]);
                Selection.activeObject = export.Related[i];
            }
        }

        public static bool NeedsSave()
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (export.Selected && export.Asset != null && EditorUtility.IsDirty(export.Asset))
                    return true;
            }

            return false;
        }

        public static async void RequestPublish(bool saveFirst)
        {
            try
            {
                await PublishAsync(saveFirst);
            }
            catch (Exception exception)
            {
                Busy = false;
                Message = "Publikacja nie doszła do skutku. " + exception.Message;
                Notify();
            }
        }

        public static void Snooze()
        {
            if (!string.IsNullOrEmpty(Signature))
                SessionState.SetString(SnoozeKey, Signature);
        }

        public static void SetAutoCheck(bool enabled)
        {
            Manifest.autoCheck = enabled;
            RemoteAssetManifest.Save(Manifest);
            Notify();
        }

        public static void SetNewEnvironmentName(string name)
        {
            Manifest.newEnvironmentName = name ?? string.Empty;
            RemoteAssetManifest.Save(Manifest);
            BuildDiff(true);
            Notify();
        }

        public static string SendButtonLabel()
        {
            return NeedsSave()
                ? "Zapisz configi i wyślij wartości z edytora"
                : "Wyślij wartości z edytora";
        }

        static async Task PublishAsync(bool saveFirst)
        {
            if (Busy || Application.isPlaying || EditorApplication.isCompiling)
            {
                Message = Application.isPlaying
                    ? "Publikacja czeka na wyjście z Play Mode."
                    : "Poczekaj na zakończenie kompilacji.";
                Notify();
                return;
            }

            if (Manifest.schemaVersion < 0 || RemoteAssetManifest.HasDuplicateRemoteIds(Manifest, out _))
            {
                Message = "Konflikt identyfikatorów w manifeście blokuje publikację.";
                Notify();
                return;
            }

            if (!CanPublishTarget(out string targetError))
            {
                Message = targetError;
                Notify();
                return;
            }

            Busy = true;
            Notify();
            string accepted = Signature;
            try
            {
                if (saveFirst && !SaveSelectedAssets(out string saveError))
                {
                    Message = saveError;
                    return;
                }

                await RefreshAfterRecognizeAsync();
                if (Signature != accepted)
                {
                    Message = "Źródła, remote albo cel zmieniły się od porównania. Sprawdź różnice i wyślij ponownie.";
                    return;
                }

                if (!TryBuildPayload(out JArray values, out JObject document, out string payloadError))
                {
                    Message = payloadError;
                    return;
                }

                if (!WriteSnapshot(values, document, out string snapshotError))
                {
                    Message = snapshotError;
                    return;
                }

                string environmentId = Cloud.Target != null ? Cloud.Target.Id : null;
                if (Cloud.NoEnvironment)
                {
                    AdminResponse createdEnvironment = await RemoteConfigAdminClient.CreateEnvironmentAsync(
                        Cloud.ProjectId,
                        Manifest.newEnvironmentName.Trim());
                    if (createdEnvironment.Status != AdminStatus.Ok)
                    {
                        Message = createdEnvironment.Message ?? "Nie utworzono środowiska.";
                        return;
                    }

                    environmentId = ReadId(createdEnvironment.Body);
                    if (string.IsNullOrEmpty(environmentId))
                    {
                        Message = "Usługa nie zwróciła identyfikatora środowiska.";
                        return;
                    }

                    Manifest.environmentId = environmentId;
                    Manifest.environmentName = Manifest.newEnvironmentName.Trim();
                    RemoteAssetManifest.Save(Manifest);
                }

                SettingsSnapshot fresh = await RemoteConfigAdminClient.FetchSettingsAsync(Cloud.ProjectId, environmentId);
                if (fresh.Status != AdminStatus.Ok)
                {
                    Message = fresh.Message ?? "Nie udało się odczytać konfiguracji przed zapisem.";
                    LastCheckOk = false;
                    return;
                }

                AdminResponse write = fresh.ConfigExists
                    ? await RemoteConfigAdminClient.UpdateSettingsAsync(Cloud.ProjectId, fresh.ConfigId, values, fresh.ETag)
                    : await RemoteConfigAdminClient.CreateSettingsAsync(Cloud.ProjectId, environmentId, values);

                if (write.Status == AdminStatus.Network)
                {
                    Message = await ConfirmOrUnverified(environmentId, document);
                    return;
                }

                if (write.Status != AdminStatus.Ok)
                {
                    Message = write.Message ?? "Publikacja nie doszła do skutku.";
                    return;
                }

                Message = await ConfirmOrUnverified(environmentId, document);
            }
            finally
            {
                Busy = false;
                string outcome = Message;
                await RefreshRemoteAsync(true);
                BuildDiff(true);
                Message = outcome;
                Notify();
            }
        }

        static async Task<string> ConfirmOrUnverified(string environmentId, JObject expected)
        {
            string expectedText = M3pConfigDocument.Serialize(expected);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(1000);

                SettingsSnapshot read = await RemoteConfigAdminClient.FetchSettingsAsync(Cloud.ProjectId, environmentId);
                if (read.Status != AdminStatus.Ok || read.Document == null)
                    continue;

                if (M3pConfigDocument.Serialize(read.Document) == expectedText)
                    return "Wysłano wybrane zmiany.";
            }

            return "Wynik publikacji niepotwierdzony. Sprawdź remote przed kolejną wysyłką.";
        }

        static async Task RefreshAfterRecognizeAsync()
        {
            CloudSession recognized = await EditorCloudBootstrap.RecognizeAsync(Manifest.environmentId);
            if (recognized.EnvironmentsLoaded)
                Cloud = recognized;
            RememberTarget();
            RebuildAssets();
            await RefreshRemoteAsync(true);
            BuildDiff(true);
        }

        static async Task RefreshRemoteAsync(bool forceNetwork)
        {
            if (Cloud == null || Cloud.Target == null)
            {
                if (Cloud != null && Cloud.NoEnvironment)
                {
                    Remote = new SettingsSnapshot { Status = AdminStatus.Ok, Message = "Brak środowiska." };
                    LastCheckOk = true;
                }
                else
                {
                    Remote = new SettingsSnapshot
                    {
                        Status = AdminStatus.Failed,
                        Message = Cloud != null ? Cloud.Blocker : "Brak projektu.",
                    };
                    LastCheckOk = false;
                }

                return;
            }

            bool stale = !LastSuccessLocal.HasValue || (DateTime.UtcNow - LastSuccessLocal.Value.ToUniversalTime()).TotalSeconds >= NetworkInterval;
            if (!forceNetwork && !stale && Remote != null && Remote.Status == AdminStatus.Ok && SameTarget(RemoteCacheEnvironment))
                return;

            SettingsSnapshot snapshot = await RemoteConfigAdminClient.FetchSettingsAsync(Cloud.ProjectId, Cloud.Target.Id);
            Remote = snapshot;
            if (snapshot.Status == AdminStatus.Ok)
            {
                LastCheckOk = true;
                LastSuccessLocal = DateTime.Now;
                RemoteCacheEnvironment = Cloud.Target.Id;
                if (!snapshot.SawETag)
                    snapshot.Message = Append(snapshot.Message, "API nie udostępniło blokady wersji. Publikuj pojedynczo w tym środowisku.");
            }
            else
            {
                LastCheckOk = false;
            }
        }

        static string RemoteCacheEnvironment;

        static void RebuildAssets()
        {
            Assets = new List<ExportResult>();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Project" });
            var seen = new HashSet<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || !seen.Add(guids[i]))
                    continue;

                RemoteAssetManifestEntry entry = RemoteAssetManifest.Find(Manifest, guids[i]);
                ExportResult export = ConfigExporter.Export(asset, guids[i], entry != null ? entry.remoteId : null, false);
                bool selectable = export.Support == SupportKind.Supported || export.Support == SupportKind.Partial;
                export.Selected = entry != null && entry.selected && selectable;
                Assets.Add(export);
            }

            Assets.Sort((a, b) => string.Compare(
                a.Asset != null ? a.Asset.name : string.Empty,
                b.Asset != null ? b.Asset.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));
        }

        static void BuildDiff(bool preserveSelection)
        {
            var previous = new Dictionary<string, bool>();
            if (preserveSelection)
            {
                for (int i = 0; i < Diffs.Count; i++)
                    previous[Diffs[i].Key] = Diffs[i].Selected;
            }

            Diffs = new List<DiffRow>();
            if (Cloud != null && Cloud.NoEnvironment)
                AddResource("env", "Środowisko " + (Manifest.newEnvironmentName ?? string.Empty).Trim() + " zostanie utworzone.");
            if (LastCheckOk && Remote != null && !Remote.ConfigExists)
                AddResource("config", "Bazowa konfiguracja settings zostanie utworzona.");
            if (LastCheckOk && Remote != null && Remote.ConfigExists && !Remote.KeyExists)
                AddResource("key", "Klucz " + M3pConfigDocument.Key + " zostanie utworzony.");

            if (!LastCheckOk)
            {
                Signature = "failed";
                return;
            }

            if (Remote != null && Remote.Status == AdminStatus.Invalid)
            {
                Diffs.Add(ErrorRow("document", null, "Dokument", Remote.Message ?? "Uszkodzony JSON."));
                Signature = ComputeSignature();
                return;
            }

            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (!export.Selected)
                    continue;
                if (export.Support == SupportKind.DataError || export.Support == SupportKind.Unsupported || export.Support == SupportKind.Unavailable)
                {
                    Diffs.Add(ErrorRow("asset:" + export.Guid, export.Asset, export.Asset != null ? export.Asset.name : export.Guid, export.Reason));
                    continue;
                }

                JObject remoteEntry = null;
                bool exists = Remote != null && Remote.Document != null && M3pConfigDocument.TryGetEntry(Remote.Document, export.RemoteId, out remoteEntry);
                if (exists && (remoteEntry.Value<string>("typeId") != export.Contract.typeId ||
                               remoteEntry.Value<int>("typeSchemaVersion") != export.Contract.typeSchemaVersion))
                {
                    Diffs.Add(ErrorRow("type:" + export.RemoteId, export.Asset, export.Asset.name, "Nieobsługiwana konfiguracja."));
                    continue;
                }

                JObject remoteData = exists ? remoteEntry["data"] as JObject : null;
                if (export.Contract?.fields == null)
                    continue;

                for (int f = 0; f < export.Contract.fields.Count; f++)
                {
                    ConfigField field = export.Contract.fields[f];
                    JToken editorValue = export.Data[field.name];
                    JToken remoteValue = remoteData?[field.name];
                    bool missing = !exists || remoteValue == null;
                    bool different = missing || !ConfigValueCodec.ValuesEqual(editorValue, remoteValue, field);
                    bool invalidRemote = !missing && !ConfigValueCodec.TryNormalize(remoteValue, field, out _, out _);
                    if (!different && !invalidRemote)
                        continue;

                    var row = new DiffRow
                    {
                        Key = export.RemoteId + "|" + field.name,
                        RemoteId = export.RemoteId,
                        AssetName = export.Asset.name,
                        FieldName = field.name,
                        EditorText = ConfigValueCodec.Display(editorValue),
                        RemoteText = missing ? "brak" : ConfigValueCodec.Display(remoteValue),
                        IsNewObject = !exists,
                        IsError = invalidRemote,
                        Error = invalidRemote ? "Błędny typ albo uszkodzona wartość." : null,
                        Selected = !invalidRemote,
                        Locked = invalidRemote,
                        Asset = export.Asset,
                    };
                    if (previous.TryGetValue(row.Key, out bool selected))
                        row.Selected = invalidRemote ? false : selected;
                    Diffs.Add(row);
                }
            }

            Signature = ComputeSignature();
        }

        static void AddResource(string key, string label)
        {
            Diffs.Add(new DiffRow
            {
                Key = "resource:" + key,
                AssetName = "Remote Config",
                FieldName = label,
                EditorText = "utworzenie",
                RemoteText = "brak",
                IsResource = true,
                Locked = true,
                Selected = true,
            });
        }

        static DiffRow ErrorRow(string key, UnityEngine.Object asset, string name, string error)
        {
            return new DiffRow
            {
                Key = key,
                AssetName = name,
                FieldName = "—",
                EditorText = "—",
                RemoteText = "—",
                IsError = true,
                Locked = true,
                Selected = false,
                Error = error,
                Asset = asset,
            };
        }

        static bool TryBuildPayload(out JArray values, out JObject document, out string error)
        {
            values = null;
            document = null;
            error = null;
            if (Remote != null && Remote.Status == AdminStatus.Invalid)
            {
                error = "Uszkodzony JSON blokuje publikację.";
                return false;
            }

            document = Remote != null && Remote.Document != null
                ? (JObject)Remote.Document.DeepClone()
                : M3pConfigDocument.CreateEmpty(0);
            document["schemaVersion"] = M3pConfigDocument.SchemaVersion;
            JObject objects = M3pConfigDocument.Objects(document);
            var blocked = new HashSet<string>();
            for (int i = 0; i < Diffs.Count; i++)
            {
                if (Diffs[i].IsError && !string.IsNullOrEmpty(Diffs[i].RemoteId))
                    blocked.Add(Diffs[i].RemoteId);
            }

            var touched = new HashSet<string>();
            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (!export.Selected || blocked.Contains(export.RemoteId))
                    continue;

                bool any = false;
                bool all = true;
                if (export.Contract?.fields == null)
                    continue;

                for (int f = 0; f < export.Contract.fields.Count; f++)
                {
                    DiffRow row = FindDiff(export.RemoteId + "|" + export.Contract.fields[f].name);
                    bool want = row == null || (row.Selected && !row.IsError);
                    if (row == null)
                        continue;
                    if (row.Selected && !row.IsError)
                        any = true;
                    else
                        all = false;
                }

                if (!any)
                    continue;

                bool exists = M3pConfigDocument.TryGetEntry(document, export.RemoteId, out JObject entry);
                if (!exists && !all)
                {
                    error = "Nowy wpis " + export.Asset.name + " wymaga pełnego kontraktu.";
                    return false;
                }

                if (!exists)
                {
                    entry = new JObject
                    {
                        ["typeId"] = export.Contract.typeId,
                        ["typeSchemaVersion"] = export.Contract.typeSchemaVersion,
                        ["data"] = export.Data.DeepClone(),
                    };
                    objects[export.RemoteId] = entry;
                }
                else
                {
                    var data = entry["data"] as JObject ?? new JObject();
                    for (int f = 0; f < export.Contract.fields.Count; f++)
                    {
                        ConfigField field = export.Contract.fields[f];
                        DiffRow row = FindDiff(export.RemoteId + "|" + field.name);
                        if (row != null && row.Selected && !row.IsError)
                            data[field.name] = export.Data[field.name].DeepClone();
                    }

                    entry["data"] = data;
                    var errors = new List<string>();
                    BalanceConfigValidator.Validate(export.Contract.typeId, export.Contract, data, errors);
                    if (errors.Count > 0)
                    {
                        error = export.Asset.name + ": " + errors[0];
                        return false;
                    }
                }

                touched.Add(export.RemoteId);
            }

            if (touched.Count == 0 && !(Cloud != null && Cloud.NoEnvironment) && Remote != null && Remote.ConfigExists && Remote.KeyExists)
            {
                error = "Brak zmian do wysłania.";
                return false;
            }

            document["revision"] = (Remote != null && Remote.Document != null ? M3pConfigDocument.ReadRevision(Remote.Document) : 0) + 1;
            values = RemoteConfigAdminClient.ReplaceKey(Remote != null ? Remote.Values : null, document);
            string payload = values.ToString(Newtonsoft.Json.Formatting.None);
            int size = M3pConfigDocument.Utf8Size(payload);
            if (size > M3pConfigDocument.MaxPayloadBytes)
            {
                error = "Publikacja zablokowana. Rozmiar konfiguracji to " + size + " bajtów, limit API to " + M3pConfigDocument.MaxPayloadBytes + ".";
                return false;
            }

            return true;
        }

        static DiffRow FindDiff(string key)
        {
            for (int i = 0; i < Diffs.Count; i++)
            {
                if (Diffs[i].Key == key)
                    return Diffs[i];
            }

            return null;
        }

        static bool WriteSnapshot(JArray values, JObject document, out string error)
        {
            error = null;
            try
            {
                string environment = Cloud != null && Cloud.Target != null ? Cloud.Target.Id : "new-environment";
                string directory = Path.Combine("Library", "M3PRemoteConfig", "History", environment);
                Directory.CreateDirectory(directory);
                var snapshot = new JObject
                {
                    ["projectId"] = Cloud != null ? Cloud.ProjectId : string.Empty,
                    ["environmentId"] = environment,
                    ["createdLocal"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["previous"] = Remote != null && Remote.Document != null ? Remote.Document : JValue.CreateNull(),
                    ["target"] = document,
                    ["values"] = values,
                };
                string path = Path.Combine(directory, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.WriteAllText(path, snapshot.ToString(Newtonsoft.Json.Formatting.Indented));
                return true;
            }
            catch (Exception exception)
            {
                error = "Błąd zapisu snapshotu zatrzymał wysyłkę. " + exception.Message;
                return false;
            }
        }

        public static void ExportLatestSnapshot()
        {
            string environment = Cloud != null && Cloud.Target != null ? Cloud.Target.Id : null;
            if (string.IsNullOrEmpty(environment))
            {
                Message = "Brak środowiska, więc nie ma snapshotu.";
                Notify();
                return;
            }

            string directory = Path.Combine("Library", "M3PRemoteConfig", "History", environment);
            if (!Directory.Exists(directory))
            {
                Message = "Brak lokalnego snapshotu.";
                Notify();
                return;
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            if (files.Length == 0)
            {
                Message = "Brak lokalnego snapshotu.";
                Notify();
                return;
            }

            Array.Sort(files, StringComparer.Ordinal);
            string destination = EditorUtility.SaveFilePanel("Eksport snapshotu", "", Path.GetFileName(files[files.Length - 1]), "json");
            if (string.IsNullOrEmpty(destination))
                return;
            File.Copy(files[files.Length - 1], destination, true);
            Message = "Zapisano snapshot.";
            Notify();
        }

        static void WriteCatalog()
        {
            const string path = "Assets/_Project/Resources/M3P/RemoteConfigCatalog.asset";
            Directory.CreateDirectory("Assets/_Project/Resources/M3P");
            var catalog = AssetDatabase.LoadAssetAtPath<RemoteConfigCatalogAsset>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RemoteConfigCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            bool playFlag = catalog.useRemoteInEditorPlayMode;
            var entries = new List<RemoteConfigCatalogEntry>();
            var types = new List<string>();
            var signature = new StringBuilder();
            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (!export.Selected || export.Asset == null)
                    continue;
                if (export.Support != SupportKind.Supported && export.Support != SupportKind.Partial)
                    continue;

                entries.Add(new RemoteConfigCatalogEntry
                {
                    remoteId = export.RemoteId,
                    typeId = export.Contract.typeId,
                    typeSchemaVersion = export.Contract.typeSchemaVersion,
                    source = export.Asset,
                    contractJson = ConfigValueApplier.ToJson(export.Contract),
                });
                signature.Append(export.RemoteId).Append('|').Append(export.Contract.typeId).Append('|')
                    .Append(export.Contract.typeSchemaVersion).Append('|').Append(ConfigValueApplier.ToJson(export.Contract)).Append('\n');
                string typeName = export.Asset.GetType().FullName;
                if (!types.Contains(typeName))
                    types.Add(typeName);
            }

            string newSignature = M3pConfigDocument.Sha256(signature.ToString());
            string projectId = Cloud != null ? Cloud.ProjectId : Manifest.projectId;
            string environmentId = Cloud != null && Cloud.Target != null ? Cloud.Target.Id : Manifest.environmentId;
            string environmentName = Cloud != null && Cloud.Target != null ? Cloud.Target.Name : Manifest.environmentName;
            string link = BuildLink(types);
            string linkPath = "Assets/_Project/RemoteConfig/link.xml";
            if (!File.Exists(linkPath) || File.ReadAllText(linkPath) != link)
            {
                Directory.CreateDirectory("Assets/_Project/RemoteConfig");
                File.WriteAllText(linkPath, link);
            }

            if (catalog.signature == newSignature &&
                catalog.projectId == projectId &&
                catalog.environmentId == environmentId &&
                catalog.environmentName == environmentName &&
                catalog.entries != null &&
                catalog.entries.Length == entries.Count)
                return;

            catalog.entries = entries.ToArray();
            catalog.signature = newSignature;
            catalog.projectId = projectId;
            catalog.environmentId = environmentId;
            catalog.environmentName = environmentName;
            catalog.useRemoteInEditorPlayMode = playFlag;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        static string BuildLink(List<string> types)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<linker>");
            builder.AppendLine("  <assembly fullname=\"Assembly-CSharp\">");
            for (int i = 0; i < types.Count; i++)
                builder.AppendLine("    <type fullname=\"" + types[i] + "\" preserve=\"all\" />");
            builder.AppendLine("  </assembly>");
            builder.AppendLine("</linker>");
            return builder.ToString();
        }

        static bool SaveSelectedAssets(out string error)
        {
            error = null;
            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (!export.Selected || export.Asset == null || !EditorUtility.IsDirty(export.Asset))
                    continue;
                AssetDatabase.SaveAssetIfDirty(export.Asset);
                if (EditorUtility.IsDirty(export.Asset))
                {
                    error = "Nie zapisano " + export.Asset.name + ". Publikacja została zatrzymana.";
                    return false;
                }
            }

            return true;
        }

        static void RememberTarget()
        {
            if (Cloud == null)
                return;
            Manifest.projectId = Cloud.ProjectId;
            if (Cloud.Target == null)
                return;
            Manifest.environmentId = Cloud.Target.Id;
            Manifest.environmentName = Cloud.Target.Name;
            RemoteAssetManifest.Save(Manifest);
        }

        static bool CanPublishTarget(out string error)
        {
            error = null;
            if (Cloud == null || string.IsNullOrEmpty(Cloud.ProjectId))
            {
                error = Cloud != null && !string.IsNullOrEmpty(Cloud.Blocker) ? Cloud.Blocker : "Brak powiązanego projektu.";
                return false;
            }

            if (!string.IsNullOrEmpty(Cloud.Blocker))
            {
                error = Cloud.Blocker;
                return false;
            }

            if (Cloud.NoEnvironment)
            {
                if (string.IsNullOrWhiteSpace(Manifest.newEnvironmentName))
                {
                    error = "Podaj nazwę środowiska, które zostanie utworzone.";
                    return false;
                }

                return true;
            }

            if (Cloud.Target == null)
            {
                error = "Nie rozpoznano środowiska docelowego.";
                return false;
            }

            return true;
        }

        static string ComputeSignature()
        {
            var builder = new StringBuilder();
            builder.Append(Cloud != null ? Cloud.ProjectId : string.Empty).Append('|');
            builder.Append(Cloud != null && Cloud.Target != null ? Cloud.Target.Id : "none").Append('|');
            builder.Append(Cloud != null && Cloud.NoEnvironment ? Manifest.newEnvironmentName : string.Empty).Append('|');
            builder.Append(Remote != null && Remote.ConfigExists).Append('|').Append(Remote != null && Remote.KeyExists).Append('|');
            builder.Append(Remote != null && Remote.Document != null ? M3pConfigDocument.ContentHash(Remote.Document) : "missing").Append('|');
            for (int i = 0; i < Assets.Count; i++)
            {
                ExportResult export = Assets[i];
                if (!export.Selected || export.Data == null)
                    continue;
                builder.Append(export.RemoteId).Append('=').Append(M3pConfigDocument.Canonicalize(export.Data)).Append('\n');
            }

            return M3pConfigDocument.Sha256(builder.ToString());
        }

        static void MaybePopup()
        {
            if (SessionState.GetString(SnoozeKey, string.Empty) == Signature)
                return;
            if (!HasVisibleDifference())
                return;
            RemoteConfigDiffWindow.Open();
        }

        static bool HasSendableChange()
        {
            for (int i = 0; i < Diffs.Count; i++)
            {
                if (!Diffs[i].IsError && !Diffs[i].IsResource)
                    return true;
            }

            return Cloud != null && Cloud.NoEnvironment;
        }

        static bool HasVisibleDifference()
        {
            for (int i = 0; i < Diffs.Count; i++)
            {
                if (!Diffs[i].IsResource)
                    return true;
            }

            return false;
        }

        static void UpdateMessage()
        {
            if (!LastCheckOk)
            {
                string when = LastSuccessLocal.HasValue ? " Ostatni sukces: " + LastSuccessLocal.Value.ToString("yyyy-MM-dd HH:mm:ss") + "." : string.Empty;
                Message = "Nie udało się sprawdzić." + when + " " + (Remote != null ? Remote.Message : Cloud != null ? Cloud.Blocker : string.Empty);
                return;
            }

            int changes = 0;
            int configs = 0;
            var counted = new HashSet<string>();
            for (int i = 0; i < Diffs.Count; i++)
            {
                if (Diffs[i].IsResource || Diffs[i].IsError)
                    continue;
                changes++;
                if (counted.Add(Diffs[i].RemoteId))
                    configs++;
            }

            if (changes == 0 && (Cloud == null || !Cloud.NoEnvironment))
            {
                int selected = 0;
                int fields = 0;
                for (int i = 0; i < Assets.Count; i++)
                {
                    if (!Assets[i].Selected)
                        continue;
                    selected++;
                    fields += Assets[i].Contract != null && Assets[i].Contract.fields != null ? Assets[i].Contract.fields.Count : 0;
                }

                Message = "Zgodne: " + selected + " configów, " + fields + " pól.";
                return;
            }

            Message = "Różnice: " + configs + " configów, " + changes + " pól.";
        }

        static bool SameTarget(string environmentId)
        {
            return Cloud != null && Cloud.Target != null && Cloud.Target.Id == environmentId;
        }

        static ExportResult FindAsset(ScriptableObject asset)
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                if (Assets[i].Asset == asset)
                    return Assets[i];
            }

            return null;
        }

        static string ReadId(string body)
        {
            try
            {
                return JObject.Parse(body).Value<string>("id");
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string Append(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return right;
            return left + " " + right;
        }

        static void Notify()
        {
            Changed?.Invoke();
        }
    }
}
