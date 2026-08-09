using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One row of the stat panel: which stat it shows, its current value and the two spend buttons.
    /// The row reports clicks and displays what it is told; the panel owns all the rules.
    /// </summary>
    public sealed class UIPlayerStatControl : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _nameLabel;
        [SerializeField] TextMeshProUGUI _valueLabel;
        [SerializeField] Button _increaseButton;
        [SerializeField] Button _decreaseButton;

        EStatType _stat;

        public event Action<EStatType> IncreaseClicked;
        public event Action<EStatType> DecreaseClicked;

        public EStatType Stat => _stat;

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
        }

        /// <summary>Tells the row which stat it represents. Called by the panel, not authored per row.</summary>
        public void Bind(EStatType stat)
        {
            _stat = stat;

            if (_nameLabel != null)
                _nameLabel.text = GetDisplayName(stat);
        }

        /// <param name="value">Committed value plus anything pending, so the row reads as the result.</param>
        /// <param name="pendingPoints">Points spent here but not yet written to the profile.</param>
        /// <param name="canIncrease">False once the character has no points left to spend.</param>
        public void Refresh(int value, int pendingPoints, bool canIncrease)
        {
            if (_valueLabel != null)
                _valueLabel.text = value.ToString();

            if (_increaseButton != null)
                _increaseButton.interactable = canIncrease;

            if (_decreaseButton != null)
                _decreaseButton.interactable = pendingPoints > 0;
        }

        void HandleIncreaseClicked() => IncreaseClicked?.Invoke(_stat);

        void HandleDecreaseClicked() => DecreaseClicked?.Invoke(_stat);

        T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        static string GetDisplayName(EStatType stat)
        {
            switch (stat)
            {
                case EStatType.Strength:
                    return "Strength";
                case EStatType.Intelligence:
                    return "Intelligence";
                case EStatType.Constitution:
                    return "Constitution";
                case EStatType.Agility:
                    return "Agility";
                default:
                    return stat.ToString();
            }
        }
    }
}
