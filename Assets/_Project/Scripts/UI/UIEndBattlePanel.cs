using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public enum BattleOutcome
    {
        Win,
        Lose
    }

    public sealed class UIEndBattlePanel : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] GameObject _winSection;
        [SerializeField] GameObject _loseSection;
        [SerializeField] Button _winButton;
        [SerializeField] Button _loseButton;

        void Awake()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            WireCloseButtons();
            Hide();
        }

        void OnValidate()
        {
            if (_winButton == null && _winSection != null)
                _winButton = _winSection.GetComponentInChildren<Button>(true);

            if (_loseButton == null && _loseSection != null)
                _loseButton = _loseSection.GetComponentInChildren<Button>(true);
        }

        public void Show(BattleOutcome outcome)
        {
            if (_winSection != null)
                _winSection.SetActive(outcome == BattleOutcome.Win);

            if (_loseSection != null)
                _loseSection.SetActive(outcome == BattleOutcome.Lose);

            if (_panelRoot != null)
                _panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        void WireCloseButtons()
        {
            WireCloseButton(_winButton);
            WireCloseButton(_loseButton);
        }

        void WireCloseButton(Button button)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(HandleCloseClicked);
            button.onClick.AddListener(HandleCloseClicked);
        }

        void HandleCloseClicked()
        {
            Hide();
            BattleManager.Instance?.DismissEndBattlePanel();
        }

        void OnDestroy()
        {
            if (_winButton != null)
                _winButton.onClick.RemoveListener(HandleCloseClicked);

            if (_loseButton != null)
                _loseButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }
}
