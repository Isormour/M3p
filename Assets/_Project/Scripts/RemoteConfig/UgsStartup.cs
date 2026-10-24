using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// One Unity Gaming Services startup shared with analytics. The first caller chooses the environment.
    /// </summary>
    public static class UgsStartup
    {
        static readonly object Gate = new object();
        static Task _task;

        public static Task EnsureAsync(string environmentName)
        {
            lock (Gate)
            {
                _task ??= InitializeAsync(environmentName);
                return _task;
            }
        }

        public static bool IsReady => _task != null && _task.IsCompletedSuccessfully;

        static async Task InitializeAsync(string environmentName)
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
                return;

            var options = new InitializationOptions();
            if (!string.IsNullOrEmpty(environmentName))
                options.SetEnvironmentName(environmentName);

            await UnityServices.InitializeAsync(options);
        }
    }
}
