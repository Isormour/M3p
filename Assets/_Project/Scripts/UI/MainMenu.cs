using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Main-menu button wiring. Buttons are found by name on the prefab so the scene does not
    /// need inspector bindings. The name field is stored on the profile and in PlayerPrefs.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] Button _continueButton;
        [SerializeField] Button _newGameButton;
        [SerializeField] Button _debugMapButton;
        [SerializeField] Button _clearProfileButton;
        [SerializeField] Button _quitButton;
        [SerializeField] TMP_InputField _nameField;
        [SerializeField] TMP_Text _provideNameLabel;

        const int NamePromptBlinks = 2;
        const float NamePromptBlinkHalfSeconds = 0.09f;
        static readonly Color NamePromptBlinkColor = new Color(1f, 0.22f, 0.18f, 1f);

        string _playerName = string.Empty;
        Color _provideNameColor = Color.white;
        Coroutine _provideNameBlink;
        bool _nameFieldBound;
        bool _nameReady;

        void Awake()
        {
            ResolveRefs();
            Bind(_continueButton, OnContinue);
            Bind(_newGameButton, OnNewGame);
            Bind(_debugMapButton, OnStartDebugMap);
            Bind(_clearProfileButton, OnClearProfile);
            Bind(_quitButton, OnQuit);
            BindNameField();
        }

        void Start()
        {
            RestorePlayerName();
            _nameReady = true;
            RefreshContinue();
        }

        void OnDisable()
        {
            if (!Application.isPlaying || !_nameReady)
                return;

            CommitPlayerName();
        }

        void OnDestroy()
        {
            Unbind(_continueButton, OnContinue);
            Unbind(_newGameButton, OnNewGame);
            Unbind(_debugMapButton, OnStartDebugMap);
            Unbind(_clearProfileButton, OnClearProfile);
            Unbind(_quitButton, OnQuit);
            UnbindNameField();
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

            CommitPlayerName();
            if (!game.TryContinueMap())
            {
                Debug.LogWarning($"{nameof(MainMenu)}: no map run to continue.", this);
                RefreshContinue();
                return;
            }

            Sfx.Play(SfxId.MenuConfirm);
            RefreshContinue();
        }

        public void OnNewGame()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            if (string.IsNullOrEmpty(PlayerProfile.SanitizeName(CurrentNameText())))
            {
                BlinkProvideName();
                return;
            }

            Sfx.Play(SfxId.MenuConfirm);
            CommitPlayerName();
            game.ResetProfileSave();
            game.StartNewGeneratedMap();
        }

        public void OnStartDebugMap()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            Sfx.Play(SfxId.MenuConfirm);
            CommitPlayerName();
            game.StartDebugMap();
        }

        public void OnClearProfile()
        {
            GameManager game = RequireGame();
            if (game == null)
                return;

            CommitPlayerName();
            game.ResetProfileSave();
            RefreshContinue();
        }

        public void OnQuit()
        {
            CommitPlayerName();
            Sfx.Play(SfxId.MenuConfirm);
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
            if (_nameField == null)
                _nameField = FindChildInput("NameField");
            if (_provideNameLabel == null)
                _provideNameLabel = FindChildText("labelProvideName");
            if (_provideNameLabel != null && _provideNameBlink == null)
                _provideNameColor = _provideNameLabel.color;
        }

        string CurrentNameText()
        {
            if (_nameField != null)
                return _nameField.text;

            return _playerName;
        }

        void BlinkProvideName()
        {
            if (_provideNameLabel == null)
                return;

            if (_provideNameBlink != null)
            {
                StopCoroutine(_provideNameBlink);
                _provideNameLabel.color = _provideNameColor;
            }

            _provideNameBlink = StartCoroutine(BlinkProvideNameRoutine());
        }

        IEnumerator BlinkProvideNameRoutine()
        {
            TMP_Text label = _provideNameLabel;
            Color rest = _provideNameColor;
            Color alert = NamePromptBlinkColor;
            alert.a = rest.a;

            for (int i = 0; i < NamePromptBlinks && label != null; i++)
            {
                yield return LerpLabelColor(label, rest, alert, NamePromptBlinkHalfSeconds);
                yield return LerpLabelColor(label, alert, rest, NamePromptBlinkHalfSeconds);
            }

            if (label != null)
                label.color = rest;

            _provideNameBlink = null;
        }

        static IEnumerator LerpLabelColor(TMP_Text label, Color from, Color to, float duration)
        {
            if (label == null || duration <= 0f)
            {
                if (label != null)
                    label.color = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && label != null)
            {
                elapsed += Time.unscaledDeltaTime;
                label.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            if (label != null)
                label.color = to;
        }

        void BindNameField()
        {
            if (_nameField == null || _nameFieldBound)
                return;

            _nameField.characterLimit = PlayerProfile.MaxPlayerNameLength;
            _nameField.richText = false;
            _nameField.resetOnDeActivation = false;
            _nameField.onValueChanged.AddListener(OnNameChanged);
            _nameField.onEndEdit.AddListener(OnNameEndEdit);
            _nameFieldBound = true;
        }

        void UnbindNameField()
        {
            if (!_nameFieldBound || _nameField == null)
                return;

            _nameField.onValueChanged.RemoveListener(OnNameChanged);
            _nameField.onEndEdit.RemoveListener(OnNameEndEdit);
            _nameFieldBound = false;
        }

        void RestorePlayerName()
        {
            if (_nameField == null)
                return;

            string name = string.Empty;
            if (!ProfileManager.TryGetRememberedPlayerName(out name) && GameManager.Instance != null)
            {
                PlayerProfile profile = GameManager.Instance.ProfileManager.CurrentProfile;
                if (profile != null)
                    name = profile.PlayerName;
            }

            _playerName = PlayerProfile.SanitizeName(name);
            _nameField.SetTextWithoutNotify(_playerName);
        }

        void OnNameChanged(string value)
        {
            if (!_nameReady || !isActiveAndEnabled)
                return;

            _playerName = value ?? string.Empty;
        }

        void OnNameEndEdit(string value)
        {
            _playerName = value ?? string.Empty;
            CommitPlayerName();
        }

        void CommitPlayerName()
        {
            if (_nameField == null)
                return;

            string name = PlayerProfile.SanitizeName(_playerName);
            _playerName = name;
            if (_nameField.isActiveAndEnabled && _nameField.text != name)
                _nameField.SetTextWithoutNotify(name);

            if (GameManager.Instance != null)
                GameManager.Instance.ProfileManager.SetPlayerName(name);
            else
                ProfileManager.RememberPlayerName(name);
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

        TMP_InputField FindChildInput(string objectName)
        {
            TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].name == objectName)
                    return fields[i];
            }

            return null;
        }

        TMP_Text FindChildText(string objectName)
        {
            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].name == objectName)
                    return labels[i];
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
