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
        internal readonly HashSet<UnityEngine.Object> RuntimeCopies = new HashSet<UnityEngine.Object>();
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

                    CreateCopy(entry.source, session);
                }
            }

            session.Config = (GameConfig)CreateCopy(source, session);
            // Register every selected asset first, then include the owners that lead to it.
            // A selected enemy can be reached through both a registry and a map encounter.
            var initialCopies = new List<UnityEngine.Object>(session.Copies.Values);
            for (int i = 0; i < initialCopies.Count; i++)
                RewireOwner(initialCopies[i], session);
            return session;
        }

        static ScriptableObject CreateCopy(ScriptableObject source, RuntimeConfigSession session)
        {
            if (session.Copies.TryGetValue(source, out UnityEngine.Object existing))
                return (ScriptableObject)existing;

            ScriptableObject copy = UnityEngine.Object.Instantiate(source);
            // Names are used as persistent keys for tiles, encounters and map graphs.
            copy.name = source.name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            session.Copies.Add(source, copy);
            session.RuntimeCopies.Add(copy);
            return copy;
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
                RewireOwner(behaviour, session);
            }
        }

        public static void RewireOwner(object target, RuntimeConfigSession session)
        {
            if (target == null || session == null || session.Copies.Count == 0)
                return;

            RewireFields(target, session, new HashSet<object>());
        }

        static void RewireFields(object target, RuntimeConfigSession session, HashSet<object> seen)
        {
            if (target == null || target is string || !seen.Add(target))
                return;

            foreach (var field in SerializedFields(target.GetType()))
            {
                object value = field.GetValue(target);
                object replacement = RewireValue(value, session, seen);
                if (field.FieldType.IsValueType || !ReferenceEquals(value, replacement))
                    field.SetValue(target, replacement);
            }
        }

        static object RewireValue(object value, RuntimeConfigSession session, HashSet<object> seen)
        {
            if (value == null)
                return null;

            if (value is UnityEngine.Object unityObject)
            {
                if (unityObject == null || session.RuntimeCopies.Contains(unityObject))
                    return value;
                if (session.Copies.TryGetValue(unityObject, out UnityEngine.Object replacement))
                    return replacement;
                if (unityObject is not ScriptableObject source ||
                    !NeedsCopy(source, session, new HashSet<ScriptableObject>()))
                    return value;

                ScriptableObject copy = CreateCopy(source, session);
                RewireFields(copy, session, seen);
                return copy;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string)
                return value;

            if (value is System.Collections.IList list)
            {
                if (!seen.Add(value) || list.IsReadOnly)
                    return value;
                for (int i = 0; i < list.Count; i++)
                    list[i] = RewireValue(list[i], session, seen);
                return value;
            }

            if (type.IsSerializable)
                RewireFields(value, session, seen);
            return value;
        }

        // Only clone owners of selected assets. Unrelated assets such as fonts stay shared.
        // Keep the visited set local to a search: caching a negative result in a cyclic
        // graph can otherwise hide a path to a selected config discovered later.
        static bool NeedsCopy(ScriptableObject source, RuntimeConfigSession session, HashSet<ScriptableObject> visited)
        {
            if (source == null || session.RuntimeCopies.Contains(source))
                return false;
            if (session.Copies.ContainsKey(source))
                return true;
            if (!visited.Add(source))
                return false;

            foreach (ScriptableObject child in ScriptableReferences(source, new HashSet<object>()))
                if (NeedsCopy(child, session, visited))
                    return true;
            return false;
        }

        static IEnumerable<ScriptableObject> ScriptableReferences(object target, HashSet<object> seen)
        {
            if (target == null || !seen.Add(target))
                yield break;
            foreach (var field in SerializedFields(target.GetType()))
                foreach (ScriptableObject child in ReferencesInValue(field.GetValue(target), seen))
                    yield return child;
        }

        static IEnumerable<ScriptableObject> ReferencesInValue(object value, HashSet<object> seen)
        {
            if (value == null)
                yield break;
            if (value is ScriptableObject config)
            {
                if (config != null)
                    yield return config;
                yield break;
            }
            Type type = value.GetType();
            if (value is UnityEngine.Object || value is string || type.IsPrimitive || type.IsEnum)
                yield break;
            if (value is System.Collections.IList list)
            {
                if (!seen.Add(value))
                    yield break;
                foreach (object item in list)
                    foreach (ScriptableObject child in ReferencesInValue(item, seen))
                        yield return child;
            }
            else if (type.IsSerializable)
            {
                foreach (ScriptableObject child in ScriptableReferences(value, seen))
                    yield return child;
            }
        }

        static IEnumerable<System.Reflection.FieldInfo> SerializedFields(Type type)
        {
            while (type != null && type != typeof(object) && type != typeof(UnityEngine.Object))
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (field.IsStatic || field.IsLiteral || field.IsInitOnly || field.IsNotSerialized)
                        continue;
                    if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)) ||
                        Attribute.IsDefined(field, typeof(SerializeReference)))
                        yield return field;
                }
                type = type.BaseType;
            }
        }
    }
}
