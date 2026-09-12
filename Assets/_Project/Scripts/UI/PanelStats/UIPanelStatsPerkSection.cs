using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The perk ladder for one tree: a fill bar showing how far the tree has climbed towards its cap,
    /// plus one marker per configured perk placed at the height of its threshold.
    /// </summary>
    public sealed class UIPanelStatsPerkSection : MonoBehaviour
    {
        [SerializeField] UIPanelStatsSkillFillBar _fillBar;
        [SerializeField] UIPanelStatsPerk _perkPrefab;
        [SerializeField] Transform _perkParent;
        [Tooltip("Gap between the fill bar's edge and the inner edge of each perk icon.")]
        [SerializeField] float _sideGap = 8f;
        [Tooltip("Extra space kept between two icons that share the same side of the bar.")]
        [SerializeField] float _verticalGap = 4f;

        readonly List<UIPanelStatsPerk> _markers = new List<UIPanelStatsPerk>();

        StatProgressionConfig _progression;
        PerkTree _tree;
        bool _built;
        bool _relayoutPending;

        /// <summary>
        /// Spawns the ladder for a tree. Cheap to call every refresh: it only rebuilds when the tree
        /// or the config it reads changes, which lets the panel bind before a config exists.
        /// </summary>
        public void Bind(PerkTree tree, StatProgressionConfig progression)
        {
            if (_built && _tree == tree && _progression == progression)
                return;

            _tree = tree;
            _progression = progression;
            _built = true;
            BuildLadder();
        }

        /// <param name="treeValue">Committed value plus anything pending, matching the row's label.</param>
        public void Refresh(int treeValue)
        {
            int max = _progression != null ? _progression.MaxStatValue : 1;

            if (_fillBar != null)
                _fillBar.SetFill(treeValue / (float)max);

            for (int i = 0; i < _markers.Count; i++)
                _markers[i].SetUnlocked(treeValue >= _markers[i].StatLevel);
        }

        void BuildLadder()
        {
            ClearLadder();

            if (_tree == null || _perkPrefab == null || _perkParent == null)
                return;

            StatPerk[] ladder = _tree.Perks;
            int max = _progression != null ? _progression.MaxStatValue : 1;
            float sideInset = ResolveSideInset();

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

            _markers.Sort((a, b) => a.StatLevel.CompareTo(b.StatLevel));
            ApplySides(sideInset);
            _relayoutPending = true;
        }

        void LateUpdate()
        {
            if (!_relayoutPending)
                return;

            _relayoutPending = false;
            ApplySides(ResolveSideInset());
        }

        float ResolveSideInset()
        {
            float inset = Mathf.Max(0f, _sideGap);

            if (_fillBar == null)
                return inset;

            RectTransform fillRect = (RectTransform)_fillBar.transform;
            float halfWidth = fillRect.rect.width * 0.5f;
            if (halfWidth <= 0.01f)
                halfWidth = Mathf.Abs(fillRect.sizeDelta.x) * 0.5f;

            return inset + halfWidth;
        }

        /// <summary>
        /// Puts each icon left or right of the centered bar, flipping side whenever two thresholds
        /// would stack on top of each other.
        /// </summary>
        void ApplySides(float sideInset)
        {
            float minNormalizedGap = ResolveMinNormalizedGap();
            float lastLeft = float.NegativeInfinity;
            float lastRight = float.NegativeInfinity;
            bool preferRight = false;

            for (int i = 0; i < _markers.Count; i++)
            {
                UIPanelStatsPerk marker = _markers[i];
                bool onRight = ChooseRightSide(marker.HeightRatio, lastLeft, lastRight, minNormalizedGap, preferRight);
                marker.Place(onRight, sideInset);

                if (onRight)
                    lastRight = marker.HeightRatio;
                else
                    lastLeft = marker.HeightRatio;

                preferRight = !onRight;
            }
        }

        float ResolveMinNormalizedGap()
        {
            RectTransform parent = _perkParent as RectTransform;
            float parentHeight = parent != null ? parent.rect.height : 0f;
            if (parentHeight <= 0f)
                return 0f;

            float iconHeight = 0f;
            if (_perkPrefab != null)
                iconHeight = ((RectTransform)_perkPrefab.transform).sizeDelta.y;

            return (iconHeight + Mathf.Max(0f, _verticalGap)) / parentHeight;
        }

        static bool ChooseRightSide(float height, float lastLeft, float lastRight, float minGap, bool preferRight)
        {
            bool leftClear = height - lastLeft >= minGap;
            bool rightClear = height - lastRight >= minGap;

            if (leftClear && rightClear)
                return preferRight;

            if (leftClear)
                return false;

            if (rightClear)
                return true;

            return height - lastRight > height - lastLeft;
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
