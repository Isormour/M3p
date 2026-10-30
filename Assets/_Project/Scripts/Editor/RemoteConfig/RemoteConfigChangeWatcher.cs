using System;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    /// <summary>
    /// Watches selected configs only while the sync window is open.
    /// It does not subscribe at editor startup, because a project-wide scan during import locks Unity.
    /// </summary>
    static class RemoteConfigChangeWatcher
    {
        static bool _listening;
        static bool _pending;
        static double _due;
        static double _nextFocusRead;

        public static void SetListening(bool listening)
        {
            if (_listening == listening)
                return;

            _listening = listening;
            if (listening)
            {
                EditorApplication.update += Tick;
                Undo.postprocessModifications += OnUndo;
                ObjectChangeEvents.changesPublished += OnChanged;
                EditorApplication.focusChanged += OnFocus;
                return;
            }

            EditorApplication.update -= Tick;
            Undo.postprocessModifications -= OnUndo;
            ObjectChangeEvents.changesPublished -= OnChanged;
            EditorApplication.focusChanged -= OnFocus;
            _pending = false;
        }

        static void Tick()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (_pending && EditorApplication.timeSinceStartup >= _due)
            {
                _pending = false;
                RemoteConfigSyncController.RequestCheck(false, true);
            }

            bool windowOpen = Resources.FindObjectsOfTypeAll<RemoteConfigSyncWindow>().Length > 0;
            if (!windowOpen)
                return;

            if (!RemoteConfigSyncController.LastSuccessLocal.HasValue)
                return;

            double age = (DateTime.Now - RemoteConfigSyncController.LastSuccessLocal.Value).TotalSeconds;
            if (age >= 60d && EditorApplication.timeSinceStartup >= _nextFocusRead)
            {
                _nextFocusRead = EditorApplication.timeSinceStartup + 60d;
                RemoteConfigSyncController.RequestCheck(true, true);
            }
        }

        static void OnFocus(bool focused)
        {
            if (!focused || Application.isPlaying)
                return;
            if (RemoteConfigSyncController.LastSuccessLocal.HasValue &&
                (DateTime.Now - RemoteConfigSyncController.LastSuccessLocal.Value).TotalSeconds < 60d)
                return;
            if (EditorApplication.timeSinceStartup < _nextFocusRead)
                return;

            _nextFocusRead = EditorApplication.timeSinceStartup + 60d;
            RemoteConfigSyncController.RequestCheck(true, true);
        }

        static UndoPropertyModification[] OnUndo(UndoPropertyModification[] modifications)
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                if (modifications[i].currentValue.target is ScriptableObject asset && IsWatched(asset))
                    Schedule();
            }

            return modifications;
        }

        static void OnChanged(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeAssetObjectProperties)
                    continue;

                stream.GetChangeAssetObjectPropertiesEvent(i, out ChangeAssetObjectPropertiesEventArgs args);
                string guid = args.guid.ToString();
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("RemoteConfigCatalog", StringComparison.Ordinal) >= 0 ||
                    path.IndexOf("RemoteAssetManifest", StringComparison.Ordinal) >= 0 ||
                    path.EndsWith("link.xml", StringComparison.Ordinal))
                    continue;

                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && IsWatched(asset as ScriptableObject))
                    Schedule();
            }
        }

        static bool IsWatched(ScriptableObject asset)
        {
            if (asset == null)
                return false;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            var entry = RemoteAssetManifest.Find(RemoteConfigSyncController.Manifest, guid);
            return entry != null && entry.selected;
        }

        static void Schedule()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            _pending = true;
            _due = EditorApplication.timeSinceStartup + 2d;
        }
    }
}
