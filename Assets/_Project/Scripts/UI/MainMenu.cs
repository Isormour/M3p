using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Main-menu button wiring. Buttons are found by name on the prefab so the scene does not
    /// need inspector bindings.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] Button _continueButton;
        [SerializeField] Button _newGameButton;
        [SerializeField] Button _debugMapButton;
        [SerializeField] Button _clearProfileButton;
        [SerializeField] Button _quitButton;

        void Awake()
        {
            ResolveRefs();
            Bind(_continueButton, OnContinue);
            Bind(_newGameButton, OnNewGame);
            Bind(_debugMapButton, OnStartDebugMap);
            Bind(_clearProfileButton, OnClearProfile);
            Bind(_quitButton, OnQuit);
        }

        void Start()
        {
            RefreshContinue();
        }

        void OnDestroy()
        {
            Unbind(_continueButton, OnContinue);
            Unbind(_newGameButton, OnNewGame);
            Unbind(_debugMapButton, OnStartDebugMap);
            Unbind(_clearProfileButton, OnClearProfile);
            Unbind(_quitButton, OnQuit);
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        public void OnContinue()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            if (!game.TryContinueMap())
                Debug.LogWarning($"{nameof(MainMenu)}: no map run to continue.", this);

            RefreshContinue();
        }

        public void OnNewGame()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            game.StartNewGeneratedMap();
        }

        public void OnStartDebugMap()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            game.StartDebugMap();
        }

        public void OnClearProfile()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            game.ResetProfileSave();
            RefreshContinue();
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void RefreshContinue()
        {
            if (_continueButton == null)
                return;

            _continueButton.interactable = GameManager.Instance != null && GameManager.Instance.HasContinuableRun;
        }

        void ResolveRefs()
        {
            if (_continueButton == null)
                _continueButton = FindChildButton("ButtonContinue");
            if (_newGameButton == null)
                _newGameButton = FindChildButton("ButtonNewGame");
            if (_debugMapButton == null)
                _debugMapButton = FindChildButton("ButtonPlayDebugMap", "ButtonStartDebugMap");
            if (_clearProfileButton == null)
                _clearProfileButton = FindChildButton("ButtonClearProfile", "ButtonOptions");
            if (_quitButton == null)
                _quitButton = FindChildButton("ButtonQuit");
        }

        Button FindChildButton(params string[] names)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (buttons[i] != null && buttons[i].name == names[n])
                        return buttons[i];
                }
            }

            return null;
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
                button.onClick.AddListener(handler);
        }

        static void Unbind(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
                button.onClick.RemoveListener(handler);
        }

        GameManager RequireGame()
        {
            if (GameManager.Instance != null)
                return GameManager.Instance;

            Debug.LogError(
                $"{nameof(MainMenu)}: no {nameof(GameManager)} in the scene. Play from {SceneFlow.BootScene}.",
                this);
            return null;
        }
    }
}
