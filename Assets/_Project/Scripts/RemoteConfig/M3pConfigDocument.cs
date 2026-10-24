using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// The M3P document stored in the single Remote Config JSON setting <see cref="Key"/>.
    /// </summary>
    public static class M3pConfigDocument
    {
        public const int SchemaVersion = 1;
        public const string Key = "m3p_configs_v1";
        public const int MaxPayloadBytes = 5 * 1024 * 1024;

        public static JObject CreateEmpty(int revision)
        {
            return new JObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["revision"] = revision,
                ["objects"] = new JObject(),
            };
        }

        public static bool TryParse(string json, out JObject document, out string error)
        {
            document = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Pusty dokument.";
                return false;
            }

            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    var settings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    };
                    document = JObject.Load(reader, settings);
                }
            }
            catch (JsonReaderException exception)
            {
                error = "Uszkodzony JSON: " + exception.Message;
                return false;
            }
            catch (JsonException)
            {
                error = "Konflikt identyfikatorów albo uszkodzony JSON.";
                return false;
            }

            if (document["schemaVersion"] == null || document["schemaVersion"].Type != JTokenType.Integer)
            {
                error = "Nieobsługiwana konfiguracja.";
                return false;
            }

            if (document.Value<int>("schemaVersion") != SchemaVersion)
            {
                error = "Nieobsługiwana konfiguracja.";
                return false;
            }

            if (document["objects"] == null || document["objects"].Type != JTokenType.Object)
            {
                error = "Uszkodzony JSON: brak obiektów.";
                return false;
            }

            return true;
        }

        public static int ReadRevision(JObject document)
        {
            if (document == null || document["revision"] == null)
                return 0;

            if (!ConfigValueCodec.TryReadInt(document["revision"], out int revision, out _))
                return 0;

            return revision;
        }

        public static JObject Objects(JObject document)
        {
            return document?["objects"] as JObject;
        }

        public static bool TryGetEntry(JObject document, string remoteId, out JObject entry)
        {
            entry = null;
            JObject objects = Objects(document);
            if (objects == null || string.IsNullOrEmpty(remoteId))
                return false;

            entry = objects[remoteId] as JObject;
            return entry != null;
        }

        public static string Serialize(JToken token)
        {
            return Canonicalize(token);
        }

        public static string ContentHash(JObject document)
        {
            var copy = document == null ? CreateEmpty(0) : (JObject)document.DeepClone();
            copy.Remove("revision");
            return Sha256(Canonicalize(copy));
        }

        public static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public static string Canonicalize(JToken token)
        {
            var builder = new StringBuilder();
            WriteCanonical(token, builder);
            return builder.ToString();
        }

        public static int Utf8Size(string text)
        {
            return Encoding.UTF8.GetByteCount(text ?? string.Empty);
        }

        static void WriteCanonical(JToken token, StringBuilder builder)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                builder.Append("null");
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    WriteObject((JObject)token, builder);
                    return;
                case JTokenType.Array:
                    WriteArray((JArray)token, builder);
                    return;
                case JTokenType.Integer:
                    builder.Append(token.Value<long>().ToString(CultureInfo.InvariantCulture));
                    return;
                case JTokenType.Float:
                    double number = token.Value<double>();
                    if (double.IsNaN(number) || double.IsInfinity(number))
                        throw new InvalidOperationException("NaN i nieskończoność są odrzucane.");
                    builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case JTokenType.String:
                    builder.Append('"').Append(Escape(token.Value<string>() ?? string.Empty)).Append('"');
                    return;
                case JTokenType.Boolean:
                    builder.Append(token.Value<bool>() ? "true" : "false");
                    return;
                default:
                    builder.Append(token.ToString(Formatting.None));
                    return;
            }
        }

        static void WriteObject(JObject obj, StringBuilder builder)
        {
            var names = new List<string>();
            foreach (JProperty property in obj.Properties())
                names.Add(property.Name);
            names.Sort(StringComparer.Ordinal);

            builder.Append('{');
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append('"').Append(Escape(names[i])).Append('"').Append(':');
                WriteCanonical(obj[names[i]], builder);
            }

            builder.Append('}');
        }

        static void WriteArray(JArray array, StringBuilder builder)
        {
            builder.Append('[');
            for (int i = 0; i < array.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                WriteCanonical(array[i], builder);
            }

            builder.Append(']');
        }

        static string Escape(string text)
        {
            var builder = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
