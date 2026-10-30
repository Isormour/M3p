using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace M3P.Editor.RemoteConfig
{
    /// <summary>
    /// Talks to the editor services assemblies without a hard reference.
    /// Those assemblies are not auto-referenced by the project scripts.
    /// </summary>
    static class EditorUnitySession
    {
        public static async Task<string> GatewayTokenAsync()
        {
            Type type = Find("Unity.Services.Core.Editor.AccessTokens");
            if (type == null)
                return null;

            object tokens = Activator.CreateInstance(type);
            MethodInfo method = type.GetMethod("GetServicesGatewayTokenAsync", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return null;

            var task = (Task<string>)method.Invoke(tokens, null);
            return await task;
        }

        public static async Task<EnvironmentRead> ReadEnvironmentsAsync()
        {
            var read = new EnvironmentRead();
            Type apiType = Find("Unity.Services.Core.Editor.Environments.EnvironmentsApi");
            if (apiType == null)
            {
                read.Error = "Edytor nie udostępnia API środowisk Unity Services.";
                return read;
            }

            object api = apiType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (api == null)
            {
                read.Error = "Edytor nie udostępnia API środowisk Unity Services.";
                return read;
            }

            MethodInfo refresh = api.GetType().GetMethod("RefreshAsync");
            if (refresh == null)
            {
                read.Error = "Edytor nie udostępnia odświeżania środowisk.";
                return read;
            }

            await (Task)refresh.Invoke(api, null);
            Type concrete = api.GetType();
            read.ActiveName = concrete.GetProperty("ActiveEnvironmentName")?.GetValue(api) as string;
            object activeId = concrete.GetProperty("ActiveEnvironmentId")?.GetValue(api);
            if (activeId is Guid guid && guid != Guid.Empty)
                read.ActiveId = guid.ToString();

            object environments = concrete.GetProperty("Environments")?.GetValue(api);
            if (environments == null)
            {
                read.Error = "Nie udało się odczytać środowisk. To nie oznacza braku konfiguracji.";
                return read;
            }

            foreach (object info in (IEnumerable)environments)
            {
                Type infoType = info.GetType();
                var item = new CloudEnvironment
                {
                    Name = infoType.GetField("Name")?.GetValue(info) as string,
                    IsDefault = infoType.GetField("IsDefault") != null && (bool)infoType.GetField("IsDefault").GetValue(info),
                };
                object id = infoType.GetField("Id")?.GetValue(info);
                item.Id = id != null ? id.ToString() : string.Empty;
                read.Environments.Add(item);
            }

            read.Loaded = true;
            return read;
        }

        static Type Find(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }

    sealed class EnvironmentRead
    {
        public bool Loaded;
        public string Error;
        public string ActiveName;
        public string ActiveId;
        public List<CloudEnvironment> Environments = new List<CloudEnvironment>();
    }
}
