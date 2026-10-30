using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace M3P.Editor.RemoteConfig
{
    public enum AdminStatus
    {
        Ok,
        Missing,
        Network,
        Unauthorized,
        Forbidden,
        Conflict,
        RateLimited,
        Ambiguous,
        Invalid,
        Failed,
    }

    public sealed class SettingsSnapshot
    {
        public AdminStatus Status;
        public string Message;
        public string ConfigId;
        public string ETag;
        public bool SawETag;
        public JArray Values = new JArray();
        public JObject Document;
        public bool ConfigExists;
        public bool KeyExists;
        public bool RulesChecked;
        public string RulesNote = "Reguły nie zostały sprawdzone.";
    }

    /// <summary>
    /// Admin calls made with the Unity Editor gateway session. Tokens are not stored in the project.
    /// </summary>
    public static class RemoteConfigAdminClient
    {
        const string ConfigRoot = "https://services.api.unity.com/remote-config/v1";
        const string EnvironmentRoot = "https://services.unity.com";

        public static async Task<SettingsSnapshot> FetchSettingsAsync(string projectId, string environmentId)
        {
            var snapshot = new SettingsSnapshot();
            string token = await TokenAsync();
            await MainThread();
            if (string.IsNullOrEmpty(token))
            {
                snapshot.Status = AdminStatus.Unauthorized;
                snapshot.Message = "Brak sesji Unity. Zaloguj się w edytorze.";
                return snapshot;
            }

            AdminResponse response = await SendAsync(
                "GET",
                ConfigRoot + "/projects/" + projectId + "/environments/" + environmentId + "/configs",
                null,
                token,
                null);
            await MainThread();

            if (response.Status == AdminStatus.Missing)
            {
                snapshot.Status = AdminStatus.Ok;
                snapshot.Message = "Brak bazowej konfiguracji.";
                snapshot.RulesNote = await DescribeRulesAsync(projectId, environmentId, token);
                snapshot.RulesChecked = snapshot.RulesNote != null && !snapshot.RulesNote.StartsWith("Reguły nie zostały", StringComparison.Ordinal);
                if (!snapshot.RulesChecked)
                    snapshot.RulesNote = "Reguły nie zostały sprawdzone.";
                return snapshot;
            }

            if (response.Status != AdminStatus.Ok)
            {
                snapshot.Status = response.Status;
                snapshot.Message = response.Message;
                return snapshot;
            }

            if (!TryPickSettings(response.Body, out JObject config, out string pickError))
            {
                snapshot.Status = pickError == null ? AdminStatus.Ok : AdminStatus.Ambiguous;
                snapshot.Message = pickError ?? "Brak bazowej konfiguracji.";
                if (snapshot.Status == AdminStatus.Ok)
                {
                    snapshot.RulesNote = await DescribeRulesAsync(projectId, environmentId, token);
                    snapshot.RulesChecked = snapshot.RulesNote != null && !snapshot.RulesNote.StartsWith("Reguły nie zostały", StringComparison.Ordinal);
                    if (!snapshot.RulesChecked)
                        snapshot.RulesNote = "Reguły nie zostały sprawdzone.";
                }

                return snapshot;
            }

            snapshot.ConfigExists = true;
            snapshot.ConfigId = config.Value<string>("id");
            snapshot.ETag = response.ETag;
            snapshot.SawETag = !string.IsNullOrEmpty(response.ETag);
            snapshot.Values = config["value"] as JArray ?? new JArray();
            if (TryExtractDocument(snapshot.Values, out JObject document, out string documentError))
            {
                snapshot.KeyExists = true;
                snapshot.Document = document;
                if (documentError != null)
                {
                    snapshot.Status = AdminStatus.Invalid;
                    snapshot.Message = documentError;
                    return snapshot;
                }
            }

            snapshot.Status = AdminStatus.Ok;
            snapshot.RulesNote = await DescribeRulesAsync(projectId, environmentId, token);
            snapshot.RulesChecked = snapshot.RulesNote != null && !snapshot.RulesNote.StartsWith("Reguły nie zostały", StringComparison.Ordinal);
            if (!snapshot.RulesChecked)
                snapshot.RulesNote = "Reguły nie zostały sprawdzone.";
            return snapshot;
        }

        public static async Task<AdminResponse> CreateEnvironmentAsync(string projectId, string name)
        {
            string token = await TokenAsync();
            await MainThread();
            if (string.IsNullOrEmpty(token))
                return new AdminResponse(AdminStatus.Unauthorized, 0, null, "Brak sesji Unity. Zaloguj się w edytorze.", null);

            var body = new JObject { ["name"] = name };
            return await SendAsync(
                "POST",
                EnvironmentRoot + "/api/unity/v1/projects/" + projectId + "/environments",
                body.ToString(Newtonsoft.Json.Formatting.None),
                token,
                null);
        }

        public static async Task<AdminResponse> CreateSettingsAsync(string projectId, string environmentId, JArray values)
        {
            string token = await TokenAsync();
            await MainThread();
            if (string.IsNullOrEmpty(token))
                return new AdminResponse(AdminStatus.Unauthorized, 0, null, "Brak sesji Unity. Zaloguj się w edytorze.", null);

            var body = new JObject
            {
                ["environmentId"] = environmentId,
                ["type"] = "settings",
                ["value"] = values,
            };
            return await SendAsync(
                "POST",
                ConfigRoot + "/projects/" + projectId + "/configs",
                body.ToString(Newtonsoft.Json.Formatting.None),
                token,
                null);
        }

        public static async Task<AdminResponse> UpdateSettingsAsync(string projectId, string configId, JArray values, string etag)
        {
            string token = await TokenAsync();
            await MainThread();
            if (string.IsNullOrEmpty(token))
                return new AdminResponse(AdminStatus.Unauthorized, 0, null, "Brak sesji Unity. Zaloguj się w edytorze.", null);

            var body = new JObject
            {
                ["type"] = "settings",
                ["value"] = values,
            };
            return await SendAsync(
                "PUT",
                ConfigRoot + "/projects/" + projectId + "/configs/" + configId,
                body.ToString(Newtonsoft.Json.Formatting.None),
                token,
                etag);
        }

        public static bool TryExtractDocument(JArray values, out JObject document, out string error)
        {
            document = null;
            error = null;
            if (values == null)
                return false;

            JToken raw = null;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] is not JObject entry)
                    continue;
                if (!string.Equals(entry.Value<string>("key"), M3P.RemoteConfig.M3pConfigDocument.Key, StringComparison.Ordinal))
                    continue;
                raw = entry["value"];
                break;
            }

            if (raw == null)
                return false;

            if (raw.Type == JTokenType.String)
            {
                if (!M3P.RemoteConfig.M3pConfigDocument.TryParse(raw.Value<string>(), out document, out error))
                    return true;
                return true;
            }

            if (raw.Type == JTokenType.Object)
            {
                if (!M3P.RemoteConfig.M3pConfigDocument.TryParse(raw.ToString(), out document, out error))
                    return true;
                return true;
            }

            error = "Uszkodzony JSON.";
            return true;
        }

        public static JArray ReplaceKey(JArray values, JObject document)
        {
            var copy = values == null ? new JArray() : (JArray)values.DeepClone();
            var setting = new JObject
            {
                ["key"] = M3P.RemoteConfig.M3pConfigDocument.Key,
                ["type"] = "json",
                ["value"] = document,
            };

            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] is JObject entry &&
                    string.Equals(entry.Value<string>("key"), M3P.RemoteConfig.M3pConfigDocument.Key, StringComparison.Ordinal))
                {
                    copy[i] = setting;
                    return copy;
                }
            }

            copy.Add(setting);
            return copy;
        }

        static bool TryPickSettings(string body, out JObject config, out string error)
        {
            config = null;
            error = null;
            JObject root;
            try
            {
                root = JObject.Parse(body);
            }
            catch (Exception)
            {
                error = "Niejednoznaczna odpowiedź usługi.";
                return false;
            }

            if (root["configs"] is not JArray configs)
            {
                error = "Niejednoznaczna odpowiedź usługi.";
                return false;
            }

            var settings = new List<JObject>();
            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i] is not JObject item)
                    continue;
                if (string.Equals(item.Value<string>("type"), "settings", StringComparison.OrdinalIgnoreCase))
                    settings.Add(item);
            }

            if (settings.Count == 0)
                return false;
            if (settings.Count > 1)
            {
                error = "Niejednoznaczna odpowiedź usługi: więcej niż jedna konfiguracja settings.";
                return false;
            }

            config = settings[0];
            return true;
        }

        static async Task<string> DescribeRulesAsync(string projectId, string environmentId, string token)
        {
            AdminResponse response = await SendAsync(
                "GET",
                ConfigRoot + "/projects/" + projectId + "/environments/" + environmentId + "/campaigns",
                null,
                token,
                null);
            await MainThread();
            if (response.Status != AdminStatus.Ok)
                return "Reguły nie zostały sprawdzone.";

            try
            {
                JObject root = JObject.Parse(response.Body);
                JArray campaigns = root["campaigns"] as JArray;
                if (campaigns == null)
                    return "Nie wykryto reguł dla klucza " + M3P.RemoteConfig.M3pConfigDocument.Key + ".";

                var names = new List<string>();
                for (int i = 0; i < campaigns.Count; i++)
                {
                    if (campaigns[i] is not JObject campaign)
                        continue;
                    if (campaign.ToString().IndexOf(M3P.RemoteConfig.M3pConfigDocument.Key, StringComparison.Ordinal) < 0)
                        continue;
                    string name = campaign.Value<string>("name");
                    if (!string.IsNullOrEmpty(name))
                        names.Add(name);
                }

                if (names.Count == 0)
                    return "Nie wykryto reguł dla klucza " + M3P.RemoteConfig.M3pConfigDocument.Key + ".";

                return "Reguły obejmujące klucz: " + string.Join(", ", names) + ". Gracz może dostać inny wariant niż baza.";
            }
            catch (Exception)
            {
                return "Reguły nie zostały sprawdzone.";
            }
        }

        static async Task<string> TokenAsync()
        {
            try
            {
                return await EditorUnitySession.GatewayTokenAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        static Task MainThread()
        {
            var done = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => done.TrySetResult(true);
            return done.Task;
        }

        static Task<AdminResponse> SendAsync(string method, string url, string body, string token, string etag)
        {
            var done = new TaskCompletionSource<AdminResponse>();
            var request = new UnityWebRequest(url, method);
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(etag))
                request.SetRequestHeader("If-Match", etag);
            request.timeout = 30;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                done.TrySetResult(Classify(request));
                request.Dispose();
            };
            return done.Task;
        }

        static AdminResponse Classify(UnityWebRequest request)
        {
            long code = request.responseCode;
            string body = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
            string etag = request.GetResponseHeader("ETag");
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                string message = string.IsNullOrEmpty(request.error) ? "Brak sieci." : request.error;
                if (message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = "Przekroczono czas żądania.";
                return new AdminResponse(AdminStatus.Network, code, body, message, etag);
            }

            if (code == 401)
                return new AdminResponse(AdminStatus.Unauthorized, code, body, "Brak sesji Unity albo sesja wygasła.", etag);
            if (code == 403)
                return new AdminResponse(AdminStatus.Forbidden, code, body, DescribeDenied(body), etag);
            if (code == 404)
                return new AdminResponse(AdminStatus.Missing, code, body, "Nie znaleziono zasobu.", etag);
            if (code == 409)
                return new AdminResponse(AdminStatus.Conflict, code, body, "Konflikt zapisu.", etag);
            if (code == 429)
                return new AdminResponse(AdminStatus.RateLimited, code, body, "Usługa ograniczyła liczbę żądań.", etag);
            if (code >= 200 && code < 300)
                return new AdminResponse(AdminStatus.Ok, code, body, null, etag);

            return new AdminResponse(AdminStatus.Failed, code, body, string.IsNullOrEmpty(body) ? request.error : body, etag);
        }

        static string DescribeDenied(string body)
        {
            if (string.IsNullOrEmpty(body))
                return "Brak uprawnień do Remote Config w tym projekcie.";
            return "Brak uprawnień do Remote Config. " + Trim(body, 280);
        }

        static string Trim(string text, int limit)
        {
            text = text.Replace("\n", " ").Replace("\r", " ");
            return text.Length <= limit ? text : text.Substring(0, limit);
        }
    }

    public readonly struct AdminResponse
    {
        public readonly AdminStatus Status;
        public readonly long Code;
        public readonly string Body;
        public readonly string Message;
        public readonly string ETag;

        public AdminResponse(AdminStatus status, long code, string body, string message, string etag)
        {
            Status = status;
            Code = code;
            Body = body;
            Message = message;
            ETag = etag;
        }
    }
}
