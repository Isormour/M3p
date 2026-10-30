using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    [Serializable]
    public sealed class RemoteAssetManifestEntry
    {
        public string guid;
        public string remoteId;
        public bool selected;
    }

    [Serializable]
    public sealed class RemoteAssetManifestData
    {
        public int schemaVersion = 1;
        public string projectId;
        public string environmentId;
        public string environmentName;
        public bool autoCheck = true;
        public string newEnvironmentName = "production";
        public RemoteAssetManifestEntry[] entries = Array.Empty<RemoteAssetManifestEntry>();
    }

    /// <summary>
    /// Versioned identity map. Rename and move keep the guid, so the remote id stays. A copy is a new guid.
    /// </summary>
    public static class RemoteAssetManifest
    {
        public const string AssetPath = "Assets/_Project/RemoteConfig/RemoteAssetManifest.json";

        public static RemoteAssetManifestData Load()
        {
            if (!File.Exists(AssetPath))
                return new RemoteAssetManifestData();

            try
            {
                RemoteAssetManifestData data = JsonUtility.FromJson<RemoteAssetManifestData>(File.ReadAllText(AssetPath));
                return data ?? new RemoteAssetManifestData();
            }
            catch (Exception)
            {
                return new RemoteAssetManifestData { schemaVersion = -1 };
            }
        }

        public static void Save(RemoteAssetManifestData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath) ?? "Assets/_Project/RemoteConfig");
            File.WriteAllText(AssetPath, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(AssetPath);
        }

        public static RemoteAssetManifestEntry Find(RemoteAssetManifestData data, string guid)
        {
            if (data?.entries == null || string.IsNullOrEmpty(guid))
                return null;

            for (int i = 0; i < data.entries.Length; i++)
            {
                if (data.entries[i] != null && data.entries[i].guid == guid)
                    return data.entries[i];
            }

            return null;
        }

        public static RemoteAssetManifestEntry EnsureIdentity(RemoteAssetManifestData data, string guid)
        {
            RemoteAssetManifestEntry existing = Find(data, guid);
            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.remoteId))
                    existing.remoteId = NewRemoteId();
                return existing;
            }

            var created = new RemoteAssetManifestEntry
            {
                guid = guid,
                remoteId = NewRemoteId(),
                selected = false,
            };
            var list = new List<RemoteAssetManifestEntry>();
            if (data.entries != null)
                list.AddRange(data.entries);
            list.Add(created);
            data.entries = list.ToArray();
            return created;
        }

        public static bool HasDuplicateRemoteIds(RemoteAssetManifestData data, out string remoteId)
        {
            remoteId = null;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (data?.entries == null)
                return false;

            for (int i = 0; i < data.entries.Length; i++)
            {
                string id = data.entries[i]?.remoteId;
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!seen.Add(id))
                {
                    remoteId = id;
                    return true;
                }
            }

            return false;
        }

        public static string NewRemoteId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
