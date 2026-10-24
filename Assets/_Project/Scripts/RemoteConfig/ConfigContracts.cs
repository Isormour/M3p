using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace M3P.RemoteConfig
{
    public static class ConfigContracts
    {
        public const string Bool = "bool";
        public const string Int = "int";
        public const string Float = "float";
        public const string String = "string";
        public const string Enum = "enum";
        public const string Array = "array";
        public const string Object = "object";
        public const string Color = "color";
        public const string Vector2 = "vector2";
        public const string Vector3 = "vector3";
        public const string Vector4 = "vector4";
        public const string Vector2Int = "vector2int";
        public const string Vector3Int = "vector3int";
        public const string Quaternion = "quaternion";
        public const string Rect = "rect";

        public static string ContractName(string memberName)
        {
            if (string.IsNullOrEmpty(memberName))
                return memberName;

            if (memberName.StartsWith("m_", StringComparison.Ordinal) && memberName.Length > 2)
                memberName = memberName.Substring(2);
            else if (memberName[0] == '_' && memberName.Length > 1)
                memberName = memberName.Substring(1);

            if (memberName.Length == 0)
                return memberName;

            if (char.IsUpper(memberName[0]))
                return char.ToLowerInvariant(memberName[0]) + memberName.Substring(1);

            return memberName;
        }
    }

    public sealed class ConfigField
    {
        public string name;
        public string memberName;
        public string kind;
        public string enumType;
        public int exactArrayLength = -1;
        public bool hasMin;
        public double min;
        public bool hasMax;
        public double max;
        public ConfigField element;
        public List<ConfigField> children;
    }

    public sealed class ConfigContract
    {
        public string typeId;
        public int typeSchemaVersion = M3pConfigTypes.CurrentTypeSchemaVersion;
        public List<ConfigField> fields = new List<ConfigField>();

        public ConfigField FindField(string fieldName)
        {
            if (fields == null || string.IsNullOrEmpty(fieldName))
                return null;

            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] != null && fields[i].name == fieldName)
                    return fields[i];
            }

            return null;
        }
    }

    /// <summary>
    /// Typed comparison and conversion for exported values. Field order and JSON whitespace are ignored.
    /// </summary>
    public static class ConfigValueCodec
    {
        public static bool TryNormalize(JToken token, ConfigField field, out JToken normalized, out string error)
        {
            normalized = null;
            error = null;
            if (field == null)
            {
                error = "Brak kontraktu pola.";
                return false;
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                error = "Brak wartości.";
                return false;
            }

            switch (field.kind)
            {
                case ConfigContracts.Bool:
                    if (token.Type != JTokenType.Boolean)
                    {
                        error = "Oczekiwano wartości logicznej.";
                        return false;
                    }

                    normalized = new JValue(token.Value<bool>());
                    return true;

                case ConfigContracts.Int:
                    if (!TryReadInt(token, out int intValue, out error))
                        return false;
                    if (!FitsRange(intValue, field, out error))
                        return false;
                    normalized = new JValue(intValue);
                    return true;

                case ConfigContracts.Float:
                    if (!TryReadFloat(token, out double floatValue, out error))
                        return false;
                    if (!FitsRange(floatValue, field, out error))
                        return false;
                    normalized = new JValue(floatValue);
                    return true;

                case ConfigContracts.String:
                    if (token.Type != JTokenType.String)
                    {
                        error = "Oczekiwano tekstu.";
                        return false;
                    }

                    normalized = new JValue(token.Value<string>() ?? string.Empty);
                    return true;

                case ConfigContracts.Enum:
                    if (token.Type != JTokenType.String || string.IsNullOrEmpty(token.Value<string>()))
                    {
                        error = "Oczekiwano nazwy wartości wyliczenia.";
                        return false;
                    }

                    normalized = new JValue(token.Value<string>());
                    return true;

                case ConfigContracts.Color:
                    return TryNormalizeComponents(token, 4, true, out normalized, out error);
                case ConfigContracts.Vector2:
                    return TryNormalizeComponents(token, 2, true, out normalized, out error);
                case ConfigContracts.Vector3:
                    return TryNormalizeComponents(token, 3, true, out normalized, out error);
                case ConfigContracts.Vector4:
                case ConfigContracts.Quaternion:
                    return TryNormalizeComponents(token, 4, true, out normalized, out error);
                case ConfigContracts.Vector2Int:
                    return TryNormalizeComponents(token, 2, false, out normalized, out error);
                case ConfigContracts.Vector3Int:
                    return TryNormalizeComponents(token, 3, false, out normalized, out error);
                case ConfigContracts.Rect:
                    return TryNormalizeRect(token, out normalized, out error);

                case ConfigContracts.Array:
                    return TryNormalizeArray(token, field, out normalized, out error);

                case ConfigContracts.Object:
                    return TryNormalizeObject(token, field.children, out normalized, out error);

                default:
                    error = "Nieobsługiwany rodzaj pola " + field.kind + ".";
                    return false;
            }
        }

        public static bool ValuesEqual(JToken left, JToken right, ConfigField field)
        {
            if (!TryNormalize(left, field, out JToken a, out _) || !TryNormalize(right, field, out JToken b, out _))
                return false;

            return JToken.DeepEquals(a, b);
        }

        public static string Display(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return "brak";

            return M3pConfigDocument.Canonicalize(token);
        }

        static bool TryNormalizeArray(JToken token, ConfigField field, out JToken normalized, out string error)
        {
            normalized = null;
            error = null;
            if (token.Type != JTokenType.Array)
            {
                error = "Oczekiwano tablicy.";
                return false;
            }

            var array = (JArray)token;
            if (field.exactArrayLength >= 0 && array.Count != field.exactArrayLength)
            {
                error = "Tablica ma " + array.Count.ToString(CultureInfo.InvariantCulture)
                    + " elementów, a kontrakt wymaga "
                    + field.exactArrayLength.ToString(CultureInfo.InvariantCulture) + ".";
                return false;
            }

            var result = new JArray();
            for (int i = 0; i < array.Count; i++)
            {
                if (!TryNormalize(array[i], field.element, out JToken element, out error))
                {
                    error = "Element " + i.ToString(CultureInfo.InvariantCulture) + ": " + error;
                    return false;
                }

                result.Add(element);
            }

            normalized = result;
            return true;
        }

        static bool TryNormalizeObject(JToken token, List<ConfigField> children, out JToken normalized, out string error)
        {
            normalized = null;
            error = null;
            if (token.Type != JTokenType.Object)
            {
                error = "Oczekiwano obiektu.";
                return false;
            }

            var source = (JObject)token;
            var result = new JObject();
            if (children == null)
            {
                normalized = result;
                return true;
            }

            for (int i = 0; i < children.Count; i++)
            {
                ConfigField child = children[i];
                JToken value = source[child.name];
                if (value == null)
                {
                    error = "Brak pola " + child.name + ".";
                    return false;
                }

                if (!TryNormalize(value, child, out JToken childValue, out error))
                {
                    error = child.name + ": " + error;
                    return false;
                }

                result[child.name] = childValue;
            }

            normalized = result;
            return true;
        }

        static bool TryNormalizeComponents(JToken token, int count, bool floating, out JToken normalized, out string error)
        {
            normalized = null;
            error = null;
            string[] names = count == 2
                ? new[] { "x", "y" }
                : count == 3
                    ? new[] { "x", "y", "z" }
                    : new[] { "x", "y", "z", "w" };

            if (token.Type != JTokenType.Object)
            {
                error = "Oczekiwano współrzędnych.";
                return false;
            }

            var source = (JObject)token;
            if (count == 4 && source["r"] != null)
                names = new[] { "r", "g", "b", "a" };

            var result = new JObject();
            for (int i = 0; i < names.Length; i++)
            {
                JToken component = source[names[i]];
                if (component == null)
                {
                    error = "Brak składowej " + names[i] + ".";
                    return false;
                }

                if (floating)
                {
                    if (!TryReadFloat(component, out double number, out error))
                        return false;
                    result[names[i]] = new JValue(number);
                }
                else
                {
                    if (!TryReadInt(component, out int number, out error))
                        return false;
                    result[names[i]] = new JValue(number);
                }
            }

            normalized = result;
            return true;
        }

        static bool TryNormalizeRect(JToken token, out JToken normalized, out string error)
        {
            normalized = null;
            error = null;
            if (token.Type != JTokenType.Object)
            {
                error = "Oczekiwano prostokąta.";
                return false;
            }

            var source = (JObject)token;
            string[] names = { "x", "y", "width", "height" };
            var result = new JObject();
            for (int i = 0; i < names.Length; i++)
            {
                if (!TryReadFloat(source[names[i]], out double number, out error))
                {
                    error = names[i] + ": " + error;
                    return false;
                }

                result[names[i]] = new JValue(number);
            }

            normalized = result;
            return true;
        }

        public static bool TryReadInt(JToken token, out int value, out string error)
        {
            value = 0;
            error = null;
            if (token == null || token.Type == JTokenType.Null)
            {
                error = "Brak liczby całkowitej.";
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                long raw = token.Value<long>();
                if (raw < int.MinValue || raw > int.MaxValue)
                {
                    error = "Liczba poza zakresem int.";
                    return false;
                }

                value = (int)raw;
                return true;
            }

            if (token.Type == JTokenType.Float)
            {
                double raw = token.Value<double>();
                if (double.IsNaN(raw) || double.IsInfinity(raw) || raw != Math.Round(raw))
                {
                    error = "Wartość nie jest liczbą całkowitą.";
                    return false;
                }

                if (raw < int.MinValue || raw > int.MaxValue)
                {
                    error = "Liczba poza zakresem int.";
                    return false;
                }

                value = (int)raw;
                return true;
            }

            error = "Oczekiwano liczby całkowitej.";
            return false;
        }

        public static bool TryReadFloat(JToken token, out double value, out string error)
        {
            value = 0d;
            error = null;
            if (token == null || token.Type == JTokenType.Null)
            {
                error = "Brak liczby.";
                return false;
            }

            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
            {
                error = "Oczekiwano liczby.";
                return false;
            }

            value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                error = "NaN i nieskończoność są odrzucane.";
                return false;
            }

            return true;
        }

        static bool FitsRange(double value, ConfigField field, out string error)
        {
            error = null;
            if (field.hasMin && value < field.min)
            {
                error = "Wartość jest mniejsza od " + field.min.ToString("R", CultureInfo.InvariantCulture) + ".";
                return false;
            }

            if (field.hasMax && value > field.max)
            {
                error = "Wartość jest większa od " + field.max.ToString("R", CultureInfo.InvariantCulture) + ".";
                return false;
            }

            return true;
        }
    }
}
