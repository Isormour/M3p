using System;
using System.Collections.Generic;
using M3P.RemoteConfig;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    public sealed class RemoteConfigSyncWindow : EditorWindow
    {
        const string MenuPath = "M3P/Remote Config/Synchronizacja";
        const string PlayMenu = "M3P/Remote Config/Testuj Remote Config";

        string _search = string.Empty;
        string _typeFilter = "Wszystkie";
        Vector2 _listScroll;
        Vector2 _diffScroll;
        bool _details;
        bool _dropHover;
        int _inspect;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<RemoteConfigSyncWindow>("M3P Remote Config");
            window.minSize = new Vector2(720f, 480f);
            window.Show();
        }

        [MenuItem(PlayMenu)]
        static void ToggleRemotePlayMode()
        {
            RemoteConfigCatalogAsset catalog = LoadOrCreateCatalog();
            catalog.useRemoteInEditorPlayMode = !catalog.useRemoteInEditorPlayMode;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        [MenuItem(PlayMenu, true)]
        static bool ToggleRemotePlayModeValidate()
        {
            RemoteConfigCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<RemoteConfigCatalogAsset>(
                "Assets/_Project/Resources/M3P/RemoteConfigCatalog.asset");
            Menu.SetChecked(PlayMenu, catalog != null && catalog.useRemoteInEditorPlayMode);
            return true;
        }

        void OnEnable()
        {
            RemoteConfigSyncController.Changed += Repaint;
            RemoteConfigSyncController.LoadManifest();
            RemoteConfigSyncController.LoadLocalList();
            RemoteConfigChangeWatcher.SetListening(true);
        }

        void OnDisable()
        {
            RemoteConfigSyncController.Changed -= Repaint;
            RemoteConfigChangeWatcher.SetListening(false);
        }

        void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawList();
            DrawDiff();
            DrawFooter();
            HandleDrop();
        }

        void DrawHeader()
        {
            CloudSession cloud = RemoteConfigSyncController.Cloud;
            string project = "—";
            if (cloud != null)
                project = !string.IsNullOrEmpty(cloud.ProjectName) ? cloud.ProjectName : cloud.ProjectId;
            string environment = cloud != null && cloud.Target != null ? cloud.Target.Name : cloud != null && cloud.NoEnvironment ? "brak — zostanie utworzone" : "—";
            EditorGUILayout.LabelField("Projekt: " + project + "    Środowisko: " + environment, EditorStyles.boldLabel);

            if (cloud != null && cloud.NoEnvironment)
            {
                string name = RemoteConfigSyncController.Manifest.newEnvironmentName ?? string.Empty;
                string edited = EditorGUILayout.TextField("Nazwa nowego środowiska", name);
                if (edited != name)
                    RemoteConfigSyncController.SetNewEnvironmentName(edited);
            }
            else if (cloud != null && cloud.Environments.Count > 0)
            {
                var labels = new string[cloud.Environments.Count];
                int current = 0;
                for (int i = 0; i < cloud.Environments.Count; i++)
                {
                    labels[i] = cloud.Environments[i].Name;
                    if (cloud.Target != null && cloud.Environments[i].Id == cloud.Target.Id)
                        current = i;
                }

                int next = EditorGUILayout.Popup("Cel publikacji", current, labels);
                if (next != current)
                    RemoteConfigSyncController.SetTarget(cloud.Environments[next].Id);
            }

            if (cloud != null && !string.IsNullOrEmpty(cloud.Blocker))
                EditorGUILayout.HelpBox(cloud.Blocker, MessageType.Warning);
            if (!string.IsNullOrEmpty(RemoteConfigSyncController.Message))
                EditorGUILayout.HelpBox(RemoteConfigSyncController.Message, MessageType.Info);

            _details = EditorGUILayout.Foldout(_details, "Szczegóły połączenia");
            if (_details && cloud != null)
            {
                EditorGUILayout.LabelField("Id projektu", cloud.ProjectId);
                EditorGUILayout.LabelField("Organizacja", cloud.OrganizationId);
                EditorGUILayout.LabelField("Konto", string.IsNullOrEmpty(cloud.UserName) ? "—" : cloud.UserName);
                EditorGUILayout.LabelField("Środowisko edytora", string.IsNullOrEmpty(cloud.ActiveEditorEnvironment) ? "—" : cloud.ActiveEditorEnvironment);
                EditorGUILayout.LabelField("Id środowiska", cloud.Target != null ? cloud.Target.Id : "—");
                EditorGUILayout.LabelField("Klucz", M3pConfigDocument.Key);
                EditorGUILayout.LabelField("Id konfiguracji", RemoteConfigSyncController.Remote != null ? RemoteConfigSyncController.Remote.ConfigId : "—");
                string read = RemoteConfigSyncController.LastSuccessLocal.HasValue
                    ? RemoteConfigSyncController.LastSuccessLocal.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : "brak";
                EditorGUILayout.LabelField("Ostatni udany odczyt", read);
                EditorGUILayout.LabelField("Reguły", RemoteConfigSyncController.Remote != null ? RemoteConfigSyncController.Remote.RulesNote : "Reguły nie zostały sprawdzone.");
                if (GUILayout.Button("Otwórz logowanie Unity", GUILayout.Width(200f)))
                    SettingsService.OpenProjectSettings("Project/Services");
            }
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _search = EditorGUILayout.TextField("Szukaj", _search);
            if (GUILayout.Button("Dodaj zaznaczone w Project", GUILayout.Width(220f)))
                RemoteConfigSyncController.AddSelectionFromProject();
            EditorGUILayout.EndHorizontal();

            var types = new List<string> { "Wszystkie" };
            List<ExportResult> assets = RemoteConfigSyncController.Assets;
            for (int i = 0; i < assets.Count; i++)
            {
                string typeName = assets[i].Asset != null ? assets[i].Asset.GetType().Name : "—";
                if (!types.Contains(typeName))
                    types.Add(typeName);
            }

            int typeIndex = Math.Max(0, types.IndexOf(_typeFilter));
            int nextType = EditorGUILayout.Popup("Typ", typeIndex, types.ToArray());
            _typeFilter = types[nextType];

            bool auto = EditorGUILayout.ToggleLeft(
                "Automatycznie sprawdzaj różnice",
                RemoteConfigSyncController.Manifest.autoCheck);
            if (auto != RemoteConfigSyncController.Manifest.autoCheck)
                RemoteConfigSyncController.SetAutoCheck(auto);
        }

        void DrawList()
        {
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MinHeight(180f));
            List<ExportResult> assets = RemoteConfigSyncController.Assets;
            for (int i = 0; i < assets.Count; i++)
            {
                ExportResult export = assets[i];
                if (!PassesFilter(export))
                    continue;

                EditorGUILayout.BeginHorizontal();
                bool canSelect = export.Support == SupportKind.Supported || export.Support == SupportKind.Partial;
                EditorGUI.BeginDisabledGroup(!canSelect || RemoteConfigSyncController.Busy);
                bool selected = EditorGUILayout.Toggle(export.Selected, GUILayout.Width(18f));
                EditorGUI.EndDisabledGroup();
                if (selected != export.Selected && canSelect)
                    RemoteConfigSyncController.SetSelected(export, selected);

                if (GUILayout.Button(export.Asset != null ? export.Asset.name : "(brak)", EditorStyles.label, GUILayout.Width(220f)))
                {
                    _inspect = i;
                    EditorGUIUtility.PingObject(export.Asset);
                    Selection.activeObject = export.Asset;
                }

                EditorGUILayout.LabelField(SupportLabel(export), GUILayout.Width(180f));
                EditorGUILayout.LabelField(SyncLabel(export));
                if (export.Related.Count > 0 && GUILayout.Button("Pokaż powiązane assety", GUILayout.Width(170f)))
                    RemoteConfigSyncController.SelectRelated(export);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            if (_inspect >= 0 && _inspect < assets.Count && assets[_inspect].Omitted.Count > 0)
                EditorGUILayout.HelpBox("Pomijane pola: " + string.Join(", ", assets[_inspect].Omitted), MessageType.None);
        }

        void DrawDiff()
        {
            EditorGUILayout.LabelField("Różnice", EditorStyles.boldLabel);
            _diffScroll = EditorGUILayout.BeginScrollView(_diffScroll, GUILayout.MinHeight(140f));
            List<DiffRow> rows = RemoteConfigSyncController.Diffs;
            for (int i = 0; i < rows.Count; i++)
                DrawDiffRow(rows[i]);
            if (rows.Count == 0)
                EditorGUILayout.LabelField("Brak różnic w bieżącym porównaniu.");
            EditorGUILayout.EndScrollView();
        }

        internal static void DrawDiffRow(DiffRow row)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(row.Locked || RemoteConfigSyncController.Busy);
            row.Selected = EditorGUILayout.Toggle(row.Selected, GUILayout.Width(18f));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(row.AssetName ?? "—", EditorStyles.label, GUILayout.Width(180f)) && row.Asset != null)
            {
                EditorGUIUtility.PingObject(row.Asset);
                Selection.activeObject = row.Asset;
            }

            EditorGUILayout.LabelField(row.FieldName, GUILayout.Width(180f));
            EditorGUILayout.LabelField(row.EditorText, GUILayout.Width(120f));
            EditorGUILayout.LabelField(row.RemoteText, GUILayout.Width(120f));
            if (row.IsError)
                EditorGUILayout.LabelField(row.Error);
            EditorGUILayout.EndHorizontal();
        }

        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(RemoteConfigSyncController.Busy);
            if (GUILayout.Button("Sprawdź teraz"))
                RemoteConfigSyncController.RequestCheck(true, false);
            if (GUILayout.Button(RemoteConfigSyncController.SendButtonLabel()))
                RemoteConfigSyncController.RequestPublish(RemoteConfigSyncController.NeedsSave());
            if (GUILayout.Button("Eksportuj snapshot"))
                RemoteConfigSyncController.ExportLatestSnapshot();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            Rect drop = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
            GUI.Box(drop, "Przeciągnij assety ScriptableObject");
            _dropHover = drop.Contains(Event.current.mousePosition);
        }

        void HandleDrop()
        {
            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
                return;
            if (!_dropHover)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (DragAndDrop.objectReferences[i] is ScriptableObject asset)
                    {
                        Selection.objects = new UnityEngine.Object[] { asset };
                        RemoteConfigSyncController.AddSelectionFromProject();
                    }
                }
            }

            current.Use();
        }

        bool PassesFilter(ExportResult export)
        {
            string name = export.Asset != null ? export.Asset.name : string.Empty;
            string typeName = export.Asset != null ? export.Asset.GetType().Name : string.Empty;
            if (_typeFilter != "Wszystkie" && typeName != _typeFilter)
                return false;
            if (string.IsNullOrEmpty(_search))
                return true;
            return name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string SupportLabel(ExportResult export)
        {
            switch (export.Support)
            {
                case SupportKind.Supported: return "Obsługiwany";
                case SupportKind.Partial: return "Częściowo obsługiwany";
                case SupportKind.DataError: return "Błąd danych";
                case SupportKind.Unavailable: return "Niedostępny — " + export.Reason;
                default: return "Nieobsługiwany — " + export.Reason;
            }
        }

        static string SyncLabel(ExportResult export)
        {
            if (!export.Selected)
                return export.Support == SupportKind.Supported || export.Support == SupportKind.Partial ? "Poza zakresem" : export.Reason;
            if (!RemoteConfigSyncController.LastCheckOk)
                return "Nie udało się sprawdzić";
            if (export.Support == SupportKind.DataError)
                return "Błąd danych";

            int changes = 0;
            bool created = false;
            List<DiffRow> rows = RemoteConfigSyncController.Diffs;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].RemoteId != export.RemoteId || rows[i].IsResource)
                    continue;
                changes++;
                created |= rows[i].IsNewObject;
            }

            if (changes == 0)
                return "Zgodne";
            if (created)
                return "Nowy";
            return changes == 1 ? "1 różnica" : changes + " różnic";
        }

        static RemoteConfigCatalogAsset LoadOrCreateCatalog()
        {
            const string path = "Assets/_Project/Resources/M3P/RemoteConfigCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<RemoteConfigCatalogAsset>(path);
            if (catalog != null)
                return catalog;

            System.IO.Directory.CreateDirectory("Assets/_Project/Resources/M3P");
            catalog = ScriptableObject.CreateInstance<RemoteConfigCatalogAsset>();
            AssetDatabase.CreateAsset(catalog, path);
            return catalog;
        }
    }

    public sealed class RemoteConfigDiffWindow : EditorWindow
    {
        Vector2 _scroll;

        public static void Open()
        {
            var window = GetWindow<RemoteConfigDiffWindow>(true, "Wykryto różnice w konfiguracji", true);
            window.minSize = new Vector2(640f, 320f);
            window.Show();
        }

        void OnEnable()
        {
            RemoteConfigSyncController.Changed += Repaint;
        }

        void OnDisable()
        {
            RemoteConfigSyncController.Changed -= Repaint;
        }

        void OnGUI()
        {
            CloudSession cloud = RemoteConfigSyncController.Cloud;
            EditorGUILayout.LabelField("Wykryto różnice w konfiguracji", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Projekt: " + (cloud != null ? cloud.ProjectName : "—"));
            EditorGUILayout.LabelField("Środowisko: " + (cloud != null && cloud.Target != null ? cloud.Target.Name : "—"));
            EditorGUILayout.LabelField("Kierunek: Edytor → Remote Config");
            EditorGUILayout.LabelField(RemoteConfigSyncController.Message);
            if (RemoteConfigSyncController.NeedsSave())
                EditorGUILayout.HelpBox("Część źródeł ma niezapisane zmiany.", MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            List<DiffRow> rows = RemoteConfigSyncController.Diffs;
            for (int i = 0; i < rows.Count; i++)
                RemoteConfigSyncWindow.DrawDiffRow(rows[i]);
            EditorGUILayout.EndScrollView();

            string environment = cloud != null && cloud.Target != null ? cloud.Target.Name : "wybranym środowisku";
            EditorGUILayout.LabelField("Wybrane wartości zastąpią ustawienia w środowisku " + environment + ".");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(RemoteConfigSyncController.Busy);
            if (GUILayout.Button(RemoteConfigSyncController.SendButtonLabel()))
                RemoteConfigSyncController.RequestPublish(RemoteConfigSyncController.NeedsSave());
            if (GUILayout.Button("Później"))
            {
                RemoteConfigSyncController.Snooze();
                Close();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
    }
}
