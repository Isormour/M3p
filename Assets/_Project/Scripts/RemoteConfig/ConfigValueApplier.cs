using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// Writes a contract document onto a memory copy. It never assigns the source asset.
    /// </summary>
    public static class ConfigValueApplier
    {
        public static bool Apply(object target, ConfigContract contract, JObject data, List<string> errors)
        {
            if (target == null || contract == null || data == null)
            {
                errors?.Add("Brak obiektu albo danych do zastosowania.");
                return false;
            }

            var validation = new List<string>();
            BalanceConfigValidator.Validate(contract.typeId, contract, data, validation);
            if (validation.Count > 0)
            {
                if (errors != null)
                    errors.AddRange(validation);
                return false;
            }

            if (contract.fields == null)
                return true;

            for (int i = 0; i < contract.fields.Count; i++)
            {
                ConfigField field = contract.fields[i];
                if (!TrySet(target, field, data[field.name], errors))
                    return false;
            }

            return true;
        }

        public static ConfigContract FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonConvert.DeserializeObject<ConfigContract>(json);
        }

        public static string ToJson(ConfigContract contract)
        {
            return JsonConvert.SerializeObject(contract, Formatting.None);
        }

        static bool TrySet(object target, ConfigField field, JToken token, List<string> errors)
        {
            FieldInfo member = FindField(target.GetType(), field.memberName);
            if (member == null)
            {
                errors?.Add("Brak pola " + field.memberName + " w " + target.GetType().Name + ".");
                return false;
            }

            if (!TryConvert(member.FieldType, field, token, out object value, out string error))
            {
                errors?.Add(field.name + ": " + error);
                return false;
            }

            member.SetValue(target, value);
            return true;
        }

        static bool TryConvert(Type type, ConfigField field, JToken token, out object value, out string error)
        {
            value = null;
            error = null;
            if (!ConfigValueCodec.TryNormalize(token, field, out JToken normalized, out error))
                return false;

            switch (field.kind)
            {
                case ConfigContracts.Bool:
                    value = normalized.Value<bool>();
                    return true;
                case ConfigContracts.Int:
                    value = normalized.Value<int>();
                    return true;
                case ConfigContracts.Float:
                    value = ConvertFloat(type, normalized.Value<double>());
                    return true;
                case ConfigContracts.String:
                    value = normalized.Value<string>();
                    return true;
                case ConfigContracts.Enum:
                    try
                    {
                        value = Enum.Parse(type, normalized.Value<string>());
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        error = "Nieznana wartość wyliczenia.";
                        return false;
                    }
                case ConfigContracts.Color:
                    value = ReadColor((JObject)normalized);
                    return true;
                case ConfigContracts.Vector2:
                    value = ReadVector2((JObject)normalized);
                    return true;
                case ConfigContracts.Vector3:
                    value = ReadVector3((JObject)normalized);
                    return true;
                case ConfigContracts.Vector4:
                    value = ReadVector4((JObject)normalized);
                    return true;
                case ConfigContracts.Quaternion:
                    value = ReadQuaternion((JObject)normalized);
                    return true;
                case ConfigContracts.Vector2Int:
                    value = ReadVector2Int((JObject)normalized);
                    return true;
                case ConfigContracts.Vector3Int:
                    value = ReadVector3Int((JObject)normalized);
                    return true;
                case ConfigContracts.Rect:
                    value = ReadRect((JObject)normalized);
                    return true;
                case ConfigContracts.Array:
                    return TryConvertArray(type, field, (JArray)normalized, out value, out error);
                case ConfigContracts.Object:
                    return TryConvertObject(type, field, (JObject)normalized, out value, out error);
                default:
                    error = "Nieobsługiwany rodzaj pola.";
                    return false;
            }
        }

        static object ConvertFloat(Type type, double number)
        {
            if (type == typeof(float))
                return (float)number;
            return number;
        }

        static bool TryConvertArray(Type type, ConfigField field, JArray array, out object value, out string error)
        {
            value = null;
            error = null;
            Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            Array created = Array.CreateInstance(elementType, array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                if (!TryConvert(elementType, field.element, array[i], out object element, out error))
                    return false;
                created.SetValue(element, i);
            }

            if (type.IsArray)
            {
                value = created;
                return true;
            }

            object list = Activator.CreateInstance(type);
            MethodInfo add = type.GetMethod("Add");
            for (int i = 0; i < created.Length; i++)
                add.Invoke(list, new[] { created.GetValue(i) });
            value = list;
            return true;
        }

        static bool TryConvertObject(Type type, ConfigField field, JObject data, out object value, out string error)
        {
            value = null;
            error = null;
            object instance;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception exception)
            {
                error = "Nie można utworzyć " + type.Name + ". " + exception.Message;
                return false;
            }

            if (field.children != null)
            {
                for (int i = 0; i < field.children.Count; i++)
                {
                    ConfigField child = field.children[i];
                    FieldInfo member = FindField(type, child.memberName);
                    if (member == null)
                    {
                        error = "Brak pola " + child.memberName + ".";
                        return false;
                    }

                    if (!TryConvert(member.FieldType, child, data[child.name], out object childValue, out error))
                        return false;
                    member.SetValue(instance, childValue);
                }
            }

            value = instance;
            return true;
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

        static Color ReadColor(JObject data)
        {
            return new Color(
                (float)data.Value<double>("r"),
                (float)data.Value<double>("g"),
                (float)data.Value<double>("b"),
                (float)data.Value<double>("a"));
        }

        static Vector2 ReadVector2(JObject data)
        {
            return new Vector2((float)data.Value<double>("x"), (float)data.Value<double>("y"));
        }

        static Vector3 ReadVector3(JObject data)
        {
            return new Vector3(
                (float)data.Value<double>("x"),
                (float)data.Value<double>("y"),
                (float)data.Value<double>("z"));
        }

        static Vector4 ReadVector4(JObject data)
        {
            return new Vector4(
                (float)data.Value<double>("x"),
                (float)data.Value<double>("y"),
                (float)data.Value<double>("z"),
                (float)data.Value<double>("w"));
        }

        static Quaternion ReadQuaternion(JObject data)
        {
            return new Quaternion(
                (float)data.Value<double>("x"),
                (float)data.Value<double>("y"),
                (float)data.Value<double>("z"),
                (float)data.Value<double>("w"));
        }

        static Vector2Int ReadVector2Int(JObject data)
        {
            return new Vector2Int(data.Value<int>("x"), data.Value<int>("y"));
        }

        static Vector3Int ReadVector3Int(JObject data)
        {
            return new Vector3Int(data.Value<int>("x"), data.Value<int>("y"), data.Value<int>("z"));
        }

        static Rect ReadRect(JObject data)
        {
            return new Rect(
                (float)data.Value<double>("x"),
                (float)data.Value<double>("y"),
                (float)data.Value<double>("width"),
                (float)data.Value<double>("height"));
        }
    }
}
