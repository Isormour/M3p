using System;
using UnityEngine;

namespace M3P.RemoteConfig
{
    [Serializable]
    public sealed class RemoteConfigCatalogEntry
    {
        public string remoteId;
        public string typeId;
        public int typeSchemaVersion = 1;
        public ScriptableObject source;
        public string contractJson;
    }

    /// <summary>
    /// Build-time map from remote id to the asset the player already has. Unknown remote ids are ignored.
    /// </summary>
    public sealed class RemoteConfigCatalogAsset : ScriptableObject
    {
        public const string ResourcePath = "M3P/RemoteConfigCatalog";

        public string signature;
        public string projectId;
        public string environmentId;
        public string environmentName;
        public bool useRemoteInEditorPlayMode;
        public RemoteConfigCatalogEntry[] entries = Array.Empty<RemoteConfigCatalogEntry>();

        public static RemoteConfigCatalogAsset Load()
        {
            return Resources.Load<RemoteConfigCatalogAsset>(ResourcePath);
        }

        public bool ContainsRemoteId(string remoteId)
        {
            if (entries == null || string.IsNullOrEmpty(remoteId))
                return false;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].remoteId == remoteId)
                    return true;
            }

            return false;
        }
    }
}
