#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace M3P.Editor
{
    /// <summary>Shows the actual texture data stored in the last WebGL player build.</summary>
    public sealed class BuildTextureWindow : EditorWindow
    {
        const string MenuPath = "M3P/WebGL/Textures in Last Build";
        static readonly string[] Sources = { "Wszystkie", "Assets", "Pakiety i Unity" };

        [SerializeField] TreeViewState<int> _treeState;
        [SerializeField] MultiColumnHeaderState _headerState;
        [SerializeField] string _search = "";
        [SerializeField] int _sourceFilter;

        BuildTextureSnapshot _snapshot;
        TextureTree _tree;
        SearchField _searchField;
        bool _refreshQueued;
        string _error;
        Vector2 _detailsScroll;
        readonly Dictionary<string, Object[]> _sourceObjects = new Dictionary<string, Object[]>();

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<BuildTextureWindow>("Build Textures");
            window.minSize = new Vector2(760, 440);
            window.Show();
        }

        void OnEnable()
        {
            _treeState ??= new TreeViewState<int>();
            var header = TextureTree.CreateHeader();
            if (_headerState != null && MultiColumnHeaderState.CanOverwriteSerializedFields(_headerState, header))
                MultiColumnHeaderState.OverwriteSerializedFields(_headerState, header);
            _headerState = header;
            _tree = new TextureTree(_treeState, new MultiColumnHeader(_headerState), this);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _tree.SetFocusAndEnsureSelectedItem;
            EditorApplication.projectChanged += HandleProjectChanged;
            QueueRefresh();
        }

        void OnDisable()
        {
            EditorApplication.delayCall -= Refresh;
            EditorApplication.projectChanged -= HandleProjectChanged;
            if (_searchField != null && _tree != null)
                _searchField.downOrUpArrowKeyPressed -= _tree.SetFocusAndEnsureSelectedItem;
            _refreshQueued = false;
        }

        void HandleProjectChanged()
        {
            _sourceObjects.Clear();
            Repaint();
        }

        void OnInspectorUpdate()
        {
            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }

        void QueueRefresh()
        {
            if (_refreshQueued) return;
            _refreshQueued = true;
            EditorApplication.delayCall += Refresh;
            Repaint();
        }

        void Refresh()
        {
            _refreshQueued = false;
            if (!this) return;
            try
            {
                EditorUtility.DisplayProgressBar("Tekstury ostatniego buildu", "Odczytywanie raportu i danych WebGL…", 0.15f);
                _snapshot = BuildTextureData.LoadLatest((progress, message) =>
                    EditorUtility.DisplayProgressBar("Tekstury ostatniego buildu", message, progress));
                _error = _snapshot?.error;
                _sourceObjects.Clear();
                _tree.SetEntries(_snapshot?.entries ?? new List<BuildTextureEntry>());
                ApplyFilter();
            }
            catch (Exception exception)
            {
                _snapshot = null;
                _tree.SetEntries(new List<BuildTextureEntry>());
                _error = exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        void OnGUI()
        {
            if (_tree == null) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(_refreshQueued || EditorApplication.isCompiling))
                {
                    if (GUILayout.Button("Odśwież raport", EditorStyles.toolbarButton, GUILayout.Width(106)))
                        QueueRefresh();
                }
                EditorGUI.BeginChangeCheck();
                _sourceFilter = EditorGUILayout.Popup(_sourceFilter, Sources, EditorStyles.toolbarPopup, GUILayout.Width(128));
                _search = _searchField.OnToolbarGUI(_search, GUILayout.MinWidth(150));
                if (EditorGUI.EndChangeCheck()) ApplyFilter();
                using (new EditorGUI.DisabledScope(_tree.VisibleCount == 0))
                {
                    if (GUILayout.Button("Wybierz widoczne", EditorStyles.toolbarButton, GUILayout.Width(124)))
                        _tree.SelectVisible();
                }
            }

            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Warning);
            if (_snapshot != null)
            {
                EditorGUILayout.LabelField($"Build: {_snapshot.buildTime}   •   {_snapshot.entries.Count} tekstur   •   {Megabytes(_snapshot.totalBytes)} MB", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Widoczne: {_tree.VisibleCount}   •   {Megabytes(_tree.VisibleBytes)} MB   •   Rozmiary przed kompresją Unity/ZIP. 1 MB = 1 000 000 B.", EditorStyles.miniLabel);
            }
            else if (_refreshQueued)
                EditorGUILayout.HelpBox("Odczytywanie ostatniego buildu…", MessageType.Info);
            else if (string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox("Brak danych. Wykonaj build WebGL i odśwież raport.", MessageType.Info);

            Rect tableRect = GUILayoutUtility.GetRect(0, 10000, 100, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _tree.OnGUI(tableRect);
            DrawDetails();
        }

        void ApplyFilter()
        {
            _tree.Filter(_search, _sourceFilter);
            Repaint();
        }

        void DrawDetails()
        {
            List<BuildTextureEntry> selected = _tree.SelectedEntries();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(146)))
            {
                if (selected.Count == 0)
                {
                    EditorGUILayout.LabelField("Wybierz teksturę, aby zobaczyć szczegóły i zaznaczyć jej asset.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Ctrl / Shift: wybór wielu. Dwuklik: pokaż asset w Project. Nagłówki kolumn: sortowanie.", EditorStyles.wordWrappedMiniLabel);
                    return;
                }
                if (selected.Count > 1)
                {
                    EditorGUILayout.LabelField($"Zaznaczone: {selected.Count}   •   {Megabytes(selected.Sum(entry => entry.packedBytes))} MB", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Dostępne assety zostały zaznaczone razem w Project. Możesz zmieniać ich ustawienia w Inspectorze.", EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("Zaznacz assety w Project", GUILayout.Width(180))) SelectAssets(selected, true);
                    return;
                }

                BuildTextureEntry entry = selected[0];
                Object source = ResolveAsset(entry);
                using (new EditorGUILayout.HorizontalScope())
                {
                    Texture preview = source ? AssetPreview.GetAssetPreview(source) ?? AssetPreview.GetMiniThumbnail(source) : null;
                    Rect previewRect = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96), GUILayout.Height(96));
                    if (preview) GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(entry.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"W buildzie: {Megabytes(entry.packedBytes)} MB   |   {Resolution(entry)}   |   {entry.format}   |   Mipmapy: {entry.mipCount}", EditorStyles.wordWrappedLabel);
                        if (!string.IsNullOrEmpty(entry.dimensionError))
                            EditorGUILayout.LabelField(entry.dimensionError, EditorStyles.wordWrappedMiniLabel);
                        else if (entry.width == 0 || entry.height == 0)
                            EditorGUILayout.LabelField("Pusty atlas fontu; jest wypełniany podczas działania gry.", EditorStyles.miniLabel);
                        _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll, GUILayout.Height(38));
                        EditorGUILayout.SelectableLabel(entry.assetPath ?? "(tekstura wygenerowana)", EditorStyles.wordWrappedLabel, GUILayout.MinHeight(32));
                        EditorGUILayout.EndScrollView();
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(!source))
                            {
                                if (GUILayout.Button("Pokaż w Project", GUILayout.Width(126))) SelectAssets(selected, true);
                            }
                            if (GUILayout.Button("Kopiuj ścieżkę", GUILayout.Width(120)))
                                EditorGUIUtility.systemCopyBuffer = entry.assetPath ?? "";
                            if (!source) GUILayout.Label("Brak dostępnego assetu źródłowego", EditorStyles.miniLabel);
                        }
                    }
                }
                EditorGUILayout.LabelField("Podgląd przedstawia aktualny asset. Dane tabeli pochodzą z ostatniego buildu.", EditorStyles.miniLabel);
            }
        }

        Object ResolveAsset(BuildTextureEntry entry)
        {
            if (string.IsNullOrEmpty(entry.assetPath)
                || !(entry.assetPath.StartsWith("Assets/", StringComparison.Ordinal) || entry.assetPath.StartsWith("Packages/", StringComparison.Ordinal)))
                return null;
            if (!_sourceObjects.TryGetValue(entry.assetPath, out Object[] assets))
            {
                assets = AssetDatabase.LoadAllAssetsAtPath(entry.assetPath);
                _sourceObjects[entry.assetPath] = assets;
            }
            Object texture = assets.FirstOrDefault(asset => asset is Texture && asset.name == entry.name);
            if (texture) return texture;
            return AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
        }

        internal void SelectAssets(List<BuildTextureEntry> entries, bool ping)
        {
            Object[] assets = entries.Select(ResolveAsset).Where(asset => asset).Distinct().ToArray();
            if (assets.Length == 0) return;
            Selection.objects = assets;
            if (ping) EditorGUIUtility.PingObject(assets[0]);
            Repaint();
        }

        static string Megabytes(long bytes) => (bytes / 1000000d).ToString("N4", CultureInfo.CurrentCulture);
        static string Resolution(BuildTextureEntry entry) => entry.width < 0 || entry.height < 0 || !string.IsNullOrEmpty(entry.dimensionError)
            ? "brak danych"
            : $"{entry.width} × {entry.height}" + (entry.type == "Cubemap" ? " × 6 ścian" : "");

        sealed class TextureTree : TreeView<int>
        {
            readonly BuildTextureWindow _owner;
            List<TextureItem> _items = new List<TextureItem>();
            List<TextureItem> _visible = new List<TextureItem>();
            string _query = "";
            int _source;

            internal int VisibleCount => _visible.Count;
            internal long VisibleBytes => _visible.Sum(item => item.Entry.packedBytes);

            internal TextureTree(TreeViewState<int> state, MultiColumnHeader header, BuildTextureWindow owner) : base(state, header)
            {
                _owner = owner;
                rowHeight = 24;
                showAlternatingRowBackgrounds = true;
                showBorder = true;
                columnIndexForTreeFoldouts = 0;
                header.sortingChanged += _ => Reload();
                Reload();
            }

            internal static MultiColumnHeaderState CreateHeader()
            {
                var columns = new[]
                {
                    Column("Tekstura", 250, 130), Column("Build (MB)", 100, 80),
                    Column("Rozdzielczość", 135, 110), Column("Format", 90, 70),
                    Column("Mipmapy", 70, 60), Column("Źródło", 90, 70), Column("Ścieżka assetu", 450, 160)
                };
                var state = new MultiColumnHeaderState(columns) { sortedColumnIndex = 1 };
                columns[1].sortedAscending = false;
                return state;
            }

            static MultiColumnHeaderState.Column Column(string title, float width, float minWidth) => new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(title), width = width, minWidth = minWidth,
                autoResize = false, allowToggleVisibility = false, canSort = true,
                headerTextAlignment = TextAlignment.Left
            };

            internal void SetEntries(List<BuildTextureEntry> entries)
            {
                _items = entries.Select((entry, index) => new TextureItem(index + 1, entry)).ToList();
                SetSelection(Array.Empty<int>());
                Reload();
            }

            internal void Filter(string query, int source)
            {
                _query = query ?? "";
                _source = source;
                Reload();
                var visibleIds = new HashSet<int>(_visible.Select(item => item.id));
                SetSelection(GetSelection().Where(visibleIds.Contains).ToList());
            }

            protected override TreeViewItem<int> BuildRoot()
            {
                var root = new TreeViewItem<int>(0, -1, "Root");
                _visible = _items.Where(item => Matches(item.Entry)).ToList();
                int column = multiColumnHeader.sortedColumnIndex;
                bool ascending = column < 0 || multiColumnHeader.IsSortedAscending(column);
                _visible.Sort((a, b) =>
                {
                    int result = Compare(a.Entry, b.Entry, column);
                    if (!ascending) result = -result;
                    return result != 0 ? result : a.id.CompareTo(b.id);
                });
                root.children = _visible.Cast<TreeViewItem<int>>().ToList();
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            bool Matches(BuildTextureEntry entry)
            {
                bool projectAsset = entry.assetPath != null && entry.assetPath.StartsWith("Assets/", StringComparison.Ordinal);
                if ((_source == 1 && !projectAsset) || (_source == 2 && projectAsset)) return false;
                return string.IsNullOrWhiteSpace(_query)
                    || Contains(entry.name, _query) || Contains(entry.assetPath, _query) || Contains(entry.format, _query);
            }

            static bool Contains(string text, string query) => text?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

            static int Compare(BuildTextureEntry a, BuildTextureEntry b, int column)
            {
                switch (column)
                {
                    case 1: return a.packedBytes.CompareTo(b.packedBytes);
                    case 2: return ((long)a.width * a.height).CompareTo((long)b.width * b.height);
                    case 3: return string.Compare(a.format, b.format, StringComparison.OrdinalIgnoreCase);
                    case 4: return a.mipCount.CompareTo(b.mipCount);
                    case 5: return string.Compare(a.sourceKind, b.sourceKind, StringComparison.OrdinalIgnoreCase);
                    case 6: return string.Compare(a.assetPath, b.assetPath, StringComparison.OrdinalIgnoreCase);
                    default: return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                }
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (TextureItem)args.item;
                BuildTextureEntry entry = item.Entry;
                for (int visibleColumn = 0; visibleColumn < args.GetNumVisibleColumns(); visibleColumn++)
                {
                    int column = args.GetColumn(visibleColumn);
                    Rect rect = args.GetCellRect(visibleColumn);
                    CenterRectUsingSingleLineHeight(ref rect);
                    rect.xMin += 4;
                    string text;
                    switch (column)
                    {
                        case 0:
                            Texture icon = AssetDatabase.GetCachedIcon(entry.assetPath ?? "");
                            if (icon) GUI.DrawTexture(new Rect(rect.x, rect.y, 18, 18), icon, ScaleMode.ScaleToFit);
                            rect.xMin += 22;
                            text = entry.name;
                            break;
                        case 1: text = Megabytes(entry.packedBytes); break;
                        case 2: text = Resolution(entry); break;
                        case 3: text = entry.format; break;
                        case 4: text = entry.mipCount.ToString(); break;
                        case 5: text = entry.sourceKind; break;
                        default: text = entry.assetPath; break;
                    }
                    GUI.Label(rect, new GUIContent(text ?? "", text ?? ""));
                }
            }

            internal List<BuildTextureEntry> SelectedEntries()
            {
                var ids = new HashSet<int>(GetSelection());
                return _visible.Where(item => ids.Contains(item.id)).Select(item => item.Entry).ToList();
            }

            internal void SelectVisible()
            {
                SetSelection(_visible.Select(item => item.id).ToList());
                _owner.SelectAssets(SelectedEntries(), true);
            }

            protected override void SelectionChanged(IList<int> selectedIds) => _owner.SelectAssets(SelectedEntries(), false);
            protected override bool CanMultiSelect(TreeViewItem<int> item) => true;

            protected override void DoubleClickedItem(int id)
            {
                TextureItem item = _visible.FirstOrDefault(candidate => candidate.id == id);
                if (item != null) _owner.SelectAssets(new List<BuildTextureEntry> { item.Entry }, true);
            }

            protected override void ContextClickedItem(int id)
            {
                TextureItem item = _visible.FirstOrDefault(candidate => candidate.id == id);
                if (item == null) return;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Pokaż asset w Project"), false, () => _owner.SelectAssets(new List<BuildTextureEntry> { item.Entry }, true));
                menu.AddItem(new GUIContent("Kopiuj ścieżkę"), false, () => EditorGUIUtility.systemCopyBuffer = item.Entry.assetPath ?? "");
                menu.ShowAsContext();
            }

            sealed class TextureItem : TreeViewItem<int>
            {
                internal readonly BuildTextureEntry Entry;
                internal TextureItem(int id, BuildTextureEntry entry) : base(id, 0, entry.name) { Entry = entry; }
            }
        }
    }
}
#endif
