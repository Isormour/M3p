using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Spends the points a level-up granted. Clicks build a pending allocation the player can take
    /// back, and only Confirm writes it into the profile — closing the panel discards it.
    /// </summary>
    public sealed class UIPanelPlayerStats : MonoBehaviour
    {
        static readonly int StatTypeCount = Enum.GetValues(typeof(EStatType)).Length;

        [SerializeField] GameObject _panelRoot;
        [SerializeField] TextMeshProUGUI _levelLabel;
        [SerializeField] TextMeshProUGUI _unspentPointsLabel;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _confirmButton;

        [Tooltip("One row per stat, top to bottom.")]
        [SerializeField] UIPlayerStatControl[] _statControls = Array.Empty<UIPlayerStatControl>();

        [Tooltip("Which stat each row above represents.")]
        [SerializeField] EStatType[] _statOrder =
        {
            EStatType.Strength,
            EStatType.Constitution,
            EStatType.Intelligence,
            EStatType.Agility
        };

        readonly int[] _pendingByStat = new int[StatTypeCount];

        bool _initialized;

        void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Also runs from <see cref="Show"/>, because a panel that starts inactive never reaches
        /// <see cref="Awake"/> until something opens it.
        /// </summary>
        void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            BindControls();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);

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

        void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        void OnValidate()
        {
            if (_statControls == null || _statControls.Length == 0)
                _statControls = GetComponentsInChildren<UIPlayerStatControl>(true);

            if (_levelLabel == null)
                _levelLabel = FindDescendantComponent<TextMeshProUGUI>("LabelValue");

            if (_unspentPointsLabel == null)
                _unspentPointsLabel = FindDescendantComponent<TextMeshProUGUI>("LabelInspendPoints");

            if (_closeButton == null)
                _closeButton = FindDescendantComponent<Button>("CloseButton");

            if (_confirmButton == null)
                _confirmButton = FindDescendantComponent<Button>("ConfirmButton");
        }

        public void Show()
        {
            Initialize();
            Root.SetActive(true);
            Refresh();
        }

        /// <summary>Closes the panel without writing the pending allocation into the profile.</summary>
        public void Hide()
        {
            DiscardPendingAllocation();
            Root.SetActive(false);
        }

        public void Toggle()
        {
            if (Root.activeSelf)
                Hide();
            else
                Show();
        }

        GameObject Root => _panelRoot != null ? _panelRoot : gameObject;

        /// <summary>Drops the allocation without spending anything, for a cancel button.</summary>
        public void DiscardPendingAllocation()
        {
            Array.Clear(_pendingByStat, 0, _pendingByStat.Length);
            Refresh();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static ProgressionService Progression => GameManager.Instance != null ? GameManager.Instance.Progression : null;

        int TotalPending
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _pendingByStat.Length; i++)
                    total += _pendingByStat[i];

                return total;
            }
        }

        void BindControls()
        {
            for (int i = 0; i < _statControls.Length; i++)
            {
                UIPlayerStatControl control = _statControls[i];
                if (control == null)
                    continue;

                if (_statOrder == null || i >= _statOrder.Length)
                {
                    Debug.LogError(
                        $"{nameof(UIPanelPlayerStats)}: row {i} has no entry in {nameof(_statOrder)}, so it cannot know which stat it edits.",
                        this);
                    continue;
                }

                control.Bind(_statOrder[i]);
                control.IncreaseClicked += HandleIncreaseClicked;
                control.DecreaseClicked += HandleDecreaseClicked;
            }
        }

        void HandleIncreaseClicked(EStatType stat)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || profile.UnspentStatPoints - TotalPending <= 0)
                return;

            _pendingByStat[(int)stat]++;
            Refresh();
        }

        void HandleDecreaseClicked(EStatType stat)
        {
            if (_pendingByStat[(int)stat] <= 0)
                return;

            _pendingByStat[(int)stat]--;
            Refresh();
        }

        void HandleCloseClicked()
        {
            Hide();
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
                Array.Clear(_pendingByStat, 0, _pendingByStat.Length);
                return;
            }

            for (int i = 0; i < _pendingByStat.Length; i++)
            {
                if (_pendingByStat[i] > 0)
                    progression.TryAllocateStatPoints((EStatType)i, _pendingByStat[i]);
            }

            Array.Clear(_pendingByStat, 0, _pendingByStat.Length);
        }

        void Refresh()
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null)
                return;

            int remainingPoints = Mathf.Max(0, profile.UnspentStatPoints - TotalPending);
            int totalPending = TotalPending;

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

                int pending = _pendingByStat[(int)control.Stat];
                control.Refresh(profile.HardStats.Get(control.Stat) + pending, pending, remainingPoints > 0);
            }
        }

        T FindDescendantComponent<T>(string childName) where T : Component
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i].GetComponentInChildren<T>(true);
            }

            return null;
        }
    }
}
