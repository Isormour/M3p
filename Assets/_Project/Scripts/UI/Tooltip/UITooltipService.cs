using System;
using System.Collections;
using UnityEngine;

namespace M3P
{
    public sealed class UITooltipService : MonoBehaviour
    {
        const float ShowDelay = 0.2f;

        [SerializeField] UITooltipWindow _windowPrefab;

        UITooltipWindow _window;
        Coroutine _pending;
        object _token;

        public static UITooltipService Instance { get; private set; }

        public static UITooltipService Get() => Instance;

        public static UITooltipService GetExisting() => Instance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            CancelPending();
        }

        public void RequestShow(object token, Func<TooltipContent> provider, RectTransform anchor)
        {
            if (token == null || provider == null)
                return;

            CancelPending();
            _token = token;
            _pending = StartCoroutine(ShowAfterDelay(provider, anchor));
        }

        public void Hide(object token)
        {
            if (_token != token)
                return;

            CancelPending();
            _token = null;
            _window?.Hide();
        }

        IEnumerator ShowAfterDelay(Func<TooltipContent> provider, RectTransform anchor)
        {
            yield return new WaitForSecondsRealtime(ShowDelay);

            _pending = null;
            TooltipContent content = provider != null ? provider.Invoke() : null;
            if (content == null || content.IsEmpty)
            {
                _window?.Hide();
                yield break;
            }

            EnsureWindow(anchor);
            if (_window == null)
                yield break;

            _window.Show(content, anchor);
        }

        void EnsureWindow(RectTransform anchor)
        {
            Transform parent = CanvasFor(anchor);
            if (parent == null)
                return;

            if (_window == null)
                _window = SpawnWindow(parent);

            if (_window == null)
                return;

            if (_window.transform.parent != parent)
                _window.transform.SetParent(parent, false);
        }

        UITooltipWindow SpawnWindow(Transform parent)
        {
            if (_windowPrefab == null)
            {
                Debug.LogError($"{nameof(UITooltipService)}: assign {nameof(_windowPrefab)}.", this);
                return null;
            }

            UITooltipWindow window = Instantiate(_windowPrefab, parent);
            window.name = _windowPrefab.name;
            window.Hide();
            return window;
        }

        static Transform CanvasFor(RectTransform anchor)
        {
            if (anchor != null)
            {
                Canvas canvas = anchor.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return canvas.transform;
            }

            return SkillCastPromptUI.FindCanvasParent();
        }

        void CancelPending()
        {
            if (_pending == null)
                return;

            StopCoroutine(_pending);
            _pending = null;
        }
    }
}
