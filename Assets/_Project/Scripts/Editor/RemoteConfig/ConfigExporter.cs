using System;
using System.Collections.Generic;
using System.Reflection;
using M3P.RemoteConfig;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor.RemoteConfig
{
    public enum SupportKind
    {
        Supported,
        Partial,
        Unsupported,
        DataError,
        Unavailable,
    }

    public sealed class ExportResult
    {
        public ScriptableObject Asset;
        public string Guid;
        public string Path;
        public string RemoteId;
        public bool Selected;
        public SupportKind Support;
        public string Reason;
        public ConfigContract Contract;
        public JObject Data;
        public List<string> Omitted = new List<string>();
        public List<string> Errors = new List<string>();
        public List<ScriptableObject> Related = new List<ScriptableObject>();
    }

    public static class ConfigExporter
    {
        public static ExportResult Export(ScriptableObject asset, string guid, string remoteId, bool selected)
        {
            var result = new ExportResult
            {
                Asset = asset,
                Guid = guid,
                Path = string.IsNullOrEmpty(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid),
                RemoteId = remoteId,
                Selected = selected,
            };

            if (asset == null)
            {
                result.Support = SupportKind.Unavailable;
                result.Reason = "Brak źródła";
                return result;
            }

            string availability = Availability(asset, result.Path);
            if (availability != null)
            {
                result.Support = SupportKind.Unavailable;
                result.Reason = availability;
                return result;
            }

            var blockers = new List<string>();
            var contract = new ConfigContract
            {
                typeId = M3pConfigTypes.For(asset.GetType()),
                typeSchemaVersion = M3pConfigTypes.CurrentTypeSchemaVersion,
            };
            var data = new JObject();
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                        continue;

                    ConfigField field = ReadProperty(property, asset.GetType(), result.Omitted, result.Related, blockers);
                    if (field == null)
                        continue;

                    contract.fields.Add(field);
                    data[field.name] = ReadValue(property, field, blockers);
                }
                while (property.NextVisible(false));
            }

            ApplyTypeRules(asset, contract);
            result.Contract = contract;
            result.Data = data;

            if (blockers.Count > 0)
            {
                result.Support = SupportKind.Unsupported;
                result.Reason = blockers[0];
                return result;
            }

            if (contract.fields.Count == 0)
            {
                result.Support = SupportKind.Unsupported;
                result.Reason = result.Omitted.Count > 0
                    ? "Same referencje Unity"
                    : "Brak pól danych do synchronizacji";
                return result;
            }

            BalanceConfigValidator.Validate(contract.typeId, contract, data, result.Errors);
            if (result.Errors.Count > 0)
            {
                result.Support = SupportKind.DataError;
                result.Reason = result.Errors[0];
                return result;
            }

            if (result.Omitted.Count > 0)
            {
                result.Support = SupportKind.Partial;
                result.Reason = "Pominięte: " + string.Join(", ", result.Omitted);
                return result;
            }

            result.Support = SupportKind.Supported;
            result.Reason = "Wszystkie pola kontraktu";
            return result;
        }

        public static string Availability(ScriptableObject asset, string path)
        {
            if (asset == null)
                return "Brak źródła";

            if ((asset.hideFlags & HideFlags.DontSave) != 0 && string.IsNullOrEmpty(path))
                return "Obiekt tymczasowy";

            if (!string.IsNullOrEmpty(path) && path.StartsWith("Packages/", StringComparison.Ordinal))
                return "Asset z pakietu";

            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsSubAsset(asset))
                return "Subasset bez obsługi tożsamości";

            if (asset is RemoteConfigCatalogAsset)
                return "Katalog narzędzia";

            return null;
        }

        static void ApplyTypeRules(ScriptableObject asset, ConfigContract contract)
        {
            if (asset is not BattleConfig)
                return;

            ConfigField bonuses = contract.FindField("damagePerSupermatchSize");
            if (bonuses != null)
                bonuses.exactArrayLength = BattleConfig.MaxSupermatchSize - BattleConfig.MinSupermatchSize + 1;
        }

        static ConfigField ReadProperty(
            SerializedProperty property,
            Type declaringType,
            List<string> omitted,
            List<ScriptableObject> related,
            List<string> blockers)
        {
            FieldInfo member = FindField(declaringType, property.name);
            string contractName = ConfigContracts.ContractName(property.name);
            SerializedPropertyType kind = property.propertyType;

            if (kind == SerializedPropertyType.ObjectReference || kind == SerializedPropertyType.ExposedReference)
            {
                omitted.Add(contractName);
                if (property.objectReferenceValue is ScriptableObject relatedAsset && !related.Contains(relatedAsset))
                    related.Add(relatedAsset);
                return null;
            }

            if (kind == SerializedPropertyType.ManagedReference)
            {
                blockers.Add("Pola polimorficzne wymagają adaptera.");
                return null;
            }

            if (kind == SerializedPropertyType.Generic && property.isArray)
                return ReadArray(property, member, contractName, omitted, related, blockers);

            if (kind == SerializedPropertyType.Generic)
            {
                Type structType = member != null ? member.FieldType : declaringType;
                if (structType == null || (member == null && property.name != "data"))
                {
                    blockers.Add("Nieobsługiwane pole " + contractName + ".");
                    return null;
                }

                var field = new ConfigField
                {
                    name = contractName,
                    memberName = property.name,
                    kind = ConfigContracts.Object,
                    children = new List<ConfigField>(),
                };
                ReadChildren(property, structType, field, omitted, related, blockers);
                return field;
            }

            ConfigField scalar = ScalarField(property, member, contractName);
            if (scalar == null)
            {
                blockers.Add("Nieobsługiwane pole " + contractName + ".");
                return null;
            }

            return scalar;
        }

        static ConfigField ReadArray(
            SerializedProperty property,
            FieldInfo member,
            string contractName,
            List<string> omitted,
            List<ScriptableObject> related,
            List<string> blockers)
        {
            Type elementType = ElementType(member);
            if (elementType == null)
            {
                blockers.Add("Nieobsługiwane pole " + contractName + ".");
                return null;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                omitted.Add(contractName);
                CollectArrayReferences(property, related);
                return null;
            }

            var field = new ConfigField
            {
                name = contractName,
                memberName = property.name,
                kind = ConfigContracts.Array,
            };
            ApplyRange(member, field);

            if (property.arraySize > 0)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(0);
                field.element = ReadProperty(element, elementType, omitted, related, blockers);
                if (field.element != null)
                {
                    field.element.name = "item";
                    field.element.memberName = "item";
                }
            }
            else
            {
                field.element = SchemaForType(elementType, null, "item", "item", omitted, blockers, new HashSet<Type>());
            }

            if (field.element == null)
                return null;

            return field;
        }

        static void ReadChildren(
            SerializedProperty property,
            Type declaringType,
            ConfigField parent,
            List<string> omitted,
            List<ScriptableObject> related,
            List<string> blockers)
        {
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            if (!child.NextVisible(true) || child.depth != property.depth + 1)
                return;

            while (!SerializedProperty.EqualContents(child, end))
            {
                ConfigField field = ReadProperty(child, declaringType, omitted, related, blockers);
                if (field != null)
                    parent.children.Add(field);
                if (!child.NextVisible(false))
                    break;
            }
        }

        static ConfigField SchemaForType(
            Type type,
            FieldInfo member,
            string name,
            string memberName,
            List<string> omitted,
            List<string> blockers)
        {
            return SchemaForType(type, member, name, memberName, omitted, blockers, new HashSet<Type>());
        }

        static ConfigField SchemaForType(
            Type type,
            FieldInfo member,
            string name,
            string memberName,
            List<string> omitted,
            List<string> blockers,
            HashSet<Type> stack)
        {
            if (type == null || !stack.Add(type))
            {
                blockers.Add("Graf z cyklami.");
                return null;
            }

            try
            {
                var field = new ConfigField
                {
                    name = name,
                    memberName = memberName,
                    kind = KindFor(type),
                };
                ApplyRange(member, field);
                if (field.kind == null)
                {
                    blockers.Add("Nieobsługiwane pole " + name + ".");
                    return null;
                }

                if (type.IsEnum)
                    field.enumType = type.AssemblyQualifiedName;

                if (field.kind == ConfigContracts.Array)
                {
                    Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    if (elementType == null || typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    {
                        omitted.Add(name);
                        return null;
                    }

                    field.element = SchemaForType(elementType, null, "item", "item", omitted, blockers, stack);
                    return field.element == null ? null : field;
                }

                if (field.kind == ConfigContracts.Object)
                {
                    if (stack.Count > 8)
                    {
                        blockers.Add("Graf z cyklami.");
                        return null;
                    }

                    field.children = new List<ConfigField>();
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo child = fields[i];
                        if (!IsSerialized(child))
                            continue;
                        ConfigField childField = SchemaForType(
                            child.FieldType,
                            child,
                            ConfigContracts.ContractName(child.Name),
                            child.Name,
                            omitted,
                            blockers,
                            stack);
                        if (childField != null)
                            field.children.Add(childField);
                    }
                }

                return field;
            }
            finally
            {
                stack.Remove(type);
            }
        }

        static ConfigField ScalarField(SerializedProperty property, FieldInfo member, string contractName)
        {
            string kind = property.propertyType switch
            {
                SerializedPropertyType.Boolean => ConfigContracts.Bool,
                SerializedPropertyType.Integer => ConfigContracts.Int,
                SerializedPropertyType.Character => ConfigContracts.Int,
                SerializedPropertyType.Float => ConfigContracts.Float,
                SerializedPropertyType.String => ConfigContracts.String,
                SerializedPropertyType.Enum => ConfigContracts.Enum,
                SerializedPropertyType.Color => ConfigContracts.Color,
                SerializedPropertyType.Vector2 => ConfigContracts.Vector2,
                SerializedPropertyType.Vector3 => ConfigContracts.Vector3,
                SerializedPropertyType.Vector4 => ConfigContracts.Vector4,
                SerializedPropertyType.Vector2Int => ConfigContracts.Vector2Int,
                SerializedPropertyType.Vector3Int => ConfigContracts.Vector3Int,
                SerializedPropertyType.Quaternion => ConfigContracts.Quaternion,
                SerializedPropertyType.Rect => ConfigContracts.Rect,
                _ => null,
            };

            if (kind == null)
                return null;

            var field = new ConfigField
            {
                name = contractName,
                memberName = property.name,
                kind = kind,
            };
            if (property.propertyType == SerializedPropertyType.Enum && member != null)
                field.enumType = member.FieldType.AssemblyQualifiedName;
            ApplyRange(member, field);
            return field;
        }

        static JToken ReadValue(SerializedProperty property, ConfigField field, List<string> blockers)
        {
            switch (field.kind)
            {
                case ConfigContracts.Bool:
                    return new JValue(property.boolValue);
                case ConfigContracts.Int:
                    return new JValue(property.intValue);
                case ConfigContracts.Float:
                    if (float.IsNaN(property.floatValue) || float.IsInfinity(property.floatValue))
                    {
                        blockers.Add(field.name + ": NaN i nieskończoność są odrzucane.");
                        return new JValue(0);
                    }

                    return new JValue((double)property.floatValue);
                case ConfigContracts.String:
                    return new JValue(property.stringValue ?? string.Empty);
                case ConfigContracts.Enum:
                    int index = property.enumValueIndex;
                    if (index < 0 || index >= property.enumNames.Length)
                    {
                        blockers.Add(field.name + ": nieznana wartość wyliczenia.");
                        return new JValue(string.Empty);
                    }

                    return new JValue(property.enumNames[index]);
                case ConfigContracts.Color:
                    Color color = property.colorValue;
                    return new JObject
                    {
                        ["r"] = (double)color.r,
                        ["g"] = (double)color.g,
                        ["b"] = (double)color.b,
                        ["a"] = (double)color.a,
                    };
                case ConfigContracts.Vector2:
                    return Vector(property.vector2Value.x, property.vector2Value.y);
                case ConfigContracts.Vector3:
                    return Vector(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                case ConfigContracts.Vector4:
                    return Vector(property.vector4Value.x, property.vector4Value.y, property.vector4Value.z, property.vector4Value.w);
                case ConfigContracts.Quaternion:
                    Quaternion quaternion = property.quaternionValue;
                    return Vector(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
                case ConfigContracts.Vector2Int:
                    return new JObject
                    {
                        ["x"] = property.vector2IntValue.x,
                        ["y"] = property.vector2IntValue.y,
                    };
                case ConfigContracts.Vector3Int:
                    return new JObject
                    {
                        ["x"] = property.vector3IntValue.x,
                        ["y"] = property.vector3IntValue.y,
                        ["z"] = property.vector3IntValue.z,
                    };
                case ConfigContracts.Rect:
                    Rect rect = property.rectValue;
                    return new JObject
                    {
                        ["x"] = (double)rect.x,
                        ["y"] = (double)rect.y,
                        ["width"] = (double)rect.width,
                        ["height"] = (double)rect.height,
                    };
                case ConfigContracts.Array:
                    var array = new JArray();
                    for (int i = 0; i < property.arraySize; i++)
                        array.Add(ReadValue(property.GetArrayElementAtIndex(i), field.element, blockers));
                    return array;
                case ConfigContracts.Object:
                    var obj = new JObject();
                    if (field.children == null)
                        return obj;
                    SerializedProperty end = property.GetEndProperty();
                    SerializedProperty child = property.Copy();
                    if (!child.NextVisible(true))
                        return obj;
                    int guard = 0;
                    while (!SerializedProperty.EqualContents(child, end) && guard++ < 10000)
                    {
                        if (child.depth == property.depth + 1)
                        {
                            ConfigField match = FindChild(field, child.name);
                            if (match != null)
                                obj[match.name] = ReadValue(child, match, blockers);
                        }

                        if (!child.NextVisible(false))
                            break;
                    }

                    return obj;
                default:
                    blockers.Add("Nieobsługiwane pole " + field.name + ".");
                    return JValue.CreateNull();
            }
        }

        static ConfigField FindChild(ConfigField field, string memberName)
        {
            if (field.children == null)
                return null;
            for (int i = 0; i < field.children.Count; i++)
            {
                if (field.children[i].memberName == memberName)
                    return field.children[i];
            }

            return null;
        }

        static JObject Vector(float x, float y, float z = float.NaN, float w = float.NaN)
        {
            var obj = new JObject
            {
                ["x"] = (double)x,
                ["y"] = (double)y,
            };
            if (!float.IsNaN(z))
                obj["z"] = (double)z;
            if (!float.IsNaN(w))
                obj["w"] = (double)w;
            return obj;
        }

        static void CollectArrayReferences(SerializedProperty property, List<ScriptableObject> related)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableObject asset && !related.Contains(asset))
                    related.Add(asset);
            }
        }

        static void ApplyRange(FieldInfo member, ConfigField field)
        {
            if (member == null)
                return;

            MinAttribute min = member.GetCustomAttribute<MinAttribute>();
            if (min != null)
            {
                field.hasMin = true;
                field.min = min.min;
            }
        }

        static Type ElementType(FieldInfo member)
        {
            if (member == null)
                return null;
            Type type = member.FieldType;
            if (type.IsArray)
                return type.GetElementType();
            if (type.IsGenericType)
                return type.GetGenericArguments()[0];
            return null;
        }

        static string KindFor(Type type)
        {
            if (type == null || typeof(UnityEngine.Object).IsAssignableFrom(type))
                return null;
            if (type == typeof(bool)) return ConfigContracts.Bool;
            if (type == typeof(int) || type == typeof(char)) return ConfigContracts.Int;
            if (type == typeof(float) || type == typeof(double)) return ConfigContracts.Float;
            if (type == typeof(string)) return ConfigContracts.String;
            if (type.IsEnum) return ConfigContracts.Enum;
            if (type == typeof(Color)) return ConfigContracts.Color;
            if (type == typeof(Vector2)) return ConfigContracts.Vector2;
            if (type == typeof(Vector3)) return ConfigContracts.Vector3;
            if (type == typeof(Vector4)) return ConfigContracts.Vector4;
            if (type == typeof(Vector2Int)) return ConfigContracts.Vector2Int;
            if (type == typeof(Vector3Int)) return ConfigContracts.Vector3Int;
            if (type == typeof(Quaternion)) return ConfigContracts.Quaternion;
            if (type == typeof(Rect)) return ConfigContracts.Rect;
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return ConfigContracts.Array;
            if (type.IsValueType || type.GetCustomAttribute<SerializableAttribute>() != null)
                return ConfigContracts.Object;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return null;
            return null;
        }

        static bool IsSerialized(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral)
                return false;
            if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                return false;
            if (field.GetCustomAttribute<SerializeField>() != null)
                return true;
            return field.IsPublic;
        }

        static FieldInfo FindField(Type type, string memberName)
        {
            while (type != null && type != typeof(object))
            {
                FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field;
                type = type.BaseType;
            }

            return null;
        }
    }
}
