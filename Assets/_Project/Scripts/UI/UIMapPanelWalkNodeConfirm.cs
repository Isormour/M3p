using System;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Confirms walking to a map node before the token moves. Close cancels; Confirm runs the move.
    /// </summary>
    public sealed class UIMapPanelWalkNodeConfirm : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _confirmButton;

        Action _onConfirm;
        bool _initialized;

        public bool IsOpen => Root.activeInHierarchy;

        GameObject Root => _panelRoot != null ? _panelRoot : gameObject;

        void Awake()
        {
            Initialize();
            Hide();
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
            ResolveRefs();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
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
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (_closeButton == null)
                _closeButton = FindDescendantButton("CloseButton");

            if (_confirmButton == null)
            {
                _confirmButton = FindDescendantButton("ConfirmButton");
                if (_confirmButton == null)
                    _confirmButton = FindDescendantButton("ConfirmButton (1)");
            }
        }

        /// <summary>Shows the panel. <paramref name="onConfirm"/> runs only if Confirm is pressed.</summary>
        public void Show(Action onConfirm)
        {
            Initialize();
            _onConfirm = onConfirm;
            Root.SetActive(true);
        }

        public void Hide()
        {
            _onConfirm = null;
            Root.SetActive(false);
        }

        void HandleCloseClicked()
        {
            Hide();
        }

        void HandleConfirmClicked()
        {
            Action callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        Button FindDescendantButton(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != childName)
                    continue;

                Button button = children[i].GetComponent<Button>();
                if (button != null)
                    return button;

                return children[i].GetComponentInChildren<Button>(true);
            }

            return null;
        }
    }
}
