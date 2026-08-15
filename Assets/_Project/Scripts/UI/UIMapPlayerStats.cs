using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD wrapper: <c>ButtonPanelStats</c> and <c>ButtonPanelCards</c> show or hide their panels.
    /// </summary>
    public sealed class UIMapPlayerStats : MonoBehaviour
    {
        [SerializeField] Button _togglePanelStatsButton;
        [SerializeField] UIPanelPlayerStats _statsPanel;
        [SerializeField] Button _togglePanelCardsButton;
        [SerializeField] UIPanelPlayerCards _cardsPanel;
        [SerializeField] bool _startHidden = true;

        void Awake()
        {
            ResolveRefs();

            if (_togglePanelStatsButton != null)
                _togglePanelStatsButton.onClick.AddListener(HandleToggleStatsClicked);

            if (_togglePanelCardsButton != null)
                _togglePanelCardsButton.onClick.AddListener(HandleToggleCardsClicked);

            if (_startHidden)
            {
                if (_statsPanel != null)
                    _statsPanel.Hide();

                if (_cardsPanel != null)
                    _cardsPanel.Hide();
            }
        }

        void OnDestroy()
        {
            if (_togglePanelStatsButton != null)
                _togglePanelStatsButton.onClick.RemoveListener(HandleToggleStatsClicked);

            if (_togglePanelCardsButton != null)
                _togglePanelCardsButton.onClick.RemoveListener(HandleToggleCardsClicked);
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (_togglePanelStatsButton == null)
                _togglePanelStatsButton = FindChildButton("ButtonPanelStats");

            if (_togglePanelCardsButton == null)
                _togglePanelCardsButton = FindChildButton("ButtonPanelCards");

            if (_statsPanel == null)
                _statsPanel = GetComponentInChildren<UIPanelPlayerStats>(true);

            if (_cardsPanel == null)
            {
                _cardsPanel = GetComponentInChildren<UIPanelPlayerCards>(true);
                if (_cardsPanel == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                        _cardsPanel = canvas.GetComponentInChildren<UIPanelPlayerCards>(true);
                }
            }
        }

        void HandleToggleStatsClicked()
        {
            if (_statsPanel == null)
            {
                Debug.LogError($"{nameof(UIMapPlayerStats)}: assign {nameof(_statsPanel)} on the prefab.", this);
                return;
            }

            _statsPanel.Toggle();
        }

        void HandleToggleCardsClicked()
        {
            if (_cardsPanel == null)
            {
                Debug.LogError($"{nameof(UIMapPlayerStats)}: assign {nameof(_cardsPanel)} on the prefab.", this);
                return;
            }

            _cardsPanel.Toggle();
        }

        Button FindChildButton(string childName)
        {
            Transform buttonTransform = transform.Find(childName);
            if (buttonTransform == null)
                return null;

            return buttonTransform.GetComponent<Button>();
        }
    }
}
