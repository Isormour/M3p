using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace M3P
{
    /// <summary>
    /// Sends gameplay events through Unity Gaming Services Analytics.
    /// A matching <c>battleEnded</c> schema has to exist in the dashboard or the event is dropped.
    /// </summary>
    public static class GameAnalytics
    {
        const string BattleEndedEvent = "battleEnded";
        const string ProfileCreatedEvent = "profileCreated";

        static readonly List<CustomEvent> Pending = new List<CustomEvent>();
        static bool _ready;
        static bool _starting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (_starting || _ready)
                return;

            _starting = true;
            _ = InitializeAsync();
        }

        /// <summary>Records a brand-new save. Later fights on that save carry the same <paramref name="profileId"/>.</summary>
        public static void RecordProfileCreated(string profileId)
        {
            Enqueue(new CustomEvent(ProfileCreatedEvent)
            {
                { "profileId", profileId ?? string.Empty },
            });
        }

        /// <summary>
        /// Records one finished fight: dungeon floor, enemy archetype, and every unlocked perk tree with its points.
        /// </summary>
        public static void RecordBattleEnded(BattleOutcome outcome, EnemyDefinition enemy)
        {
            int floor = 1;
            MapRunState run = MapRunState.Active;
            if (run != null)
                floor = Mathf.Max(1, run.FloorIndex);

            string enemyName = "unknown";
            if (enemy != null)
                enemyName = string.IsNullOrEmpty(enemy.Name) ? enemy.name : enemy.Name;

            string perkTrees = DescribePerkTrees();
            bool won = outcome == BattleOutcome.Win;

            var analyticsEvent = new CustomEvent(BattleEndedEvent)
            {
                { "profileId", CurrentProfileId() },
                { "floor", floor },
                { "enemy", enemyName },
                { "won", won },
                { "perkTrees", perkTrees },
            };

            Enqueue(analyticsEvent);
        }

        static string CurrentProfileId()
        {
            PlayerProfile profile = GameManager.Instance != null
                ? GameManager.Instance.ProfileManager.CurrentProfile
                : null;
            return profile != null && !string.IsNullOrEmpty(profile.ProfileId)
                ? profile.ProfileId
                : "none";
        }

        static string DescribePerkTrees()
        {
            PlayerProfile profile = GameManager.Instance != null
                ? GameManager.Instance.ProfileManager.CurrentProfile
                : null;
            if (profile?.PerkTreePoints == null || profile.PerkTreePoints.Count == 0)
                return "none";

            StatProgressionConfig progression = GameManager.Instance.Config != null
                ? GameManager.Instance.Config.StatProgression
                : null;

            var builder = new StringBuilder();
            for (int i = 0; i < profile.PerkTreePoints.Count; i++)
            {
                PerkTreePoints entry = profile.PerkTreePoints[i];
                if (entry.TreeId == PerkTree.InvalidId)
                    continue;

                PerkTree tree = progression != null ? progression.GetTree(entry.TreeId) : null;
                string name = tree != null ? tree.DisplayName : entry.TreeId.ToString();
                if (builder.Length > 0)
                    builder.Append('|');

                builder.Append(name);
                builder.Append(':');
                builder.Append(Mathf.Max(0, entry.Value));
            }

            return builder.Length > 0 ? builder.ToString() : "none";
        }

        static void Enqueue(CustomEvent analyticsEvent)
        {
            if (_ready)
            {
                Record(analyticsEvent);
                return;
            }

            Pending.Add(analyticsEvent);
        }

        static void Record(CustomEvent analyticsEvent)
        {
            try
            {
                AnalyticsService.Instance.RecordEvent(analyticsEvent);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Analytics: failed to record an event. {exception.Message}");
            }
        }

        /// <summary>
        /// Older Analytics builds collect only after <see cref="IAnalyticsService.StartDataCollection"/>.
        /// Unity 6 rejects that call once <see cref="EndUserConsent"/> already owns the SDK.
        /// </summary>
        static void TryStartLegacyCollection()
        {
            try
            {
                AnalyticsService.Instance.StartDataCollection();
            }
            catch (InvalidOperationException)
            {
            }
        }

        static async Task InitializeAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();

                ConsentState consent = EndUserConsent.GetConsentState();
                consent.AnalyticsIntent = ConsentStatus.Granted;
                EndUserConsent.SetConsentState(consent);
                TryStartLegacyCollection();

                _ready = true;
                for (int i = 0; i < Pending.Count; i++)
                    Record(Pending[i]);
                Pending.Clear();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Analytics: Unity Services did not start. {exception.Message}");
            }
        }
    }
}
