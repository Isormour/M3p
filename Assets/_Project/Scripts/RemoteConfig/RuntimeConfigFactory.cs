using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// Last accepted document, split by project, environment, schema and catalog signature.
    /// </summary>
    public static class RemoteBalanceCache
    {
        public static string PathFor(string projectId, string environmentId, string signature)
        {
            string root = Path.Combine(Application.persistentDataPath, "M3PRemoteConfig");
            string name = M3pConfigDocument.Sha256(
                (projectId ?? string.Empty) + "|" +
                (environmentId ?? string.Empty) + "|" +
                M3pConfigDocument.SchemaVersion + "|" +
                (signature ?? string.Empty));
            return Path.Combine(root, name + ".json");
        }

        public static void Save(string projectId, string environmentId, string signature, JObject document)
        {
            if (document == null)
                return;

            try
            {
                string path = PathFor(projectId, environmentId, signature);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
                var envelope = new JObject
                {
                    ["projectId"] = projectId ?? string.Empty,
                    ["environmentId"] = environmentId ?? string.Empty,
                    ["schemaVersion"] = M3pConfigDocument.SchemaVersion,
                    ["catalogSignature"] = signature ?? string.Empty,
                    ["document"] = document,
                };
                File.WriteAllText(path, envelope.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Remote Config: nie zapisano lokalnej kopii. " + exception.Message);
            }
        }

        public static bool TryLoad(string projectId, string environmentId, string signature, out JObject document)
        {
            document = null;
            try
            {
                string path = PathFor(projectId, environmentId, signature);
                if (!File.Exists(path))
                    return false;

                var envelope = JObject.Parse(File.ReadAllText(path));
                if (envelope.Value<string>("projectId") != (projectId ?? string.Empty))
                    return false;
                if (envelope.Value<string>("environmentId") != (environmentId ?? string.Empty))
                    return false;
                if (envelope.Value<int>("schemaVersion") != M3pConfigDocument.SchemaVersion)
                    return false;
                if (envelope.Value<string>("catalogSignature") != (signature ?? string.Empty))
                    return false;

                document = envelope["document"] as JObject;
                return document != null && M3pConfigDocument.TryParse(document.ToString(), out document, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public sealed class RuntimeConfigSession
    {
        public GameConfig Config;
        public Dictionary<UnityEngine.Object, UnityEngine.Object> Copies = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
    }

    /// <summary>
    /// Clones the configs the game will mutate and points owners at those copies.
    /// </summary>
    public static class RuntimeConfigFactory
    {
        public static RuntimeConfigSession Build(GameConfig source, RemoteConfigCatalogAsset catalog)
        {
            var session = new RuntimeConfigSession();
            if (source == null)
                return session;

            if (catalog != null && catalog.entries != null)
            {
                for (int i = 0; i < catalog.entries.Length; i++)
                {
                    RemoteConfigCatalogEntry entry = catalog.entries[i];
                    if (entry?.source == null || session.Copies.ContainsKey(entry.source))
                        continue;

                    ScriptableObject copy = UnityEngine.Object.Instantiate(entry.source);
                    copy.name = entry.source.name + " (Remote)";
                    copy.hideFlags = HideFlags.HideAndDontSave;
                    session.Copies.Add(entry.source, copy);
                }
            }

            GameConfig configCopy = UnityEngine.Object.Instantiate(source);
            configCopy.name = source.name + " (Remote)";
            configCopy.hideFlags = HideFlags.HideAndDontSave;
            Rewire(configCopy, session.Copies);
            session.Config = configCopy;
            return session;
        }

        public static bool ApplyDocument(RuntimeConfigSession session, RemoteConfigCatalogAsset catalog, JObject document, out string error)
        {
            error = null;
            if (session == null || catalog == null || catalog.entries == null)
                return true;

            if (!M3pConfigDocument.TryParse(M3pConfigDocument.Serialize(document), out JObject parsed, out error))
                return false;

            var pending = new List<(ScriptableObject copy, ConfigContract contract, JObject data)>();
            for (int i = 0; i < catalog.entries.Length; i++)
            {
                RemoteConfigCatalogEntry entry = catalog.entries[i];
                if (entry?.source == null)
                    continue;

                if (!M3pConfigDocument.TryGetEntry(parsed, entry.remoteId, out JObject remoteEntry))
                    continue;

                string typeId = remoteEntry.Value<string>("typeId");
                int schema = remoteEntry["typeSchemaVersion"] != null ? remoteEntry.Value<int>("typeSchemaVersion") : 0;
                if (typeId != entry.typeId || schema != entry.typeSchemaVersion)
                {
                    error = "Niepoprawny kontrakt wpisu " + entry.remoteId + ".";
                    return false;
                }

                if (remoteEntry["data"] is not JObject data)
                {
                    error = "Niepoprawny kontrakt wpisu " + entry.remoteId + ".";
                    return false;
                }

                ConfigContract contract = ConfigValueApplier.FromJson(entry.contractJson);
                if (contract == null)
                {
                    error = "Brak kontraktu dla " + entry.remoteId + ".";
                    return false;
                }

                if (!session.Copies.TryGetValue(entry.source, out UnityEngine.Object copyObject) || copyObject is not ScriptableObject copy)
                {
                    error = "Brak kopii obiektu " + entry.remoteId + ".";
                    return false;
                }

                pending.Add((copy, contract, data));
            }

            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                var errors = new List<string>();
                BalanceConfigValidator.Validate(item.contract.typeId, item.contract, item.data, errors);
                if (errors.Count > 0)
                {
                    error = errors[0];
                    return false;
                }
            }

            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                var errors = new List<string>();
                if (!ConfigValueApplier.Apply(item.copy, item.contract, item.data, errors))
                {
                    error = errors.Count > 0 ? errors[0] : "Nie zastosowano konfiguracji.";
                    return false;
                }
            }

            return true;
        }

        public static void RewireScene(RuntimeConfigSession session)
        {
            if (session == null || session.Copies.Count == 0)
                return;

            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid())
                    continue;
                Rewire(behaviour, session.Copies);
            }
        }

        static void Rewire(object target, Dictionary<UnityEngine.Object, UnityEngine.Object> copies)
        {
            var seen = new HashSet<object>();
            RewireFields(target, copies, seen);
        }

        static void RewireFields(object target, Dictionary<UnityEngine.Object, UnityEngine.Object> copies, HashSet<object> seen)
        {
            if (target == null || target is string || !seen.Add(target))
                return;

            Type type = target.GetType();
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.IsStatic || field.IsLiteral)
                        continue;
                    if (field.IsNotSerialized && field.GetCustomAttributes(typeof(SerializeField), true).Length == 0 && !field.IsPublic)
                        continue;

                    Type fieldType = field.FieldType;
                    if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                    {
                        if (field.GetValue(target) is UnityEngine.Object current &&
                            current != null &&
                            copies.TryGetValue(current, out UnityEngine.Object replacement))
                            field.SetValue(target, replacement);
                        continue;
                    }

                    if (fieldType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(fieldType.GetElementType()))
                    {
                        if (field.GetValue(target) is Array array)
                        {
                            for (int index = 0; index < array.Length; index++)
                            {
                                if (array.GetValue(index) is UnityEngine.Object current &&
                                    current != null &&
                                    copies.TryGetValue(current, out UnityEngine.Object replacement))
                                    array.SetValue(replacement, index);
                            }
                        }

                        continue;
                    }

                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                        typeof(UnityEngine.Object).IsAssignableFrom(fieldType.GetGenericArguments()[0]))
                    {
                        if (field.GetValue(target) is System.Collections.IList list)
                        {
                            for (int index = 0; index < list.Count; index++)
                            {
                                if (list[index] is UnityEngine.Object current &&
                                    current != null &&
                                    copies.TryGetValue(current, out UnityEngine.Object replacement))
                                    list[index] = replacement;
                            }
                        }

                        continue;
                    }

                    if (!fieldType.IsPrimitive && !fieldType.IsEnum && fieldType != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                    {
                        object nested = field.GetValue(target);
                        if (nested != null && !fieldType.IsValueType)
                            RewireFields(nested, copies, seen);
                    }
                }

                type = type.BaseType;
            }
        }
    }
}
