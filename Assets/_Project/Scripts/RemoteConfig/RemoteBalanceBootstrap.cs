using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// Prepares memory copies before profile services exist, then applies one remote snapshot per launch.
    /// </summary>
    public static class RemoteBalanceBootstrap
    {
        const int NetworkTimeoutMs = 5000;

        static RuntimeConfigSession _session;
        static bool _applied;
        static bool _started;
        static int _epoch;
        static bool _sceneHooked;

        public static bool IsReady { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void KickoffServices()
        {
            if (!Application.isPlaying)
                return;

            _ = UgsStartup.EnsureAsync(EnvironmentNameForServices());
        }

        public static string EnvironmentNameForServices()
        {
            RemoteConfigCatalogAsset catalog = RemoteConfigCatalogAsset.Load();
            if (catalog == null || string.IsNullOrEmpty(catalog.environmentName))
                return null;

            if (Application.isEditor && !catalog.useRemoteInEditorPlayMode)
                return null;

            return catalog.environmentName;
        }

        public static bool ShouldFetchRemote
        {
            get
            {
                RemoteConfigCatalogAsset catalog = RemoteConfigCatalogAsset.Load();
                if (catalog == null || catalog.entries == null || catalog.entries.Length == 0)
                    return false;

                if (Application.isEditor && !catalog.useRemoteInEditorPlayMode)
                    return false;

                return true;
            }
        }

        public static void Install(GameManager manager)
        {
            if (!Application.isPlaying || manager == null || _started)
                return;

            _started = true;

            RemoteConfigCatalogAsset catalog = RemoteConfigCatalogAsset.Load();
            _session = RuntimeConfigFactory.Build(manager.Config, catalog);
            if (_session.Config != null)
                manager.ApplyRuntimeConfig(_session.Config);

            // Temporarily disabled to isolate the WebGL OOM when starting a new game.
            // HookScenes();
            // RuntimeConfigFactory.RewireScene(_session);

            if (!ShouldFetchRemote)
            {
                IsReady = true;
                return;
            }

            IsReady = false;
            _ = ApplyRemoteAsync(catalog, ++_epoch);
        }

        static void HookScenes()
        {
            if (_sceneHooked)
                return;

            _sceneHooked = true;
            SceneManager.sceneLoaded += (_, _) =>
            {
                if (_session != null)
                    RuntimeConfigFactory.RewireScene(_session);
            };
        }

        static async Task ApplyRemoteAsync(RemoteConfigCatalogAsset catalog, int epoch)
        {
            JObject fallback = null;
            RemoteBalanceCache.TryLoad(catalog.projectId, catalog.environmentId, catalog.signature, out fallback);

            Task<JObject> fetch = FetchValidDocument(catalog);
            Task finished = await Task.WhenAny(fetch, Task.Delay(NetworkTimeoutMs));
            if (epoch != _epoch)
                return;

            if (finished != fetch)
            {
                ApplyFallback(catalog, fallback);
                ObserveLateResult(fetch, catalog, epoch);
                IsReady = true;
                return;
            }

            try
            {
                JObject document = await fetch;
                if (epoch != _epoch)
                    return;

                if (document != null && TryApply(catalog, document))
                {
                    _applied = true;
                    RemoteBalanceCache.Save(catalog.projectId, catalog.environmentId, catalog.signature, document);
                }
                else
                {
                    ApplyFallback(catalog, fallback);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Remote Config: używam lokalnych wartości. " + exception.Message);
                ApplyFallback(catalog, fallback);
            }

            IsReady = true;
        }

        static async void ObserveLateResult(Task<JObject> fetch, RemoteConfigCatalogAsset catalog, int epoch)
        {
            try
            {
                JObject document = await fetch;
                if (epoch != _epoch || document == null)
                    return;

                if (_applied)
                {
                    if (RuntimeConfigFactory.ApplyDocument(BuildScratch(catalog), catalog, document, out _))
                        RemoteBalanceCache.Save(catalog.projectId, catalog.environmentId, catalog.signature, document);
                    return;
                }

                if (RuntimeConfigFactory.ApplyDocument(_session, catalog, document, out _))
                {
                    _applied = true;
                    RemoteBalanceCache.Save(catalog.projectId, catalog.environmentId, catalog.signature, document);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Remote Config: spóźniona odpowiedź nie zastąpiła sesji. " + exception.Message);
            }
        }

        static void ApplyFallback(RemoteConfigCatalogAsset catalog, JObject fallback)
        {
            if (fallback != null && TryApply(catalog, fallback))
            {
                _applied = true;
                return;
            }

            _applied = true;
        }

        static bool TryApply(RemoteConfigCatalogAsset catalog, JObject document)
        {
            if (!RuntimeConfigFactory.ApplyDocument(_session, catalog, document, out string error))
            {
                Debug.LogWarning("Remote Config: odrzucono zestaw. " + error);
                return false;
            }

            return true;
        }

        static async Task<JObject> FetchValidDocument(RemoteConfigCatalogAsset catalog)
        {
            await UgsStartup.EnsureAsync(EnvironmentNameForServices());
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            RuntimeConfig config = await RemoteConfigService.Instance.FetchConfigsAsync(
                new RemoteUserAttributes(),
                new RemoteAppAttributes());

            if (config == null || !config.HasKey(M3pConfigDocument.Key))
                return null;

            string json = config.GetJson(M3pConfigDocument.Key, string.Empty);
            if (!M3pConfigDocument.TryParse(json, out JObject document, out string error))
                throw new InvalidOperationException(error);

            if (!RuntimeConfigFactory.ApplyDocument(BuildScratch(catalog), catalog, document, out error))
                throw new InvalidOperationException(error);

            return document;
        }

        static RuntimeConfigSession BuildScratch(RemoteConfigCatalogAsset catalog)
        {
            var scratch = new RuntimeConfigSession();
            if (catalog?.entries == null)
                return scratch;

            for (int i = 0; i < catalog.entries.Length; i++)
            {
                RemoteConfigCatalogEntry entry = catalog.entries[i];
                if (entry?.source == null || scratch.Copies.ContainsKey(entry.source))
                    continue;

                ScriptableObject copy = UnityEngine.Object.Instantiate(entry.source);
                copy.hideFlags = HideFlags.HideAndDontSave;
                scratch.Copies.Add(entry.source, copy);
            }

            return scratch;
        }

        struct RemoteUserAttributes
        {
        }

        struct RemoteAppAttributes
        {
        }
    }
}
