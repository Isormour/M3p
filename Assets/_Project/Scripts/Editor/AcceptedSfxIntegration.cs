using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    /// <summary>Explicit, local editor import and verification; ordinary reloads do nothing.</summary>
    [InitializeOnLoad]
    public static class AcceptedSfxIntegration
    {
        const string RequestPath = "Library/AcceptedSfxIntegration.request";
        const string ManifestPath = "Assets/_Project/Audio/AcceptedSfxManifest.json";
        const string LibraryPath = "Assets/_Project/Audio/SfxLibrary.asset";
        const string ReportPath = "Docs/AudioGeneration/2026-11-20/accepted-set-01/unity_integration_report.json";
        static double _nextCheck;

        [Serializable] sealed class Manifest
        {
            public int acceptedCount;
            public string integrationReportPath;
            public Entry[] entries;
        }
        [Serializable] sealed class Request
        {
            public string reportPath;
            public int expectedAccepted;
        }
        [Serializable] sealed class Entry { public string id; public string assetPath; public string directory; }
        [Serializable] sealed class ClipReport
        {
            public string id;
            public string assetPath;
            public string guid;
            public float duration;
            public int frequency;
            public int channels;
            public bool referenced;
        }
        [Serializable] sealed class Report
        {
            public string request;
            public string checkedAt;
            public string unityVersion;
            public bool success;
            public string error;
            public int acceptedClips;
            public int expectedAccepted;
            public int generalEntries;
            public int skillEntries;
            public int preservedBindings;
            public int linkedGameConfigs;
            public ClipReport[] clips;
        }

        static AcceptedSfxIntegration() => EditorApplication.update += CheckRequest;

        static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.timeSinceStartup < _nextCheck)
                return;
            _nextCheck = EditorApplication.timeSinceStartup + 0.5;
            if (!File.Exists(RequestPath))
                return;
            string request = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            Run(request);
        }

        [MenuItem("M3P/Audio/Import and verify accepted sounds")]
        public static void RunFromMenu() => Run("menu");

        public static void Run(string request)
        {
            string reportPath = ReportPath;
            var report = new Report
            {
                request = request, checkedAt = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
            };
            try
            {
                var options = request.StartsWith("{", StringComparison.Ordinal)
                    ? JsonUtility.FromJson<Request>(request) : null;
                if (!string.IsNullOrEmpty(options?.reportPath))
                    reportPath = ResolveReportPath(options.reportPath);
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Import accepted sounds in Edit Mode.");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
                if (string.IsNullOrEmpty(options?.reportPath) && !string.IsNullOrEmpty(manifest?.integrationReportPath))
                    reportPath = ResolveReportPath(manifest.integrationReportPath);
                if (manifest?.entries == null || manifest.acceptedCount <= 0
                    || manifest.entries.Length != manifest.acceptedCount)
                    throw new InvalidOperationException("Accepted clip count does not match its manifest.");
                report.expectedAccepted = manifest.acceptedCount;
                if (options != null && options.expectedAccepted > 0 && options.expectedAccepted != manifest.acceptedCount)
                    throw new InvalidOperationException("Request and manifest accepted counts differ.");
                var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
                var previous = SnapshotBindings(library);
                var clips = new List<ClipReport>();
                foreach (var entry in manifest.entries)
                {
                    AssetDatabase.ImportAsset(entry.assetPath, ImportAssetOptions.ForceSynchronousImport);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.assetPath);
                    if (!clip || clip.samples <= 0 || clip.frequency <= 0 || clip.channels <= 0)
                        throw new InvalidOperationException("Audio import failed: " + entry.id);
                    clips.Add(new ClipReport { id = entry.id, assetPath = entry.assetPath,
                        guid = AssetDatabase.AssetPathToGUID(entry.assetPath),
                        duration = clip.length, frequency = clip.frequency, channels = clip.channels });
                }
                SfxLibraryBuilder.Build();
                library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
                var current = SnapshotBindings(library);
                foreach (var binding in previous)
                {
                    if (!current.TryGetValue(binding.Key, out string guid) || guid != binding.Value)
                        throw new InvalidOperationException("An existing binding changed: " + binding.Key);
                    report.preservedBindings++;
                }
                var serialized = new SerializedObject(library);
                var paths = new HashSet<string>(StringComparer.Ordinal);
                var general = serialized.FindProperty("_clips");
                var skills = serialized.FindProperty("_skills");
                report.generalEntries = general.arraySize;
                report.skillEntries = skills.arraySize;
                for (int i = 0; i < general.arraySize; i++)
                    AddReference(paths, general.GetArrayElementAtIndex(i).FindPropertyRelative("Clip"));
                for (int i = 0; i < skills.arraySize; i++)
                {
                    var skill = skills.GetArrayElementAtIndex(i);
                    AddReference(paths, skill.FindPropertyRelative("Layer"));
                    AddReference(paths, skill.FindPropertyRelative("Accent"));
                }
                foreach (var clip in clips)
                {
                    clip.referenced = paths.Contains(clip.assetPath);
                    if (!clip.referenced)
                        throw new InvalidOperationException("Accepted clip is not wired: " + clip.id);
                }
                foreach (string guid in AssetDatabase.FindAssets("t:GameConfig"))
                {
                    var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (config && new SerializedObject(config).FindProperty("_sfx").objectReferenceValue == library)
                        report.linkedGameConfigs++;
                }
                if (report.linkedGameConfigs == 0)
                    throw new InvalidOperationException("No GameConfig references the sound library.");
                report.clips = clips.ToArray();
                report.acceptedClips = clips.Count;
                report.success = true;
            }
            catch (Exception exception) { report.error = exception.ToString(); }
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            if (report.success)
                Debug.Log("Accepted SFX integration verified: " + report.acceptedClips + " clips, "
                    + report.generalEntries + " events, " + report.skillEntries + " skills.");
            else Debug.LogError("Accepted SFX integration failed: " + report.error);
        }

        static string ResolveReportPath(string path)
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string reports = Path.GetFullPath(Path.Combine(project, "Docs/AudioGeneration")) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(project, path));
            if (!full.StartsWith(reports, StringComparison.OrdinalIgnoreCase)
                || !Path.GetExtension(full).Equals(".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Accepted SFX report must be JSON inside Docs/AudioGeneration.");
            return full;
        }

        static void AddReference(HashSet<string> paths, SerializedProperty property)
        {
            if (property.objectReferenceValue)
                paths.Add(AssetDatabase.GetAssetPath(property.objectReferenceValue));
        }

        static Dictionary<string, string> SnapshotBindings(SfxLibrary library)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!library) return result;
            var serialized = new SerializedObject(library);
            var clips = serialized.FindProperty("_clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                var entry = clips.GetArrayElementAtIndex(i);
                AddSnapshot(result, "event:" + entry.FindPropertyRelative("Id").intValue,
                    entry.FindPropertyRelative("Clip"));
                var alternatives = entry.FindPropertyRelative("Alternates");
                if (alternatives != null)
                    for (int j = 0; j < alternatives.arraySize; j++)
                        AddSnapshot(result, "alternate:" + entry.FindPropertyRelative("Id").intValue + ":" + j,
                            alternatives.GetArrayElementAtIndex(j));
            }
            var skills = serialized.FindProperty("_skills");
            for (int i = 0; i < skills.arraySize; i++)
            {
                var entry = skills.GetArrayElementAtIndex(i);
                AddSnapshot(result, "accent:" + entry.FindPropertyRelative("SkillAssetName").stringValue,
                    entry.FindPropertyRelative("Accent"));
            }
            return result;
        }

        static void AddSnapshot(Dictionary<string, string> result, string key, SerializedProperty property)
        {
            if (property.objectReferenceValue)
                result[key] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.objectReferenceValue));
        }
    }
}
