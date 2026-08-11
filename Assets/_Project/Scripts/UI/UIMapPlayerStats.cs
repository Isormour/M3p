using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD wrapper: <c>ButtonPanelStats</c> shows or hides the allocation panel.
    /// </summary>
    public sealed class UIMapPlayerStats : MonoBehaviour
    {
        [SerializeField] Button _toggleButton;
        [SerializeField] UIPanelPlayerStats _statsPanel;
        [SerializeField] bool _startHidden = true;

        void Awake()
        {
            ResolveRefs();

            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(HandleToggleClicked);

            if (_startHidden && _statsPanel != null)
                _statsPanel.Hide();
        }

        void OnDestroy()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(HandleToggleClicked);
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (_toggleButton == null)
            {
                Transform buttonTransform = transform.Find("ButtonPanelStats");
                if (buttonTransform != null)
                    _toggleButton = buttonTransform.GetComponent<Button>();
            }

            if (_statsPanel == null)
                _statsPanel = GetComponentInChildren<UIPanelPlayerStats>(true);
        }

        void HandleToggleClicked()
        {
            if (_statsPanel == null)
            {
                Debug.LogError($"{nameof(UIMapPlayerStats)}: assign {nameof(_statsPanel)} on the prefab.", this);
                return;
            }

            _statsPanel.Toggle();
        }
    }
}
