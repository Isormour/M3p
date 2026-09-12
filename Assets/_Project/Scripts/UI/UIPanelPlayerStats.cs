using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Spends the points a level-up granted. Clicks build a pending allocation the player can take
    /// back, and only Confirm writes it into the profile — closing the panel discards it.
    /// </summary>
    public sealed class UIPanelPlayerStats : UIPanelClosable
    {
        [SerializeField] TextMeshProUGUI _levelLabel;
        [SerializeField] TextMeshProUGUI _unspentPointsLabel;

        [SerializeField] Button _confirmButton;

        [Tooltip("One row per perk tree, top to bottom. Extra trees are ignored until more rows exist.")]
        [SerializeField] UIPlayerStatControl[] _statControls = Array.Empty<UIPlayerStatControl>();

        readonly Dictionary<int, int> _pendingByTree = new Dictionary<int, int>();

        protected override void OnInitialize()
        {
            BindControls();

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
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
            // Closing without Confirm must not write the allocation.
            DiscardPendingAllocation();

            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= Refresh;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        protected override void OnValidate()
        {
            // Also re-lists on a hole, because a row lost to a prefab edit would silently stop updating.
            if (HasUnassignedControl())
                _statControls = GetComponentsInChildren<UIPlayerStatControl>(true);

            base.OnValidate();
        }

        bool HasUnassignedControl()
        {
            if (_statControls == null || _statControls.Length == 0)
                return true;

            for (int i = 0; i < _statControls.Length; i++)
            {
                if (_statControls[i] == null)
                    return true;
            }

            return false;
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        /// <summary>Closes the panel without writing the pending allocation into the profile.</summary>
        public override void Hide()
        {
            DiscardPendingAllocation();
            base.Hide();
        }

        /// <summary>Drops the allocation without spending anything, for a cancel button.</summary>
        public void DiscardPendingAllocation()
        {
            _pendingByTree.Clear();
            Refresh();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        protected override void ResolveRefs()
        {
            base.ResolveRefs();

            if (_levelLabel == null)
                _levelLabel = FindDescendantComponent<TextMeshProUGUI>("LabelValue");

            if (_unspentPointsLabel == null)
                _unspentPointsLabel = FindDescendantComponent<TextMeshProUGUI>("LabelInspendPoints");

            if (_confirmButton == null)
                _confirmButton = FindDescendantComponent<Button>("ConfirmButton");
        }

        static ProgressionService Progression => GameManager.Instance != null ? GameManager.Instance.Progression : null;

        int TotalPending
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<int, int> entry in _pendingByTree)
                    total += entry.Value;

                return total;
            }
        }

        int GetPending(int treeId)
        {
            return _pendingByTree.TryGetValue(treeId, out int pending) ? pending : 0;
        }

        void BindControls()
        {
            for (int i = 0; i < _statControls.Length; i++)
            {
                UIPlayerStatControl control = _statControls[i];
                if (control == null)
                    continue;

                control.IncreaseClicked += HandleIncreaseClicked;
                control.DecreaseClicked += HandleDecreaseClicked;
            }
        }

        void HandleIncreaseClicked(int treeId)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || treeId == PerkTree.InvalidId || profile.UnspentStatPoints - TotalPending <= 0)
                return;

            StatProgressionConfig progression = Progression?.StatProgression;
            int value = profile.GetPerkTreePoints(treeId) + GetPending(treeId);
            if (progression != null && value >= progression.MaxStatValue)
                return;

            _pendingByTree[treeId] = GetPending(treeId) + 1;
            Refresh();
        }

        void HandleDecreaseClicked(int treeId)
        {
            int pending = GetPending(treeId);
            if (pending <= 0)
                return;

            if (pending == 1)
                _pendingByTree.Remove(treeId);
            else
                _pendingByTree[treeId] = pending - 1;

            Refresh();
        }

        void HandleConfirmClicked()
        {
            CommitPendingAllocation();
            Root.SetActive(false);
        }

        void CommitPendingAllocation()
        {
            if (TotalPending <= 0)
                return;

            ProgressionService progression = Progression;
            if (progression == null)
            {
                Debug.LogError(
                    $"{nameof(UIPanelPlayerStats)}: no {nameof(GameManager)} in the scene, so the allocation cannot be saved.",
                    this);
                _pendingByTree.Clear();
                return;
            }

            foreach (KeyValuePair<int, int> entry in _pendingByTree)
            {
                if (entry.Value > 0)
                    progression.TryAllocatePerkTreePoints(entry.Key, entry.Value);
            }

            _pendingByTree.Clear();
        }

        void Refresh()
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null)
                return;

            int remainingPoints = Mathf.Max(0, profile.UnspentStatPoints - TotalPending);
            int totalPending = TotalPending;

            StatProgressionConfig progression = Progression?.StatProgression;
            PerkTree[] trees = progression != null ? progression.PerkTrees : Array.Empty<PerkTree>();
            int maxStatValue = progression != null ? progression.MaxStatValue : int.MaxValue;

            if (_levelLabel != null)
                _levelLabel.text = $"Level {profile.Level}";

            if (_unspentPointsLabel != null)
                _unspentPointsLabel.text = $"Points: {remainingPoints}";

            if (_confirmButton != null)
                _confirmButton.interactable = totalPending > 0;

            for (int i = 0; i < _statControls.Length; i++)
            {
                UIPlayerStatControl control = _statControls[i];
                if (control == null)
                    continue;

                PerkTree tree = i < trees.Length ? trees[i] : null;
                control.Bind(tree);

                if (tree == null)
                {
                    control.Refresh(progression, 0, 0, false);
                    continue;
                }

                int pending = GetPending(tree.Id);
                int value = profile.GetPerkTreePoints(tree.Id) + pending;
                control.Refresh(progression, value, pending, remainingPoints > 0 && value < maxStatValue);
            }
        }
    }
}
