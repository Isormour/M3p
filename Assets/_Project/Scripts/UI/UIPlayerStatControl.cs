using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One row of the stat panel: which perk tree it shows, its current value and the two spend buttons.
    /// The row reports clicks and displays what it is told; the panel owns all the rules.
    /// </summary>
    public sealed class UIPlayerStatControl : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _nameLabel;
        [SerializeField] TextMeshProUGUI _valueLabel;
        [SerializeField] Button _increaseButton;
        [SerializeField] Button _decreaseButton;
        [SerializeField] Image statIcon;

        [Tooltip("Perk ladder drawn above the row; optional for rows that only show a number.")]
        [SerializeField] UIPanelStatsPerkSection _perkSection;

        PerkTree _tree;

        public event Action<int> IncreaseClicked;
        public event Action<int> DecreaseClicked;

        public PerkTree Tree => _tree;
        public int TreeId => _tree != null ? _tree.Id : PerkTree.InvalidId;

        void Awake()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.AddListener(HandleIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.AddListener(HandleDecreaseClicked);
        }

        void OnDestroy()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.RemoveListener(HandleIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.RemoveListener(HandleDecreaseClicked);
        }

        void OnValidate()
        {
            if (_increaseButton == null)
                _increaseButton = FindChild<Button>("ButtonIncrease");

            if (_decreaseButton == null)
                _decreaseButton = FindChild<Button>("ButtonDecrease");

            if (_nameLabel == null)
                _nameLabel = FindChild<TextMeshProUGUI>("LabelStatName");

            if (_valueLabel == null)
                _valueLabel = FindChild<TextMeshProUGUI>("LabelStatValue");

            if (_perkSection == null)
                _perkSection = GetComponentInChildren<UIPanelStatsPerkSection>(true);

            if (statIcon == null)
                statIcon = FindChild<Image>("StatImage");
        }

        /// <summary>Tells the row which perk tree it represents. Called by the panel, not authored per row.</summary>
        public void Bind(PerkTree tree)
        {
            _tree = tree;

            if (_nameLabel != null)
                _nameLabel.text = tree != null ? tree.DisplayName : string.Empty;
        }

        /// <param name="progression">Source of the cap the ladder is scaled against.</param>
        /// <param name="value">Committed value plus anything pending, so the row reads as the result.</param>
        /// <param name="pendingPoints">Points spent here but not yet written to the profile.</param>
        /// <param name="canIncrease">False once there are no points left to spend or the cap is reached.</param>
        public void Refresh(StatProgressionConfig progression, int value, int pendingPoints, bool canIncrease)
        {
            if (_valueLabel != null)
                _valueLabel.text = value.ToString();

            ApplyStatIcon();

            if (_perkSection != null)
            {
                _perkSection.Bind(_tree, progression);
                _perkSection.Refresh(value);
            }

            if (_increaseButton != null)
                _increaseButton.interactable = canIncrease;

            if (_decreaseButton != null)
                _decreaseButton.interactable = pendingPoints > 0;
        }

        void ApplyStatIcon()
        {
            if (statIcon == null)
                return;

            Sprite icon = _tree != null ? _tree.Icon : null;
            statIcon.sprite = icon;
            statIcon.enabled = icon != null;
        }

        void HandleIncreaseClicked()
        {
            if (TreeId != PerkTree.InvalidId)
                IncreaseClicked?.Invoke(TreeId);
        }

        void HandleDecreaseClicked()
        {
            if (TreeId != PerkTree.InvalidId)
                DecreaseClicked?.Invoke(TreeId);
        }

        T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
