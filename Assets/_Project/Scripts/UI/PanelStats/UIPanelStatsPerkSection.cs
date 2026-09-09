using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The perk ladder for one stat: a fill bar showing how far the stat has climbed towards its cap,
    /// plus one marker per configured perk placed at the height of its threshold.
    /// </summary>
    public sealed class UIPanelStatsPerkSection : MonoBehaviour
    {
        [SerializeField] UIPanelStatsSkillFillBar _fillBar;
        [SerializeField] UIPanelStatsPerk _perkPrefab;
        [SerializeField] Transform _perkParent;

        readonly List<UIPanelStatsPerk> _markers = new List<UIPanelStatsPerk>();

        StatProgressionConfig _progression;
        EStatType _stat;
        bool _built;

        /// <summary>
        /// Spawns the ladder for a stat. Cheap to call every refresh: it only rebuilds when the stat
        /// or the config it reads changes, which lets the panel bind before a config exists.
        /// </summary>
        public void Bind(EStatType stat, StatProgressionConfig progression)
        {
            if (_built && _stat == stat && _progression == progression)
                return;

            _stat = stat;
            _progression = progression;
            _built = true;
            BuildLadder();
        }

        /// <param name="statValue">Committed value plus anything pending, matching the row's label.</param>
        public void Refresh(int statValue)
        {
            int max = _progression != null ? _progression.MaxStatValue : 1;

            if (_fillBar != null)
                _fillBar.SetFill(statValue / (float)max);

            for (int i = 0; i < _markers.Count; i++)
                _markers[i].SetUnlocked(statValue >= _markers[i].StatLevel);
        }

        void BuildLadder()
        {
            ClearLadder();

            if (_progression == null || _perkPrefab == null || _perkParent == null)
                return;

            StatPerk[] ladder = _progression.GetProgression(_stat);
            int max = _progression.MaxStatValue;

            for (int i = 0; i < ladder.Length; i++)
            {
                StatPerk entry = ladder[i];

                // A threshold past the cap can never be reached, so it has no place on the bar.
                if (entry.Perk == null || entry.StatLevel <= 0 || entry.StatLevel > max)
                    continue;

                UIPanelStatsPerk marker = Instantiate(_perkPrefab, _perkParent);
                marker.name = $"Perk{entry.StatLevel}_{entry.Perk.name}";
                marker.Bind(entry.Perk, entry.StatLevel, entry.StatLevel / (float)max);
                _markers.Add(marker);
            }
        }

        void ClearLadder()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null)
                    Destroy(_markers[i].gameObject);
            }

            _markers.Clear();
        }

        void OnValidate()
        {
            if (_fillBar == null)
                _fillBar = GetComponentInChildren<UIPanelStatsSkillFillBar>(true);
        }
    }
}
