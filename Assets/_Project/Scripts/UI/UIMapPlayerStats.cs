using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD wrapper: stats, deck, and crafting buttons show or hide their panels.
    /// </summary>
    public sealed class UIMapPlayerStats : MonoBehaviour
    {
        [SerializeField] Button _togglePanelStatsButton;
        [SerializeField] UIPanelPlayerStats _statsPanel;
        [SerializeField] Button _togglePanelCardsButton;
        [SerializeField] UIPanelPlayerCards _cardsPanel;
        [SerializeField] Button _togglePanelCraftingButton;
        [SerializeField] UIPanelCardCrafting _craftingPanel;
        [SerializeField] bool _startHidden = true;

        void Awake()
        {
            ResolveRefs();

            if (_togglePanelStatsButton != null)
                _togglePanelStatsButton.onClick.AddListener(HandleToggleStatsClicked);

            if (_togglePanelCardsButton != null)
                _togglePanelCardsButton.onClick.AddListener(HandleToggleCardsClicked);

            if (_togglePanelCraftingButton != null)
                _togglePanelCraftingButton.onClick.AddListener(HandleToggleCraftingClicked);

            if (_startHidden)
            {
                if (_statsPanel != null)
                    _statsPanel.Hide();

                if (_cardsPanel != null)
                    _cardsPanel.Hide();

                if (_craftingPanel != null)
                    _craftingPanel.Hide();
            }
        }

        void OnDestroy()
        {
            if (_togglePanelStatsButton != null)
                _togglePanelStatsButton.onClick.RemoveListener(HandleToggleStatsClicked);

            if (_togglePanelCardsButton != null)
                _togglePanelCardsButton.onClick.RemoveListener(HandleToggleCardsClicked);

            if (_togglePanelCraftingButton != null)
                _togglePanelCraftingButton.onClick.RemoveListener(HandleToggleCraftingClicked);
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

            if (_togglePanelCraftingButton == null)
                _togglePanelCraftingButton = FindChildButton("ButtonPanelCraftingCards");

            if (_statsPanel == null)
                _statsPanel = GetComponentInChildren<UIPanelPlayerStats>(true);

            if (_cardsPanel == null)
                _cardsPanel = FindPanelOnCanvas<UIPanelPlayerCards>();

            if (_craftingPanel == null)
                _craftingPanel = FindPanelOnCanvas<UIPanelCardCrafting>();
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

        void HandleToggleCraftingClicked()
        {
            if (_craftingPanel == null)
            {
                Debug.LogError($"{nameof(UIMapPlayerStats)}: assign {nameof(_craftingPanel)} on the prefab.", this);
                return;
            }

            _craftingPanel.Toggle();
        }

        T FindPanelOnCanvas<T>() where T : MonoBehaviour
        {
            T panel = GetComponentInChildren<T>(true);
            if (panel != null)
                return panel;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas.GetComponentInChildren<T>(true);

            return null;
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
