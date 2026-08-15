using System;
using System.Collections.Generic;
using Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Temporary prompt for cards that need a colour or gravity direction after targeting.
    /// </summary>
    public sealed class UICardChoiceOverlay : MonoBehaviour
    {
        readonly List<Button> _buttons = new List<Button>();

        Action<int> _picked;
        Action _cancelled;

        public static UICardChoiceOverlay Show(
            Transform parent,
            string title,
            IReadOnlyList<CardChoiceOption> options,
            Action<int> picked,
            Action cancelled)
        {
            GameObject root = new GameObject("CardChoiceOverlay", typeof(RectTransform), typeof(UICardChoiceOverlay));
            root.transform.SetParent(parent, false);

            UICardChoiceOverlay overlay = root.GetComponent<UICardChoiceOverlay>();
            overlay.Build(title, options, picked, cancelled);
            return overlay;
        }

        void Build(string title, IReadOnlyList<CardChoiceOption> options, Action<int> picked, Action cancelled)
        {
            _picked = picked;
            _cancelled = cancelled;

            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, 0.22f);
            rect.anchorMax = new Vector2(0.5f, 0.22f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(420f, options.Count * 110f), 88f);

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateLabel(title, 0.7f);

            for (int i = 0; i < options.Count; i++)
            {
                CardChoiceOption option = options[i];
                CreateChoiceButton(option);
            }

            CreateCancelButton();
        }

        void CreateLabel(string text, float flex)
        {
            GameObject labelObject = new GameObject("Title", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 20f;
            label.raycastTarget = false;

            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flex;
            layout.minWidth = 90f;
        }

        void CreateChoiceButton(CardChoiceOption option)
        {
            GameObject buttonObject = new GameObject(option.Label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(transform, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = option.Color;

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredWidth = 96f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = option.Label;
            label.alignment = TextAlignmentOptions.Center;
            label.color = ContrastText(option.Color);
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 18f;
            label.raycastTarget = false;

            Button button = buttonObject.GetComponent<Button>();
            int value = option.Value;
            button.onClick.AddListener(() => HandlePicked(value));
            _buttons.Add(button);
        }

        void CreateCancelButton()
        {
            GameObject buttonObject = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(transform, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.25f, 0.2f, 0.2f, 1f);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = 64f;
            layout.preferredWidth = 80f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Anuluj";
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 16f;
            label.raycastTarget = false;

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(HandleCancelled);
            _buttons.Add(button);
        }

        void HandleCancelled()
        {
            Action cancelled = _cancelled;
            _picked = null;
            _cancelled = null;
            cancelled?.Invoke();
        }

        void HandlePicked(int value)
        {
            Action<int> picked = _picked;
            _picked = null;
            _cancelled = null;
            picked?.Invoke(value);
        }

        public void Dismiss()
        {
            _picked = null;
            _cancelled = null;
            if (gameObject != null)
                Destroy(gameObject);
        }

        static Color ContrastText(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.55f ? Color.black : Color.white;
        }
    }
}
