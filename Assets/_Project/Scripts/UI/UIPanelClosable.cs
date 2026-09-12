using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Shared show/hide/close wiring for map HUD panels. A panel that starts inactive never
    /// reaches <see cref="Awake"/> until something opens it, so <see cref="Show"/> also initializes.
    /// </summary>
    public class UIPanelClosable : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Button _closeButton;

        bool _initialized;

        protected GameObject Root => _panelRoot != null ? _panelRoot : gameObject;

        public bool IsOpen => Root.activeInHierarchy;

        static int _blockedWorldInputFrame = -1;

        /// <summary>
        /// True when a panel opened or closed this frame, so a UI click cannot also hit a map node.
        /// </summary>
        public static bool IsWorldInputBlocked => _blockedWorldInputFrame == Time.frameCount;

        public static void BlockWorldInput()
        {
            _blockedWorldInputFrame = Time.frameCount;
        }

        public static bool IsAnyOpen()
        {
            UIPanelClosable[] panels = FindObjectsByType<UIPanelClosable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i].IsOpen)
                    return true;
            }

            return false;
        }

        void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Also runs from <see cref="Show"/>, because a panel that starts inactive never reaches
        /// <see cref="Awake"/> until something opens it.
        /// </summary>
        protected void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ResolveRefs();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        void HandleCloseClicked()
        {
            Hide();
        }

        public virtual void Show()
        {
            Initialize();
            BlockWorldInput();
            Root.SetActive(true);
        }

        public virtual void Hide()
        {
            if (Root.activeInHierarchy)
                BlockWorldInput();

            Root.SetActive(false);
        }

        public virtual void Toggle()
        {
            if (Root.activeSelf)
                Hide();
            else
                Show();
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        protected virtual void OnValidate()
        {
            ResolveRefs();
        }

        protected virtual void ResolveRefs()
        {
            if (_panelRoot == null)
                _panelRoot = gameObject;

            if (_closeButton == null)
                _closeButton = FindDescendantButton("CloseButton");
        }

        protected Button FindDescendantButton(string childName)
        {
            Transform child = FindDescendant(childName);
            if (child == null)
                return null;

            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;

            return child.GetComponentInChildren<Button>(true);
        }

        protected Transform FindDescendant(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i];
            }

            return null;
        }

        protected T FindDescendantComponent<T>(string childName) where T : Component
        {
            Transform child = FindDescendant(childName);
            if (child == null)
                return null;

            return child.GetComponentInChildren<T>(true);
        }
    }
}
