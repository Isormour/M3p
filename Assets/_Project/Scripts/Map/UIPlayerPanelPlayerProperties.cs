using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    /// <summary>
    /// Lists the profile's player properties. Pending stat-panel points are included, so the column
    /// moves before Confirm writes them.
    /// </summary>
    public sealed class UIPlayerPanelPlayerProperties : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("barParent")] Transform _barParent;
        [SerializeField, FormerlySerializedAs("barPrefab")] UIPanelStatsPropertyBar _barPrefab;

        readonly List<UIPanelStatsPropertyBar> _bars = new List<UIPanelStatsPropertyBar>();

        void Awake()
        {
            ResolveRefs();
        }

        void OnEnable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= Refresh;
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        /// <summary>Rebuilds the column from the current profile and any points the stat panel is holding.</summary>
        public void Refresh()
        {
            EnsureBars();

            PlayerProfile profile = Profiles != null ? Profiles.CurrentProfile : null;
            StatProgressionConfig progression = Progression != null ? Progression.StatProgression : null;
            TalentConfig talents = Progression != null ? Progression.Talents : null;
            PlayerProperties properties = profile != null
                ? profile.CalculateProperties(progression, talents, PendingPoints)
                : default;

            Show(properties);
        }

        public void Show(PlayerProperties properties)
        {
            EnsureBars();

            StatProgressionConfig progression = Progression != null ? Progression.StatProgression : null;
            PlayerProperty[] order = PlayerProperties.All;
            for (int i = 0; i < _bars.Count && i < order.Length; i++)
            {
                if (_bars[i] == null)
                    continue;

                Sprite icon = progression != null ? progression.GetPropertyIcon(order[i]) : null;
                _bars[i].Show(order[i], properties.Get(order[i]), icon);
            }
        }

        int PendingPoints(int treeId)
        {
            UIPanelPlayerStats panel = GetComponentInParent<UIPanelPlayerStats>(true);
            return panel != null ? panel.PendingPoints(treeId) : 0;
        }

        void EnsureBars()
        {
            PlayerProperty[] order = PlayerProperties.All;
            if (_bars.Count == order.Length && _bars[0] != null)
                return;

            ClearBars();

            if (_barPrefab == null || _barParent == null)
            {
                Debug.LogError(
                    $"{nameof(UIPlayerPanelPlayerProperties)}: assign the bar parent and prefab.",
                    this);
                return;
            }

            for (int i = 0; i < order.Length; i++)
            {
                UIPanelStatsPropertyBar bar = Instantiate(_barPrefab, _barParent);
                bar.name = order[i].ToString();
                _bars.Add(bar);
            }
        }

        void ClearBars()
        {
            for (int i = _bars.Count - 1; i >= 0; i--)
            {
                if (_bars[i] != null)
                    Destroy(_bars[i].gameObject);
            }

            _bars.Clear();

            if (_barParent == null)
                return;

            for (int i = _barParent.childCount - 1; i >= 0; i--)
            {
                Transform child = _barParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        void ResolveRefs()
        {
            if (_barParent == null)
            {
                Transform parent = transform.Find("Properties");
                if (parent != null)
                    _barParent = parent;
            }
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static ProgressionService Progression => GameManager.Instance != null ? GameManager.Instance.Progression : null;
    }
}
