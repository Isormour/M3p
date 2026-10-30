using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace M3P.Editor.RemoteConfig
{
    public sealed class CloudEnvironment
    {
        public string Id;
        public string Name;
        public bool IsDefault;
    }

    public sealed class CloudSession
    {
        public string ProjectId;
        public string ProjectName;
        public string OrganizationId;
        public string UserName;
        public bool LoggedIn;
        public string Blocker;
        public bool EnvironmentsLoaded;
        public bool NoEnvironment;
        public List<CloudEnvironment> Environments = new List<CloudEnvironment>();
        public CloudEnvironment Target;
        public string ActiveEditorEnvironment;
    }

    /// <summary>
    /// Reads the linked Unity project and the editor session. It does not switch project or environment after an error.
    /// </summary>
    public static class EditorCloudBootstrap
    {
        public static async Task<CloudSession> RecognizeAsync(string rememberedEnvironmentId)
        {
            var session = new CloudSession
            {
                ProjectId = CloudProjectSettings.projectId,
                ProjectName = CloudProjectSettings.projectName,
                OrganizationId = CloudProjectSettings.organizationId,
                UserName = CloudProjectSettings.userName,
            };

            if (string.IsNullOrEmpty(session.ProjectId))
            {
                session.Blocker = "Projekt nie jest powiązany z Unity Cloud.";
                return session;
            }

            session.LoggedIn = !string.IsNullOrEmpty(CloudProjectSettings.userName)
                || !string.IsNullOrEmpty(CloudProjectSettings.accessToken);
            try
            {
                EnvironmentRead read = await EditorUnitySession.ReadEnvironmentsAsync();
                await NextTick();
                session.ActiveEditorEnvironment = read.ActiveName;
                if (!string.IsNullOrEmpty(read.Error) || !read.Loaded)
                {
                    session.Blocker = string.IsNullOrEmpty(read.Error)
                        ? "Nie udało się odczytać środowisk. To nie oznacza braku konfiguracji."
                        : read.Error;
                    return session;
                }

                session.Environments = read.Environments;
                session.EnvironmentsLoaded = true;
                session.Target = Choose(session.Environments, rememberedEnvironmentId, read.ActiveId);
                if (session.Environments.Count == 0)
                    session.NoEnvironment = true;
                else if (session.Target == null && !string.IsNullOrEmpty(rememberedEnvironmentId))
                    session.Blocker = "Zapamiętane środowisko nie jest już dostępne. Wybierz cel ręcznie.";
                else if (session.Target == null)
                    session.Blocker = "Nie rozpoznano środowiska docelowego. Wybierz je na liście.";
            }
            catch (Exception exception)
            {
                session.Blocker = "Nie udało się odczytać środowisk. " + exception.Message;
            }

            return session;
        }

        [MenuItem("M3P/Remote Config/Sprawdź połączenie")]
        static async void ProbeConnection()
        {
            CloudSession session = await RecognizeAsync(null);
            var report = new StringBuilder();
            report.Append("project=").Append(session.ProjectName).Append(" (").Append(session.ProjectId).AppendLine(")");
            report.Append("user=").AppendLine(session.UserName);
            report.Append("loggedIn=").AppendLine(session.LoggedIn.ToString());
            report.Append("loaded=").AppendLine(session.EnvironmentsLoaded.ToString());
            report.Append("blocker=").AppendLine(session.Blocker);
            report.Append("editorEnvironment=").AppendLine(session.ActiveEditorEnvironment);
            report.Append("target=").AppendLine(session.Target != null ? session.Target.Name + " " + session.Target.Id : "none");
            report.Append("count=").AppendLine(session.Environments.Count.ToString());
            for (int i = 0; i < session.Environments.Count; i++)
                report.Append(session.Environments[i].Name).Append(" ").AppendLine(session.Environments[i].Id);

            if (session.Target != null)
            {
                SettingsSnapshot settings = await RemoteConfigAdminClient.FetchSettingsAsync(session.ProjectId, session.Target.Id);
                report.Append("settings=").Append(settings.Status).Append(' ').AppendLine(settings.Message);
                report.Append("configExists=").AppendLine(settings.ConfigExists.ToString());
                report.Append("keyExists=").AppendLine(settings.KeyExists.ToString());
                report.Append("rulesChecked=").AppendLine(settings.RulesChecked.ToString());
                report.Append("rules=").AppendLine(settings.RulesNote);
            }

            RemoteConfigSyncController.LoadLocalList();
            report.Append("localAssets=").AppendLine(RemoteConfigSyncController.Assets.Count.ToString());

            Directory.CreateDirectory("Library/M3PRemoteConfig");
            File.WriteAllText("Library/M3PRemoteConfig/probe.txt", report.ToString());
            UnityEngine.Debug.Log(report.ToString());
        }

        static CloudEnvironment Choose(List<CloudEnvironment> environments, string rememberedId, string activeId)
        {
            if (!string.IsNullOrEmpty(rememberedId))
            {
                CloudEnvironment remembered = Find(environments, rememberedId);
                return remembered;
            }

            if (!string.IsNullOrEmpty(activeId))
            {
                CloudEnvironment active = Find(environments, activeId);
                if (active != null)
                    return active;
            }

            for (int i = 0; i < environments.Count; i++)
            {
                if (environments[i].IsDefault)
                    return environments[i];
            }

            return null;
        }

        public static CloudEnvironment Find(List<CloudEnvironment> environments, string id)
        {
            if (environments == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < environments.Count; i++)
            {
                if (string.Equals(environments[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return environments[i];
            }

            return null;
        }

        static Task NextTick()
        {
            var done = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => done.TrySetResult(true);
            return done.Task;
        }
    }
}
