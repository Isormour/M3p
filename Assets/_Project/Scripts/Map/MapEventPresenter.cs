using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>Lightweight runtime popup for shop / chest (and other map events).</summary>
    public sealed class MapEventPresenter : MonoBehaviour
    {
        Canvas _canvas;
        GameObject _panel;
        TextMeshProUGUI _title;
        TextMeshProUGUI _body;
        Button _confirmButton;
        Action _onConfirm;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Show(string title, string body, string confirmLabel, Action onConfirm)
        {
            EnsureUi();
            EnsureEventSystem();
            _onConfirm = onConfirm;
            _title.text = title;
            _body.text = body;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = confirmLabel;
            _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null)
                _panel.SetActive(false);

            _onConfirm = null;
        }

        void EnsureUi()
        {
            if (_canvas != null)
                return;

            var canvasObject = new GameObject("MapEventCanvas");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            _panel = CreatePanel(canvasObject.transform);
            _panel.SetActive(false);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 240f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            _title = CreateText(panel.transform, "Title", 28f, new Vector2(0f, 70f), new Vector2(400f, 40f));
            _body = CreateText(panel.transform, "Body", 20f, new Vector2(0f, 10f), new Vector2(400f, 80f));
            _body.color = new Color(0.85f, 0.85f, 0.9f);

            var buttonObject = new GameObject("Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0f, -70f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f, 1f);
            _confirmButton = buttonObject.GetComponent<Button>();
            _confirmButton.onClick.AddListener(HandleConfirm);

            TextMeshProUGUI buttonLabel = CreateText(buttonObject.transform, "Label", 22f, Vector2.zero, new Vector2(180f, 44f));
            buttonLabel.text = "OK";

            return panel;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        void HandleConfirm()
        {
            Action callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirm);
        }
    }
}
